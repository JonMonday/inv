using InvServer.Core.Constants;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure.Services;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly InvDbContext _db;

    public WorkflowEngine(InvDbContext db)
    {
        _db = db;
    }

    public async Task<long> StartWorkflowAsync(string workflowCode, string businessEntityKey, long initiatorUserId, List<WorkflowManualAssignmentDto>? manualAssignments = null, long? versionId = null)
    {
        // 1. Get workflow version
        WorkflowDefinitionVersion? version = null;

        if (versionId.HasValue)
        {
            version = await _db.WorkflowDefinitionVersions
                .Include(v => v.WorkflowDefinition)
                .Include(v => v.Steps.OrderBy(s => s.SequenceNo))
                .FirstOrDefaultAsync(v => v.WorkflowDefinitionVersionId == versionId.Value);
        }
        else
        {
            version = await _db.WorkflowDefinitionVersions
                .Include(v => v.WorkflowDefinition)
                .Include(v => v.Steps.OrderBy(s => s.SequenceNo))
                .Where(v => v.WorkflowDefinition.Code == workflowCode && v.IsActive)
                .OrderByDescending(v => v.VersionNo)
                .FirstOrDefaultAsync();
        }

        if (version == null)
            throw new InvalidOperationException($"Workflow version {(versionId.HasValue ? versionId.Value.ToString() : workflowCode)} not found or inactive.");

        var startStep = version.Steps.FirstOrDefault(s => s.SequenceNo == 0);
        if (startStep == null)
            throw new InvalidOperationException($"No start step defined for workflow {version.WorkflowDefinition.Name}.");

        // 2. Create Instance
        var instanceStatusId = await GetStatusIdAsync("WORKFLOW_INSTANCE_STATUS", WorkflowInstanceStatusCodes.Active);

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionVersionId = version.WorkflowDefinitionVersionId,
            WorkflowInstanceStatusId = instanceStatusId,
            InitiatorUserId = initiatorUserId,
            BusinessEntityKey = businessEntityKey,
            CurrentWorkflowStepId = startStep.WorkflowStepId,
            StartedAt = DateTime.UtcNow
        };

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync(); // Get instance ID

        // 3. Store Manual Assignments if any
        if (manualAssignments != null && manualAssignments.Any())
        {
            foreach (var ma in manualAssignments)
            {
                foreach (var userId in ma.UserIds)
                {
                    _db.WorkflowInstanceManualAssignments.Add(new WorkflowInstanceManualAssignment
                    {
                        WorkflowInstanceId = instance.WorkflowInstanceId,
                        WorkflowStepId = ma.WorkflowStepId,
                        UserId = userId
                    });
                }
            }
            await _db.SaveChangesAsync();
        }

        // 4. Create Task for Start Step
        await CreateTasksForStepAsync(instance, startStep);
        await _db.SaveChangesAsync();

        return instance.WorkflowInstanceId;
    }

    public async Task ProcessActionAsync(long taskId, string actionCode, long userId, string? notes = null, string? payloadJson = null)
    {
        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowInstance)
            .Include(t => t.WorkflowStep)
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == taskId);

        if (task == null) throw new KeyNotFoundException("Task not found.");

        // 4. Validate Task Status (Must be active/pending or claimed)
        var pendingStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Pending);
        var availableStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Available);
        var claimedStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Claimed);
        
        if (task.WorkflowTaskStatusId != pendingStatusId && 
            task.WorkflowTaskStatusId != availableStatusId && 
            task.WorkflowTaskStatusId != claimedStatusId)
        {
            throw new InvalidOperationException($"Task {taskId} is not in an active state.");
        }

        // 5. Validate Assignee/Claimer (Strict Rule)
        bool canProcess = false;
        if (task.ClaimedByUserId != null)
        {
            if (task.ClaimedByUserId == userId)
                canProcess = true;
            else
                throw new InvalidOperationException($"Task {taskId} is already claimed by another user.");
        }
        else
        {
            // If not claimed, user must be an assignee to take action
            canProcess = task.Assignees.Any(a => a.UserId == userId);
            
            // Auto-claim if they are an assignee and it's not claimed
            if (canProcess)
            {
                task.ClaimedByUserId = userId;
                task.ClaimedAt = DateTime.UtcNow;
            }
        }

        if (!canProcess)
            throw new UnauthorizedAccessException($"User {userId} is not authorized for Task {taskId}.");

        // 6. Record Action
        var actionTypeId = await GetTypeIdAsync("WORKFLOW_ACTION_TYPE", actionCode);
        
        var action = new WorkflowTaskAction
        {
            WorkflowTaskId = taskId,
            WorkflowActionTypeId = actionTypeId,
            ActionByUserId = userId,
            ActionAt = DateTime.UtcNow,
            Notes = notes,
            PayloadJson = payloadJson
        };
        _db.WorkflowTaskActions.Add(action);

        // 7. Update Assignee Status
        var assignee = task.Assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignee != null)
        {
            string assigneeStatus = actionCode switch
            {
                WorkflowActionCodes.Approve => WorkflowTaskAssigneeStatusCodes.Approved,
                WorkflowActionCodes.Reject => WorkflowTaskAssigneeStatusCodes.Rejected,
                _ => WorkflowTaskAssigneeStatusCodes.Approved // Default for others like 'Complete' or 'Submit'
            };
            assignee.AssigneeStatusId = await GetStatusIdAsync("WORKFLOW_TASK_ASSIGNEE_STATUS", assigneeStatus);
            assignee.DecidedAt = DateTime.UtcNow;
        }

        // 8. Evaluate Step Completion
        var rule = await _db.WorkflowStepRules.FirstOrDefaultAsync(r => r.WorkflowStepId == task.WorkflowStepId);
        bool isStepComplete = false;

        if (actionCode == WorkflowActionCodes.Reject || actionCode == WorkflowActionCodes.Cancel)
        {
            isStepComplete = true; // Immediate completion for rejections/cancellations
        }
        else if (rule != null)
        {
            var approvedStatusId = await GetStatusIdAsync("WORKFLOW_TASK_ASSIGNEE_STATUS", WorkflowTaskAssigneeStatusCodes.Approved);
            var approvedCount = task.Assignees.Count(a => a.AssigneeStatusId == approvedStatusId);

            if (rule.RequireAll)
                isStepComplete = approvedCount >= task.Assignees.Count;
            else
                isStepComplete = approvedCount >= rule.MinApprovers;
        }
        else
        {
            isStepComplete = true; // No rule = first action completes
        }

        if (isStepComplete)
        {
            string taskStatus = actionCode switch
            {
                WorkflowActionCodes.Approve => WorkflowTaskStatusCodes.Approved,
                WorkflowActionCodes.Reject => WorkflowTaskStatusCodes.Rejected,
                WorkflowActionCodes.Cancel => WorkflowTaskStatusCodes.Cancelled,
                _ => WorkflowTaskStatusCodes.Completed
            };
            task.WorkflowTaskStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", taskStatus);
            task.CompletedAt = DateTime.UtcNow;

            // 9. Find Transition
            var transition = await _db.WorkflowTransitions
                .FirstOrDefaultAsync(tr => tr.FromWorkflowStepId == task.WorkflowStepId && tr.WorkflowActionTypeId == actionTypeId);

            if (transition != null)
            {
                task.WorkflowInstance.CurrentWorkflowStepId = transition.ToWorkflowStepId;
                var nextStep = await _db.WorkflowSteps.FindAsync(transition.ToWorkflowStepId);
                if (nextStep != null)
                {
                    await CreateTasksForStepAsync(task.WorkflowInstance, nextStep);
                }
            }
            else
            {
                // Terminal step
                string instanceStatus = actionCode switch
                {
                    WorkflowActionCodes.Reject => WorkflowInstanceStatusCodes.Rejected,
                    WorkflowActionCodes.Cancel => WorkflowInstanceStatusCodes.Cancelled,
                    _ => WorkflowInstanceStatusCodes.Completed
                };
                task.WorkflowInstance.WorkflowInstanceStatusId = await GetStatusIdAsync("WORKFLOW_INSTANCE_STATUS", instanceStatus);
                task.WorkflowInstance.CompletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task ClaimTaskAsync(long taskId, long userId)
    {
        // 1. Check eligibility: user must be an assignee
        var isAssignee = await _db.WorkflowTaskAssignees
            .AnyAsync(a => a.WorkflowTaskId == taskId && a.UserId == userId);
        
        if (!isAssignee)
            throw new UnauthorizedAccessException($"User {userId} is not an assignee for task {taskId}.");

        // 2. Atomic claim with ExecuteUpdate
        var availableStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Available);
        var claimedStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Claimed);

        var affected = await _db.WorkflowTasks
            .Where(t => t.WorkflowTaskId == taskId && t.ClaimedByUserId == null && t.WorkflowTaskStatusId == availableStatusId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.ClaimedByUserId, userId)
                .SetProperty(t => t.ClaimedAt, DateTime.UtcNow)
                .SetProperty(t => t.WorkflowTaskStatusId, claimedStatusId));

        if (affected == 0)
        {
            var task = await _db.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(t => t.WorkflowTaskId == taskId);
            if (task == null) throw new KeyNotFoundException("Task not found.");
            if (task.ClaimedByUserId != null) throw new InvalidOperationException("Task already claimed.");
            if (task.WorkflowTaskStatusId != availableStatusId) throw new InvalidOperationException("Task is not available for claiming.");
        }
    }

    private async Task CreateTasksForStepAsync(WorkflowInstance instance, WorkflowStep step)
    {
        var taskStatusId = await GetStatusIdAsync("WORKFLOW_TASK_STATUS", WorkflowTaskStatusCodes.Pending);
        
        var task = new WorkflowTask
        {
            WorkflowInstanceId = instance.WorkflowInstanceId,
            WorkflowStepId = step.WorkflowStepId,
            WorkflowTaskStatusId = taskStatusId,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkflowTasks.Add(task);
        
        // Resolve Assignees
        // 1. Check for Manual Assignments for this specific instance and step
        var manualAssignments = await _db.WorkflowInstanceManualAssignments
            .Where(ma => ma.WorkflowInstanceId == instance.WorkflowInstanceId && ma.WorkflowStepId == step.WorkflowStepId)
            .Select(ma => ma.UserId)
            .ToListAsync();

        if (manualAssignments.Any())
        {
            var statusId = await GetStatusIdAsync("WORKFLOW_TASK_ASSIGNEE_STATUS", WorkflowTaskAssigneeStatusCodes.Pending);
            foreach (var uid in manualAssignments)
            {
                task.Assignees.Add(new WorkflowTaskAssignee
                {
                    UserId = uid,
                    AssigneeStatusId = statusId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // 2. Resolve Assignees based on rules
            var rule = await _db.WorkflowStepRules.FirstOrDefaultAsync(r => r.WorkflowStepId == step.WorkflowStepId);
            if (rule != null)
            {
                var mode = await _db.WorkflowAssignmentModes.FindAsync(rule.AssignmentModeId);
                var userIds = new List<long>();

                if (mode != null)
                {
                    // Pre-fetch initiator contexts if needed
                    var initiatorRoles = new List<long>();
                    var initiatorDepts = new List<long>();

                    if (mode.Code.Contains("REQ") || mode.Code == WorkflowAssignmentModeCodes.Requestor)
                    {
                        var initiator = await _db.Users
                            .Include(u => u.Roles)
                            .Include(u => u.Departments)
                            .FirstOrDefaultAsync(u => u.UserId == instance.InitiatorUserId);
                        
                        if (initiator != null)
                        {
                            initiatorRoles = initiator.Roles.Select(r => r.RoleId).ToList();
                            initiatorDepts = initiator.Departments.Select(d => d.DepartmentId).ToList();
                        }
                    }

                    var query = _db.Users.AsQueryable();

                    switch (mode.Code)
                    {
                        case WorkflowAssignmentModeCodes.Requestor:
                            query = query.Where(u => u.UserId == instance.InitiatorUserId);
                            break;

                        case WorkflowAssignmentModeCodes.Role:
                            if (rule.RoleId.HasValue)
                                query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId));
                            break;

                        case WorkflowAssignmentModeCodes.Department:
                            if (rule.DepartmentId.HasValue)
                                query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId));
                            break;

                        case WorkflowAssignmentModeCodes.RoleAndDepartment:
                            if (rule.RoleId.HasValue)
                                query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId));
                            if (rule.DepartmentId.HasValue)
                                query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId));
                            break;

                        case WorkflowAssignmentModeCodes.RequestorDepartment:
                            if (initiatorDepts.Any())
                                query = query.Where(u => u.Departments.Any(d => initiatorDepts.Contains(d.DepartmentId)));
                            break;
                            
                        case WorkflowAssignmentModeCodes.RequestorRole:
                            if (initiatorRoles.Any())
                                query = query.Where(u => u.Roles.Any(r => initiatorRoles.Contains(r.RoleId)));
                            break;

                        case WorkflowAssignmentModeCodes.RequestorRoleAndDepartment:
                            // Ambiguous: "Role X in Req Dept" vs "Req Role in Req Dept". 
                            // Assuming "Defined Role X in Requestor's Department" (e.g. My Manager)
                            if (rule.RoleId.HasValue)
                                query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId));
                            
                            if (initiatorDepts.Any())
                                query = query.Where(u => u.Departments.Any(d => initiatorDepts.Contains(d.DepartmentId)));
                            break;
                    }

                    userIds = await query.Select(u => u.UserId).ToListAsync();
                }

                // Fallback: If no users found (and not explicit Requestor mode), assign to Admin as fail-safe or leave empty?
                // For now, leaving empty means no one assigned, which is a stall.
                
                var statusId = await GetStatusIdAsync("WORKFLOW_TASK_ASSIGNEE_STATUS", WorkflowTaskAssigneeStatusCodes.Pending);
                
                foreach (var uid in userIds.Distinct())
                {
                    task.Assignees.Add(new WorkflowTaskAssignee
                    {
                        UserId = uid,
                        AssigneeStatusId = statusId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private async Task<long> GetStatusIdAsync(string table, string code)
    {
        var id = table switch
        {
            "WORKFLOW_TASK_STATUS" => (await _db.WorkflowTaskStatuses.FirstOrDefaultAsync(s => s.Code == code))?.WorkflowTaskStatusId,
            "WORKFLOW_TASK_ASSIGNEE_STATUS" => (await _db.WorkflowTaskAssigneeStatuses.FirstOrDefaultAsync(s => s.Code == code))?.AssigneeStatusId,
            "WORKFLOW_INSTANCE_STATUS" => (await _db.WorkflowInstanceStatuses.FirstOrDefaultAsync(s => s.Code == code))?.WorkflowInstanceStatusId,
            _ => throw new ArgumentException($"Unknown table {table}")
        };

        if (id == null) throw new InvalidOperationException($"{table} code '{code}' not found.");
        return id.Value;
    }

    private async Task<long> GetTypeIdAsync(string table, string code)
    {
        var id = table switch
        {
            "WORKFLOW_ACTION_TYPE" => (await _db.WorkflowActionTypes.FirstOrDefaultAsync(t => t.Code == code))?.WorkflowActionTypeId,
            "WORKFLOW_STEP_TYPE" => (await _db.WorkflowStepTypes.FirstOrDefaultAsync(t => t.Code == code))?.WorkflowStepTypeId,
            _ => throw new ArgumentException($"Unknown table {table}")
        };

        if (id == null) throw new InvalidOperationException($"{table} code '{code}' not found.");
        return id.Value;
    }
}
