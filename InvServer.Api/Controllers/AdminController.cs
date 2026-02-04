using InvServer.Api.Filters;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("workflow_mutation")]
public class AdminController : ControllerBase
{
    private readonly InvDbContext _db;
    private readonly IAuditService _auditService;

    public AdminController(InvDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet("roles")]
    [RequirePermission("role.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetRoles([FromQuery] PagedRequest request)
    {
        var query = _db.Roles.AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(r => r.Name.Contains(request.SearchTerm) || r.Code.Contains(request.SearchTerm));
        }
            
        var projectedQuery = query.OrderBy(r => r.Name)
            .Select(r => new 
            {
                r.RoleId,
                r.Name, 
                r.Code,
                r.Description,
                r.IsActive,
                UserCount = _db.UserRoles.Count(ur => ur.RoleId == r.RoleId)
            });

        var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("permissions")]
    [RequirePermission("permission.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetPermissions([FromQuery] PagedRequest request)
    {
        var query = _db.Permissions.AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => p.Code.Contains(request.SearchTerm) || p.Name.Contains(request.SearchTerm));
        }

        var projectedQuery = query.OrderBy(p => p.Code)
            .Select(p => new 
            {
                p.PermissionId,
                p.Code,
                p.Name,
                p.Description,
                p.IsActive
            });

        var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("roles/{id}")]
    [RequirePermission("role.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetRole(long id)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == id);
        if (role == null) return NotFound();
        return Ok(new ApiResponse<object> { Data = role });
    }

    [HttpPost("roles")]
    [RequirePermission("role.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var role = new Role { Name = request.Name, Code = request.Code, IsActive = true };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<long> { Data = role.RoleId });
    }

    [HttpPut("roles/{id}")]
    [RequirePermission("role.update")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        role.Name = request.Name;
        role.Code = request.Code;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<string> { Data = "Role updated." });
    }

    [HttpGet("users")]
    [RequirePermission("user.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetUsers([FromQuery] PagedRequest request)
    {
        var query = _db.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(u => u.Username.Contains(request.SearchTerm) || u.DisplayName.Contains(request.SearchTerm) || u.Email.Contains(request.SearchTerm));
        }

        var projectedQuery = query.OrderBy(u => u.Username)
            .Select(u => new 
            {
                 u.UserId, 
                 u.Username, 
                 u.Email, 
                 u.DisplayName, 
                 u.IsActive,
                 Roles = u.Roles.Select(r => r.Role.Name)
            });

        var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("users/{id}")]
    [RequirePermission("user.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetUser(long id)
    {
        var user = await _db.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();
        
        var result = new 
        {
             user.UserId, user.Username, user.Email, user.DisplayName, user.IsActive,
             Roles = user.Roles.Select(r => r.Role.Name),
             RoleIds = user.Roles.Select(r => r.RoleId)
        };
        return Ok(new ApiResponse<object> { Data = result });
    }

    [HttpPost("users")]
    [RequirePermission("user.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new ApiErrorResponse { Message = "Username already exists." });

        var user = new User 
        { 
            Username = request.Username, 
            Email = request.Email, 
            DisplayName = request.DisplayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        // In real app, hash password here. Skipping for brevity/demo focus.
        user.PasswordHash = "HASHED_DEFAULT"; 

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach(var rid in request.RoleIds)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid });
            }
            await _db.SaveChangesAsync();
        }

        return Ok(new ApiResponse<long> { Data = user.UserId });
    }

    [HttpPut("users/{id}")]
    [RequirePermission("user.update")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        user.Email = request.Email;
        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;

        if (request.RoleIds != null)
        {
            _db.UserRoles.RemoveRange(user.Roles);
            foreach(var rid in request.RoleIds)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid });
            }
            // Bump perm version to force re-login/refresh
            user.PermVersion++;
        }

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<string> { Data = "User updated." });
    }

    [HttpPost("roles/{id}/permissions")]
    [RequirePermission("role_permission.grant")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateRolePermissions(long id, [FromBody] List<long> permissionIds)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == id);
        if (role == null) return NotFound();

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        long.TryParse(userIdStr, out var userId);

        var oldPermissions = role.RolePermissions.Select(p => p.PermissionId).ToList();
        
        // Simple sync
        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var pid in permissionIds)
        {
            role.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = pid });
        }

        await _db.SaveChangesAsync();

        // Audit Log with Diff
        await _auditService.LogChangeAsync(userId, "UPDATE_ROLE_PERMISSIONS", 
            new { RoleId = id, Permissions = oldPermissions }, 
            new { RoleId = id, Permissions = permissionIds });

        return Ok(new ApiResponse<string> { Data = "Permissions updated." });
    }

    [HttpPost("workflows/publish")]
    [RequirePermission("workflow_definition_version.publish")]
    public async Task<ActionResult<ApiResponse<long>>> PublishWorkflow([FromBody] WorkflowPublishRequest request)
    {
        var def = await _db.WorkflowDefinitions
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Code == request.WorkflowCode);

        if (def == null) return NotFound();

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        long.TryParse(userIdStr, out var userId);

        var newVersion = new WorkflowDefinitionVersion
        {
            WorkflowDefinitionId = def.WorkflowDefinitionId,
            VersionNo = (def.Versions.Any() ? def.Versions.Max(v => v.VersionNo) : 0) + 1,
            DefinitionJson = request.DefinitionJson,
            IsActive = true,
            PublishedAt = DateTime.UtcNow,
            PublishedByUserId = userId
        };

        // Deactivate old versions
        foreach (var v in def.Versions) v.IsActive = false;

        _db.WorkflowDefinitionVersions.Add(newVersion);
        await _db.SaveChangesAsync();

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var doc = JsonDocument.Parse(request.DefinitionJson);
            var root = doc.RootElement;
            var stepsNode = root.GetProperty("steps");

            var stepTypes = await _db.WorkflowStepTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowStepTypeId);
            var actionTypes = await _db.WorkflowActionTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowActionTypeId);

            var createdSteps = new Dictionary<string, WorkflowStep>();

            // 1. Create Steps & Rules
            foreach (var sNode in stepsNode.EnumerateArray())
            {
                var key = sNode.GetProperty("key").GetString();
                var name = sNode.GetProperty("name").GetString();
                var typeCode = sNode.GetProperty("type").GetString();
                var seq = sNode.GetProperty("sequence").GetInt32();

                var step = new WorkflowStep
                {
                    WorkflowDefinitionVersionId = newVersion.WorkflowDefinitionVersionId,
                    StepKey = key ?? "UNK",
                    Name = name ?? key ?? "Untitled",
                    WorkflowStepTypeId = stepTypes.ContainsKey(typeCode ?? "") ? stepTypes[typeCode!] : 1,
                    SequenceNo = seq,
                    IsActive = true
                };
                _db.WorkflowSteps.Add(step);
                createdSteps[key!] = step;

                if (sNode.TryGetProperty("rule", out var rNode))
                {
                    var rule = new WorkflowStepRule
                    {
                        WorkflowStep = step,
                        AssignmentModeId = rNode.GetProperty("assignmentMode").GetInt64(),
                        RoleId = rNode.TryGetProperty("roleId", out var rid) && rid.ValueKind != JsonValueKind.Null ? rid.GetInt64() : null,
                        DepartmentId = rNode.TryGetProperty("departmentId", out var did) && did.ValueKind != JsonValueKind.Null ? did.GetInt64() : null,
                        MinApprovers = rNode.TryGetProperty("minApprovers", out var min) ? min.GetInt32() : 1,
                        RequireAll = rNode.TryGetProperty("requireAll", out var ra) && ra.GetBoolean()
                    };
                    _db.WorkflowStepRules.Add(rule);
                }
            }
            await _db.SaveChangesAsync();

            // 2. Create Transitions
            foreach (var sNode in stepsNode.EnumerateArray())
            {
                var fromKey = sNode.GetProperty("key").GetString();
                if (sNode.TryGetProperty("transitions", out var tNode))
                {
                    foreach (var trans in tNode.EnumerateArray())
                    {
                        var actionCode = trans.GetProperty("action").GetString();
                        var toKey = trans.GetProperty("to").GetString();

                        if (createdSteps.ContainsKey(fromKey!) && createdSteps.ContainsKey(toKey!) && actionTypes.ContainsKey(actionCode!))
                        {
                            _db.WorkflowTransitions.Add(new WorkflowTransition
                            {
                                WorkflowDefinitionVersionId = newVersion.WorkflowDefinitionVersionId,
                                FromWorkflowStepId = createdSteps[fromKey!].WorkflowStepId,
                                ToWorkflowStepId = createdSteps[toKey!].WorkflowStepId,
                                WorkflowActionTypeId = actionTypes[actionCode!]
                            });
                        }
                    }
                }
            }
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogChangeAsync(userId, "PUBLISH_WORKFLOW", 
                new { Code = request.WorkflowCode, Version = newVersion.VersionNo - 1 }, 
                new { Code = request.WorkflowCode, Version = newVersion.VersionNo });

            return Ok(new ApiResponse<long> { Data = newVersion.WorkflowDefinitionVersionId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new ApiErrorResponse { Message = "Failed to parse/publish workflow: " + ex.Message });
        }
    }

    [HttpGet("departments")]
    [RequirePermission("department.read")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetDepartments()
    {
        var depts = await _db.Departments.ToListAsync();
        return Ok(new ApiResponse<IEnumerable<object>> { Data = depts });
    }

    [HttpPost("users/{id}/reset-password")]
    [RequirePermission("user.reset_password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // In real app, hash password here.
        user.PasswordHash = "HASHED_" + request.NewPassword; 
        user.PermVersion++; // Force token refresh
        
        await _db.SaveChangesAsync();
        await _auditService.LogChangeAsync(0, "ADMIN_PASSWORD_RESET", new { UserId = id }, new { Action = "Reset" });

        return Ok(new ApiResponse<string> { Data = "Password reset successfully." });
    }

    [HttpGet("audit-logs")]
    [RequirePermission("audit_log.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetAuditLogs([FromQuery] PagedRequest request)
    {
        var query = _db.AuditLogs
            .Include(l => l.User)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(l => l.Action.Contains(request.SearchTerm) || (l.User != null && l.User.Username.Contains(request.SearchTerm)));
        }

        var projectedQuery = query.OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new {
                l.AuditLogId,
                Action = l.Action,
                Timestamp = l.CreatedAtUtc,
                PerformedBy = l.User != null ? l.User.Username : "Unknown",
                Payload = l.PayloadJson
            });

        var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("workflows/{code}")]
    [RequirePermission("workflow_definition.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowTemplate(string code)
    {
        var def = await _db.WorkflowDefinitions
            .Include(d => d.Versions.OrderByDescending(v => v.VersionNo))
            .ThenInclude(v => v.Steps.OrderBy(s => s.SequenceNo))
            .ThenInclude(s => s.StepType)
            .Include(d => d.Versions)
            .ThenInclude(v => v.Steps)
            .ThenInclude(s => s.Rule)
            .Include(d => d.Versions)
            .ThenInclude(v => v.Transitions)
            .FirstOrDefaultAsync(d => d.Code == code);

        if (def == null) return NotFound();

        var latestVersion = def.Versions.OrderByDescending(v => v.VersionNo).FirstOrDefault();
        
        var actionTypes = await _db.WorkflowActionTypes.ToDictionaryAsync(a => a.WorkflowActionTypeId, a => a.Code);

        var steps = latestVersion?.Steps.OrderBy(s => s.SequenceNo).Select(s => new {
            s.WorkflowStepId,
            s.StepKey,
            s.Name,
            s.SequenceNo,
            s.IsSystemRequired,
            StepType = s.StepType.Code,
            Rule = s.Rule != null ? new {
                s.Rule.AssignmentModeId,
                s.Rule.RoleId,
                s.Rule.DepartmentId,
                s.Rule.UseRequesterDepartment,
                s.Rule.AllowRequesterSelect,
                s.Rule.MinApprovers,
                s.Rule.RequireAll
            } : null
        }).ToList();

        var transitions = latestVersion?.Transitions.Select(t => new {
            t.WorkflowTransitionId,
            t.FromWorkflowStepId,
            t.ToWorkflowStepId,
            t.WorkflowActionTypeId,
            ActionCode = actionTypes.ContainsKey(t.WorkflowActionTypeId) ? actionTypes[t.WorkflowActionTypeId] : "UNKNOWN"
        }).ToList();
        
        return Ok(new ApiResponse<object> { 
            Data = new { 
                def.WorkflowDefinitionId, 
                def.Code, 
                def.Name,
                VersionNo = latestVersion?.VersionNo ?? 0,
                Steps = (object?)(steps) ?? new List<object>(),
                Transitions = (object?)(transitions) ?? new List<object>(),
                DefinitionJson = latestVersion?.DefinitionJson ?? "{ \"steps\": [] }"
            } 
        });
    }

    [HttpPost("workflows")]
    [RequirePermission("workflow_definition.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateWorkflowTemplate([FromBody] CreateWorkflowTemplateRequest request)
    {
        if (await _db.WorkflowDefinitions.AnyAsync(d => d.Code == request.Code))
            return BadRequest(new ApiErrorResponse { Message = "Workflow code already exists." });

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var def = new WorkflowDefinition
            {
                Code = request.Code,
                Name = request.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.WorkflowDefinitions.Add(def);
            await _db.SaveChangesAsync();

            // Create v1
            var v1 = new WorkflowDefinitionVersion
            {
                WorkflowDefinitionId = def.WorkflowDefinitionId,
                VersionNo = 1,
                IsActive = true,
                PublishedAt = DateTime.UtcNow,
                DefinitionJson = "{}"
            };
            _db.WorkflowDefinitionVersions.Add(v1);
            await _db.SaveChangesAsync();

            // Fetch Reference Data
            var stepTypes = await _db.WorkflowStepTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowStepTypeId);
            var actionTypes = await _db.WorkflowActionTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowActionTypeId);
            var assignmentModes = await _db.WorkflowAssignmentModes.ToDictionaryAsync(m => m.Code, m => m.AssignmentModeId);
            // Default to first available role if needed (e.g. MANAGER/ADMIN) or generic
            var managerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "MANAGER" || r.Code == "ADMIN");
            
            // Create Default Steps
            var sStart = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "START", Name = "Submission", WorkflowStepTypeId = stepTypes.ContainsKey("START") ? stepTypes["START"] : 1, SequenceNo = 0, IsSystemRequired = true };
            var sFulfill = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "FULFILL", Name = "Fulfillment", WorkflowStepTypeId = stepTypes.ContainsKey("FULFILLMENT") ? stepTypes["FULFILLMENT"] : 1, SequenceNo = 1, IsSystemRequired = true };
            var sConfirm = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "CONFIRM", Name = "Confirmation", WorkflowStepTypeId = stepTypes.ContainsKey("REVIEW") ? stepTypes["REVIEW"] : 1, SequenceNo = 2, IsSystemRequired = true };
            var sEnd = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "END", Name = "End", WorkflowStepTypeId = stepTypes.ContainsKey("END") ? stepTypes["END"] : 1, SequenceNo = 3, IsSystemRequired = true };

            _db.WorkflowSteps.AddRange(sStart, sFulfill, sConfirm, sEnd);
            await _db.SaveChangesAsync();

            // Create Default Rules
            var reqMode = assignmentModes.ContainsKey("REQUESTOR") ? assignmentModes["REQUESTOR"] : 1;
            var storeMode = assignmentModes.ContainsKey("STOREKEEPER") ? assignmentModes["STOREKEEPER"] : 1;

            _db.WorkflowStepRules.Add(new WorkflowStepRule { WorkflowStepId = sStart.WorkflowStepId, AssignmentModeId = reqMode });
            _db.WorkflowStepRules.Add(new WorkflowStepRule { WorkflowStepId = sFulfill.WorkflowStepId, AssignmentModeId = storeMode, RoleId = managerRole?.RoleId });
            _db.WorkflowStepRules.Add(new WorkflowStepRule { WorkflowStepId = sConfirm.WorkflowStepId, AssignmentModeId = reqMode });
            await _db.SaveChangesAsync();

            // Create Default Transitions
            var submit = actionTypes.ContainsKey("SUBMIT") ? actionTypes["SUBMIT"] : 1;
            var complete = actionTypes.ContainsKey("COMPLETE") ? actionTypes["COMPLETE"] : 1;
            var approve = actionTypes.ContainsKey("APPROVE") ? actionTypes["APPROVE"] : 1;
            var cancel = actionTypes.ContainsKey("CANCEL") ? actionTypes["CANCEL"] : 1;

            _db.WorkflowTransitions.AddRange(
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sStart.WorkflowStepId, ToWorkflowStepId = sFulfill.WorkflowStepId, WorkflowActionTypeId = submit },
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sFulfill.WorkflowStepId, ToWorkflowStepId = sConfirm.WorkflowStepId, WorkflowActionTypeId = complete },
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sConfirm.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = approve },
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sStart.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = cancel },
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sFulfill.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = cancel },
                new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sConfirm.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = cancel }
            );
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new ApiResponse<long> { Data = def.WorkflowDefinitionId });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public record ResetPasswordRequest(string NewPassword);
public record WorkflowPublishRequest(string WorkflowCode, string DefinitionJson);
public record CreateRoleRequest(string Name, string Code);
public record UpdateRoleRequest(string Name, string Code);
public record CreateUserRequest(string Username, string Email, string DisplayName, List<long>? RoleIds);
public record UpdateUserRequest(string Email, string DisplayName, bool IsActive, List<long>? RoleIds);
public record CreateWorkflowTemplateRequest(string Name, string Code);
