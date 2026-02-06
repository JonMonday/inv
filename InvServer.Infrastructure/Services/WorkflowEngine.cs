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

    public async Task<long> StartWorkflowAsync(
        long templateId,
        string businessEntityKey,
        long initiatorUserId,
        List<WorkflowManualAssignmentDto>? manualAssignments = null)
    {
        var template = await _db.WorkflowTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);

        if (template == null) throw new InvalidOperationException("Template not found.");
        if (!template.IsActive) throw new InvalidOperationException("Template is inactive.");
        if (template.Status != WorkflowTemplateStatuses.Published)
            throw new InvalidOperationException("You can only start instances from a PUBLISHED template.");

        var startStep = await _db.WorkflowSteps.AsNoTracking()
            .Where(s => s.WorkflowTemplateId == templateId && s.IsActive)
            .OrderBy(s => s.SequenceNo)
            .FirstOrDefaultAsync();

        if (startStep == null) throw new InvalidOperationException("Template has no active steps.");

        var activeInstanceStatusId = await GetInstanceStatusIdAsync(WorkflowInstanceStatusCodes.Active);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var instance = new WorkflowInstance
        {
            WorkflowTemplateId = templateId,
            WorkflowInstanceStatusId = activeInstanceStatusId,
            InitiatorUserId = initiatorUserId,
            BusinessEntityKey = businessEntityKey,
            CurrentWorkflowStepId = startStep.WorkflowStepId,
            StartedAt = DateTime.UtcNow
        };

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync();

        // Manual assignments (optional) - validated against step rules
        if (manualAssignments != null && manualAssignments.Any())
        {
            // Ensure manual assignments refer to steps within this template
            var templateStepIds = await _db.WorkflowSteps
                .AsNoTracking()
                .Where(s => s.WorkflowTemplateId == templateId)
                .Select(s => s.WorkflowStepId)
                .ToListAsync();

            var stepIdSet = templateStepIds.ToHashSet();

            var rulesByStepId = await _db.WorkflowStepRules
                .AsNoTracking()
                .Where(r => stepIdSet.Contains(r.WorkflowStepId))
                .ToDictionaryAsync(r => r.WorkflowStepId);

            foreach (var ma in manualAssignments)
            {
                if (!stepIdSet.Contains(ma.WorkflowStepId))
                    throw new InvalidOperationException($"Manual assignment references a step outside this template: {ma.WorkflowStepId}");

                if (!rulesByStepId.TryGetValue(ma.WorkflowStepId, out var stepRule) || !stepRule.AllowRequesterSelect)
                    throw new InvalidOperationException($"Step {ma.WorkflowStepId} does not allow requester selection.");

                if (ma.UserIds == null || ma.UserIds.Count == 0)
                    throw new InvalidOperationException($"Step {ma.WorkflowStepId} requires at least one assignee.");

                // replace existing manual assignments for this (instance, step)
                var existing = await _db.WorkflowInstanceManualAssignments
                    .Where(x => x.WorkflowInstanceId == instance.WorkflowInstanceId && x.WorkflowStepId == ma.WorkflowStepId)
                    .ToListAsync();

                if (existing.Any())
                    _db.WorkflowInstanceManualAssignments.RemoveRange(existing);

                foreach (var uid in ma.UserIds.Distinct())
                {
                    var eligible = await IsUserEligibleForStepAsync(ma.WorkflowStepId, uid, stepRule, initiatorUserId);
                    if (!eligible)
                        throw new InvalidOperationException($"User {uid} is not eligible for step {ma.WorkflowStepId}.");

                    _db.WorkflowInstanceManualAssignments.Add(new WorkflowInstanceManualAssignment
                    {
                        WorkflowInstanceId = instance.WorkflowInstanceId,
                        WorkflowStepId = ma.WorkflowStepId,
                        UserId = uid
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        await CreateTaskForStepAsync(instance.WorkflowInstanceId, startStep.WorkflowStepId, initiatorUserId);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();
        return instance.WorkflowInstanceId;
    }

    public async Task ClaimTaskAsync(long taskId, long userId)
    {
        var isAssignee = await _db.WorkflowTaskAssignees
            .AnyAsync(a => a.WorkflowTaskId == taskId && a.UserId == userId);

        if (!isAssignee)
            throw new UnauthorizedAccessException("You are not an assignee for this task.");

        var availableId = await GetTaskStatusIdAsync(WorkflowTaskStatusCodes.Available);
        var claimedId = await GetTaskStatusIdAsync(WorkflowTaskStatusCodes.Claimed);

        var affected = await _db.WorkflowTasks
            .Where(t => t.WorkflowTaskId == taskId && t.ClaimedByUserId == null && t.WorkflowTaskStatusId == availableId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.ClaimedByUserId, userId)
                .SetProperty(t => t.ClaimedAt, DateTime.UtcNow)
                .SetProperty(t => t.WorkflowTaskStatusId, claimedId));

        if (affected == 0)
        {
            var task = await _db.WorkflowTasks.AsNoTracking().FirstOrDefaultAsync(t => t.WorkflowTaskId == taskId);
            if (task == null) throw new KeyNotFoundException("Task not found.");
            if (task.ClaimedByUserId != null) throw new InvalidOperationException("Task already claimed.");
            if (task.WorkflowTaskStatusId != availableId) throw new InvalidOperationException("Task is not available for claiming.");
        }
    }

    public async Task ProcessActionAsync(
        long taskId,
        string actionCode,
        long userId,
        string? notes = null,
        string? payloadJson = null,
        string? idempotencyKey = null,
        long? nextAssigneeUserId = null)
    {
        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowInstance)
            .Include(t => t.WorkflowTaskStatus)
            .Include(t => t.WorkflowStep)
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == taskId);

        if (task == null) throw new KeyNotFoundException("Task not found.");

        // must be active task
        if (task.WorkflowTaskStatus.IsTerminal)
            throw new InvalidOperationException("Task is already terminal.");

        // cancellation rule: requestor only, and only at submission step (sequence 0)
        if (actionCode == WorkflowActionCodes.Cancel)
        {
            if (task.WorkflowInstance.InitiatorUserId != userId)
                throw new UnauthorizedAccessException("Only the requestor can cancel.");

            var stepSeq = await _db.WorkflowSteps.Where(s => s.WorkflowStepId == task.WorkflowStepId).Select(s => s.SequenceNo).FirstAsync();
            if (stepSeq != 0)
                throw new InvalidOperationException("Cancellation is only allowed at the submission step.");
        }

        // assignee/claimer enforcement
        if (task.ClaimedByUserId != null && task.ClaimedByUserId != userId)
            throw new InvalidOperationException("Task is already claimed by another user.");

        if (task.ClaimedByUserId == null)
        {
            var isAssignee = task.Assignees.Any(a => a.UserId == userId);
            if (!isAssignee)
                throw new UnauthorizedAccessException("You are not assigned to this task.");

            // auto-claim on first action
            task.ClaimedByUserId = userId;
            task.ClaimedAt = DateTime.UtcNow;
        }

        var actionTypeId = await GetActionTypeIdAsync(actionCode);

        // write action (with idempotency key)
        _db.WorkflowTaskActions.Add(new WorkflowTaskAction
        {
            WorkflowTaskId = taskId,
            WorkflowActionTypeId = actionTypeId,
            ActionByUserId = userId,
            ActionAt = DateTime.UtcNow,
            Notes = notes,
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey
        });

        // assignee status update (if exists)
        var assignee = task.Assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignee != null)
        {
            var assigneeStatusCode = actionCode switch
            {
                WorkflowActionCodes.Reject => WorkflowTaskAssigneeStatusCodes.Rejected,
                _ => WorkflowTaskAssigneeStatusCodes.Approved
            };

            assignee.AssigneeStatusId = await GetAssigneeStatusIdAsync(assigneeStatusCode);
            assignee.DecidedAt = DateTime.UtcNow;
        }

        // completion evaluation
        var rule = await _db.WorkflowStepRules.AsNoTracking().FirstOrDefaultAsync(r => r.WorkflowStepId == task.WorkflowStepId);

        bool isStepComplete;
        if (actionCode == WorkflowActionCodes.Reject || actionCode == WorkflowActionCodes.Cancel)
        {
            isStepComplete = true;
        }
        else if (rule == null)
        {
            isStepComplete = true;
        }
        else
        {
            var approvedId = await GetAssigneeStatusIdAsync(WorkflowTaskAssigneeStatusCodes.Approved);
            var approvedCount = task.Assignees.Count(a => a.AssigneeStatusId == approvedId);

            isStepComplete = rule.RequireAll
                ? approvedCount >= task.Assignees.Count
                : approvedCount >= rule.MinApprovers;
        }

        if (!isStepComplete)
        {
            await _db.SaveChangesAsync();
            return;
        }

        // mark task terminal
        var newTaskStatus = actionCode switch
        {
            WorkflowActionCodes.Reject => WorkflowTaskStatusCodes.Rejected,
            WorkflowActionCodes.Cancel => WorkflowTaskStatusCodes.Cancelled,
            _ => WorkflowTaskStatusCodes.Completed
        };

        task.WorkflowTaskStatusId = await GetTaskStatusIdAsync(newTaskStatus);
        task.CompletedAt = DateTime.UtcNow;

        // routing
        if (actionCode == WorkflowActionCodes.Cancel)
        {
            task.WorkflowInstance.WorkflowInstanceStatusId = await GetInstanceStatusIdAsync(WorkflowInstanceStatusCodes.Cancelled);
            task.WorkflowInstance.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        if (actionCode == WorkflowActionCodes.Reject)
        {
            // per your rule: rejection ALWAYS goes back to submission (lowest sequence)
            var submissionStepId = await _db.WorkflowSteps
                .Where(s => s.WorkflowTemplateId == task.WorkflowInstance.WorkflowTemplateId && s.IsActive)
                .OrderBy(s => s.SequenceNo)
                .Select(s => s.WorkflowStepId)
                .FirstAsync();

            task.WorkflowInstance.CurrentWorkflowStepId = submissionStepId;

            await CreateTaskForStepAsync(task.WorkflowInstanceId, submissionStepId, task.WorkflowInstance.InitiatorUserId);
            await _db.SaveChangesAsync();
            return;
        }

        // normal transition (Approve/Submit/Complete/etc)
        var transition = await _db.WorkflowTransitions
            .AsNoTracking()
            .FirstOrDefaultAsync(tr =>
                tr.WorkflowTemplateId == task.WorkflowInstance.WorkflowTemplateId &&
                tr.FromWorkflowStepId == task.WorkflowStepId &&
                tr.WorkflowActionTypeId == actionTypeId);

        if (transition == null)
        {
            // no transition = complete instance
            task.WorkflowInstance.WorkflowInstanceStatusId = await GetInstanceStatusIdAsync(WorkflowInstanceStatusCodes.Completed);
            task.WorkflowInstance.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        task.WorkflowInstance.CurrentWorkflowStepId = transition.ToWorkflowStepId;

        // If the next step supports requester selection, persist the selected assignee (or ensure one exists)
        await ApplyNextStepManualAssignmentAsync(
            task.WorkflowInstanceId,
            transition.ToWorkflowStepId,
            task.WorkflowInstance.InitiatorUserId,
            nextAssigneeUserId);

        // create next task
        await CreateTaskForStepAsync(task.WorkflowInstanceId, transition.ToWorkflowStepId, task.WorkflowInstance.InitiatorUserId);
        await _db.SaveChangesAsync();
    }

    private async Task CreateTaskForStepAsync(long instanceId, long stepId, long initiatorUserId)
    {
        var step = await _db.WorkflowSteps.AsNoTracking().FirstAsync(s => s.WorkflowStepId == stepId);

        var taskStatusId = await GetTaskStatusIdAsync(WorkflowTaskStatusCodes.Pending);

        var rule = await _db.WorkflowStepRules.AsNoTracking()
            .Include(r => r.AssignmentMode)
            .FirstOrDefaultAsync(r => r.WorkflowStepId == stepId);

        var dueAt = (rule?.SLA_Minutes != null) ? DateTime.UtcNow.AddMinutes(rule.SLA_Minutes.Value) : (DateTime?)null;

        var task = new WorkflowTask
        {
            WorkflowInstanceId = instanceId,
            WorkflowStepId = stepId,
            WorkflowTaskStatusId = taskStatusId,
            CreatedAt = DateTime.UtcNow,
            DueAt = dueAt
        };

        _db.WorkflowTasks.Add(task);
        await _db.SaveChangesAsync();

        // Manual assignment wins
        var manualUserIds = await _db.WorkflowInstanceManualAssignments
            .Where(ma => ma.WorkflowInstanceId == instanceId && ma.WorkflowStepId == stepId)
            .Select(ma => ma.UserId)
            .ToListAsync();

        List<long> assigneeUserIds;
        if (manualUserIds.Any())
        {
            assigneeUserIds = manualUserIds;
        }
        else
        {
            assigneeUserIds = await ResolveAssigneesForStepAsync(stepId, rule, initiatorUserId);
        }

        var assigneeStatusId = await GetAssigneeStatusIdAsync(WorkflowTaskAssigneeStatusCodes.Pending);

        foreach (var uid in assigneeUserIds.Distinct())
        {
            _db.WorkflowTaskAssignees.Add(new WorkflowTaskAssignee
            {
                WorkflowTaskId = task.WorkflowTaskId,
                UserId = uid,
                AssigneeStatusId = assigneeStatusId,
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<List<long>> ResolveAssigneesForStepAsync(long stepId, WorkflowStepRule? rule, long initiatorUserId)
    {
        if (rule == null) return new List<long> { initiatorUserId };

        var modeCode = await _db.WorkflowAssignmentModes
            .Where(m => m.AssignmentModeId == rule.AssignmentModeId)
            .Select(m => m.Code)
            .FirstOrDefaultAsync();

        if (modeCode == WorkflowAssignmentModeCodes.Requestor)
            return new List<long> { initiatorUserId };

        var users = _db.Users.AsNoTracking().Where(u => u.IsActive);

        // Resolve dynamic constraints based on assignment mode
        long? requiredDeptId = rule.DepartmentId;
        HashSet<long>? requiredRoleIds = null;

        if (modeCode == WorkflowAssignmentModeCodes.RequestorDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
        }
        else if (modeCode == WorkflowAssignmentModeCodes.RequestorRole)
        {
            requiredRoleIds = await GetRoleIdsForUserAsync(initiatorUserId);
        }
        else if (modeCode == WorkflowAssignmentModeCodes.RequestorRoleAndDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
            requiredRoleIds = await GetRoleIdsForUserAsync(initiatorUserId);
        }

        // If a rule explicitly says "use requester department", it overrides the department constraint
        if (rule.UseRequesterDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
        }

        // Static constraints from rule
        if (rule.RoleId.HasValue)
        {
            var staticRole = new HashSet<long> { rule.RoleId.Value };
            requiredRoleIds = requiredRoleIds == null
                ? staticRole
                : requiredRoleIds.Intersect(staticRole).ToHashSet();
        }

        if (requiredRoleIds != null && requiredRoleIds.Any())
            users = users.Where(u => u.Roles.Any(r => requiredRoleIds.Contains(r.RoleId)));

        if (requiredDeptId.HasValue)
            users = users.Where(u => u.Departments.Any(d => d.DepartmentId == requiredDeptId.Value));

        var resolved = await users.Select(u => u.UserId).ToListAsync();

        // fallback (don't stall the workflow)
        return resolved.Any() ? resolved : new List<long> { initiatorUserId };
    }

    private async Task ApplyNextStepManualAssignmentAsync(
        long instanceId,
        long toStepId,
        long initiatorUserId,
        long? nextAssigneeUserId)
    {
        var rule = await _db.WorkflowStepRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.WorkflowStepId == toStepId);

        // If there's no rule, there's nothing to manually assign.
        if (rule == null) return;

        if (!rule.AllowRequesterSelect)
        {
            // ignore unless caller tries to force it
            if (nextAssigneeUserId.HasValue)
                throw new InvalidOperationException("This next step does not allow manual assignee selection.");
            return;
        }

        // If a manual assignment already exists (chosen earlier by the requester), we don't require a new selection.
        var existingUserIds = await _db.WorkflowInstanceManualAssignments
            .Where(x => x.WorkflowInstanceId == instanceId && x.WorkflowStepId == toStepId)
            .Select(x => x.UserId)
            .ToListAsync();

        if (!nextAssigneeUserId.HasValue)
        {
            if (existingUserIds.Any()) return;
            throw new InvalidOperationException("Next assignee is required for the next step.");
        }

        var eligible = await IsUserEligibleForStepAsync(toStepId, nextAssigneeUserId.Value, rule, initiatorUserId);
        if (!eligible)
            throw new InvalidOperationException("Selected next assignee is not eligible for the next step.");

        // Replace any existing manual assignments for that step (single assignee selection)
        var existing = await _db.WorkflowInstanceManualAssignments
            .Where(x => x.WorkflowInstanceId == instanceId && x.WorkflowStepId == toStepId)
            .ToListAsync();

        if (existing.Any())
            _db.WorkflowInstanceManualAssignments.RemoveRange(existing);

        _db.WorkflowInstanceManualAssignments.Add(new WorkflowInstanceManualAssignment
        {
            WorkflowInstanceId = instanceId,
            WorkflowStepId = toStepId,
            UserId = nextAssigneeUserId.Value
        });
    }

    private async Task<bool> IsUserEligibleForStepAsync(long stepId, long candidateUserId, WorkflowStepRule rule, long initiatorUserId)
    {
        // Requestor mode always maps to initiator
        var modeCode = await _db.WorkflowAssignmentModes
            .Where(m => m.AssignmentModeId == rule.AssignmentModeId)
            .Select(m => m.Code)
            .FirstOrDefaultAsync();

        if (modeCode == WorkflowAssignmentModeCodes.Requestor)
            return candidateUserId == initiatorUserId;

        var query = _db.Users.AsNoTracking().Where(u => u.IsActive && u.UserId == candidateUserId);

        long? requiredDeptId = rule.DepartmentId;
        HashSet<long>? requiredRoleIds = null;

        if (modeCode == WorkflowAssignmentModeCodes.RequestorDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
        }
        else if (modeCode == WorkflowAssignmentModeCodes.RequestorRole)
        {
            requiredRoleIds = await GetRoleIdsForUserAsync(initiatorUserId);
        }
        else if (modeCode == WorkflowAssignmentModeCodes.RequestorRoleAndDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
            requiredRoleIds = await GetRoleIdsForUserAsync(initiatorUserId);
        }

        if (rule.UseRequesterDepartment)
        {
            requiredDeptId = await GetPrimaryDepartmentIdAsync(initiatorUserId);
        }

        if (rule.RoleId.HasValue)
        {
            var staticRole = new HashSet<long> { rule.RoleId.Value };
            requiredRoleIds = requiredRoleIds == null
                ? staticRole
                : requiredRoleIds.Intersect(staticRole).ToHashSet();
        }

        if (requiredRoleIds != null && requiredRoleIds.Any())
            query = query.Where(u => u.Roles.Any(r => requiredRoleIds.Contains(r.RoleId)));

        if (requiredDeptId.HasValue)
            query = query.Where(u => u.Departments.Any(d => d.DepartmentId == requiredDeptId.Value));

        return await query.AnyAsync();
    }

    private async Task<long?> GetPrimaryDepartmentIdAsync(long userId)
    {
        var deptId = await _db.UserDepartments
            .AsNoTracking()
            .Where(ud => ud.UserId == userId)
            .OrderByDescending(ud => ud.IsPrimary)
            .Select(ud => (long?)ud.DepartmentId)
            .FirstOrDefaultAsync();

        return deptId;
    }

    private async Task<HashSet<long>> GetRoleIdsForUserAsync(long userId)
    {
        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        return roleIds.ToHashSet();
    }

    private async Task<long> GetTaskStatusIdAsync(string code)
    {
        var id = await _db.WorkflowTaskStatuses.Where(s => s.Code == code).Select(s => s.WorkflowTaskStatusId).FirstOrDefaultAsync();
        if (id == 0) throw new InvalidOperationException($"WORKFLOW_TASK_STATUS '{code}' not seeded.");
        return id;
    }

    private async Task<long> GetAssigneeStatusIdAsync(string code)
    {
        var id = await _db.WorkflowTaskAssigneeStatuses.Where(s => s.Code == code).Select(s => s.AssigneeStatusId).FirstOrDefaultAsync();
        if (id == 0) throw new InvalidOperationException($"WORKFLOW_TASK_ASSIGNEE_STATUS '{code}' not seeded.");
        return id;
    }

    private async Task<long> GetInstanceStatusIdAsync(string code)
    {
        var id = await _db.WorkflowInstanceStatuses.Where(s => s.Code == code).Select(s => s.WorkflowInstanceStatusId).FirstOrDefaultAsync();
        if (id == 0) throw new InvalidOperationException($"WORKFLOW_INSTANCE_STATUS '{code}' not seeded.");
        return id;
    }

    private async Task<long> GetActionTypeIdAsync(string code)
    {
        var id = await _db.WorkflowActionTypes.Where(a => a.Code == code).Select(a => a.WorkflowActionTypeId).FirstOrDefaultAsync();
        if (id == 0) throw new InvalidOperationException($"WORKFLOW_ACTION_TYPE '{code}' not seeded.");
        return id;
    }

}
