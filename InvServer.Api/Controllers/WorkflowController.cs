using InvServer.Api.Filters;
using InvServer.Core.Constants;
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

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("workflow_mutation")]
public class WorkflowController : ControllerBase
{
    private readonly InvDbContext _db;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IStockService _stockService;

    public WorkflowController(InvDbContext db, IWorkflowEngine workflowEngine, IStockService stockService)
    {
        _db = db;
        _workflowEngine = workflowEngine;
        _stockService = stockService;
    }

    // --------------------------
    // Helpers
    // --------------------------
    private long GetUserIdOrThrow()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) throw new UnauthorizedAccessException();
        return userId;
    }

    private string? GetIdempotencyHeader()
    {
        if (Request.Headers.TryGetValue("X-Idempotency-Key", out var v) && !string.IsNullOrWhiteSpace(v))
            return v.ToString();
        return null;
    }

    private async Task<(WorkflowTemplate Template, bool HasInstances)> GetTemplateAndInstanceFlag(long templateId)
    {
        var template = await _db.WorkflowTemplates.FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);
        if (template == null) throw new KeyNotFoundException("Template not found.");

        var hasInstances = await _db.WorkflowInstances.AnyAsync(i => i.WorkflowTemplateId == templateId);
        return (template, hasInstances);
    }

    private async Task EnsureTemplateEditable(long templateId)
    {
        var (template, hasInstances) = await GetTemplateAndInstanceFlag(templateId);

        if (template.Status != WorkflowTemplateStatuses.Draft)
            throw new InvalidOperationException("Template is not editable because it is not in DRAFT.");

        if (hasInstances)
            throw new InvalidOperationException("Template is not editable because instances already exist for it.");
    }

    private static long? ExtractRequestId(string businessKey)
    {
        if (string.IsNullOrEmpty(businessKey) || !businessKey.StartsWith("INV_REQ:")) return null;
        if (long.TryParse(businessKey.Substring(8), out var id)) return id;
        return null;
    }

    private decimal GetRequestedQty(long requestId, long productId)
    {
        return _db.InventoryRequestLines
            .Where(l => l.RequestId == requestId && l.ProductId == productId)
            .Select(l => l.QtyRequested)
            .FirstOrDefault();
    }

    // --------------------------
    // Templates
    // --------------------------

    [HttpGet("templates")]
    [RequirePermission("workflow_template.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetTemplates([FromQuery] PagedRequest request)
    {
        var query = _db.WorkflowTemplates
            .AsNoTracking()
            .Select(t => new
            {
                t.WorkflowTemplateId,
                t.Code,
                t.Name,
                t.Status,
                t.IsActive,
                t.CreatedAt,
                t.PublishedAt,
                t.SourceTemplateId
            });

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(x => x.Name.Contains(request.SearchTerm) || x.Code.Contains(request.SearchTerm));

        var paged = await query.OrderBy(x => x.Name).ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("templates/{templateId:long}")]
    [RequirePermission("workflow_template.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetTemplate(long templateId)
    {
        var template = await _db.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.SequenceNo))
                .ThenInclude(s => s.StepType)
            .Include(t => t.Steps)
                .ThenInclude(s => s.Rule)
                    .ThenInclude(r => r.AssignmentMode)
            .Include(t => t.Transitions)
                .ThenInclude(tr => tr.ActionType)
            .Include(t => t.Transitions)
                .ThenInclude(tr => tr.FromStep)
            .Include(t => t.Transitions)
                .ThenInclude(tr => tr.ToStep)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);

        if (template == null) return NotFound();

        var data = new
        {
            template.WorkflowTemplateId,
            template.Code,
            template.Name,
            template.Status,
            template.IsActive,
            template.CreatedAt,
            template.PublishedAt,
            template.SourceTemplateId,
            Steps = template.Steps.OrderBy(s => s.SequenceNo).Select(s => new
            {
                s.WorkflowStepId,
                s.StepKey,
                s.Name,
                StepTypeCode = s.StepType.Code,
                s.SequenceNo,
                s.IsActive,
                s.IsSystemRequired,
                Rule = s.Rule == null ? null : new
                {
                    AssignmentModeCode = s.Rule.AssignmentMode.Code,
                    s.Rule.RoleId,
                    s.Rule.DepartmentId,
                    s.Rule.UseRequesterDepartment,
                    s.Rule.AllowRequesterSelect,
                    s.Rule.MinApprovers,
                    s.Rule.RequireAll,
                    s.Rule.AllowReassign,
                    s.Rule.AllowDelegate,
                    s.Rule.SLA_Minutes
                }
            }),
            Transitions = template.Transitions.Select(tr => new
            {
                tr.WorkflowTransitionId,
                FromStepKey = tr.FromStep.StepKey,
                ActionCode = tr.ActionType.Code,
                ToStepKey = tr.ToStep.StepKey
            })
        };

        return Ok(new ApiResponse<object> { Data = data });
    }

    public record CreateTemplateRequest(string Code, string Name);

    [HttpPost("templates")]
    [RequirePermission("workflow_template.create")]
    public async Task<ActionResult<ApiResponse<object>>> CreateTemplate([FromBody] CreateTemplateRequest req)
    {
        var userId = GetUserIdOrThrow();

        if (await _db.WorkflowTemplates.AnyAsync(t => t.Name == req.Name))
            return Conflict(new ApiErrorResponse { Message = "Template name must be unique." });

        if (await _db.WorkflowTemplates.AnyAsync(t => t.Code == req.Code))
            return Conflict(new ApiErrorResponse { Message = "Template code must be unique." });

        var template = new WorkflowTemplate
        {
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Status = WorkflowTemplateStatuses.Draft,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkflowTemplates.Add(template);
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Data = new { template.WorkflowTemplateId } });
    }

    public record UpdateTemplateMetaRequest(string Code, string Name, bool IsActive);

    [HttpPut("templates/{templateId:long}")]
    [RequirePermission("workflow_template.update")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateTemplateMeta(long templateId, [FromBody] UpdateTemplateMetaRequest req)
    {
        try { await EnsureTemplateEditable(templateId); }
        catch (InvalidOperationException ex) { return Conflict(new ApiErrorResponse { Message = ex.Message }); }

        var template = await _db.WorkflowTemplates.FirstAsync(t => t.WorkflowTemplateId == templateId);

        if (await _db.WorkflowTemplates.AnyAsync(t => t.WorkflowTemplateId != templateId && t.Name == req.Name))
            return Conflict(new ApiErrorResponse { Message = "Template name must be unique." });

        if (await _db.WorkflowTemplates.AnyAsync(t => t.WorkflowTemplateId != templateId && t.Code == req.Code))
            return Conflict(new ApiErrorResponse { Message = "Template code must be unique." });

        template.Name = req.Name.Trim();
        template.Code = req.Code.Trim();
        template.IsActive = req.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<string> { Data = "Template updated." });
    }

    // Relational definition upsert
    public record UpsertTemplateDefinitionRequest(List<TemplateStepDto> Steps, List<TemplateTransitionDto> Transitions);

    public record TemplateStepDto(
        string StepKey,
        string Name,
        string StepTypeCode,
        int SequenceNo,
        bool IsActive = true,
        bool IsSystemRequired = false,
        TemplateStepRuleDto? Rule = null
    );

    public record TemplateStepRuleDto(
        string AssignmentModeCode,
        long? RoleId = null,
        long? DepartmentId = null,
        bool UseRequesterDepartment = false,
        bool AllowRequesterSelect = false,
        int MinApprovers = 1,
        bool RequireAll = false,
        bool AllowReassign = true,
        bool AllowDelegate = true,
        int? SLA_Minutes = null
    );

    public record TemplateTransitionDto(string FromStepKey, string ActionCode, string ToStepKey);

    [HttpPut("templates/{templateId:long}/definition")]
    [RequirePermission("workflow_template.update")]
    public async Task<ActionResult<ApiResponse<string>>> UpsertTemplateDefinition(long templateId, [FromBody] UpsertTemplateDefinitionRequest req)
    {
        try { await EnsureTemplateEditable(templateId); }
        catch (InvalidOperationException ex) { return Conflict(new ApiErrorResponse { Message = ex.Message }); }

        if (req.Steps == null || req.Steps.Count == 0)
            return BadRequest(new ApiErrorResponse { Message = "At least one step is required." });

        var dupKeys = req.Steps.GroupBy(s => s.StepKey.Trim()).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupKeys.Any())
            return BadRequest(new ApiErrorResponse { Message = $"Duplicate StepKey(s): {string.Join(", ", dupKeys)}" });

        var stepTypes = await _db.WorkflowStepTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.WorkflowStepTypeId);
        var actionTypes = await _db.WorkflowActionTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.WorkflowActionTypeId);
        var assignmentModes = await _db.WorkflowAssignmentModes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.AssignmentModeId);

        foreach (var s in req.Steps)
        {
            if (!stepTypes.ContainsKey(s.StepTypeCode))
                return BadRequest(new ApiErrorResponse { Message = $"Unknown StepTypeCode: {s.StepTypeCode}" });

            if (s.Rule != null && !assignmentModes.ContainsKey(s.Rule.AssignmentModeCode))
                return BadRequest(new ApiErrorResponse { Message = $"Unknown AssignmentModeCode: {s.Rule.AssignmentModeCode}" });
        }

        foreach (var tr in req.Transitions ?? new List<TemplateTransitionDto>())
        {
            if (!actionTypes.ContainsKey(tr.ActionCode))
                return BadRequest(new ApiErrorResponse { Message = $"Unknown ActionCode: {tr.ActionCode}" });
        }

        var keys = req.Steps.Select(s => s.StepKey.Trim()).ToHashSet();
        foreach (var tr in req.Transitions ?? new List<TemplateTransitionDto>())
        {
            if (!keys.Contains(tr.FromStepKey.Trim()) || !keys.Contains(tr.ToStepKey.Trim()))
                return BadRequest(new ApiErrorResponse { Message = $"Transition references unknown step key: {tr.FromStepKey} -> {tr.ToStepKey}" });
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        // wipe existing definition
        _db.WorkflowTransitions.RemoveRange(await _db.WorkflowTransitions.Where(x => x.WorkflowTemplateId == templateId).ToListAsync());

        var existingRules = await _db.WorkflowStepRules
            .Where(r => r.WorkflowStep.WorkflowTemplateId == templateId)
            .ToListAsync();
        _db.WorkflowStepRules.RemoveRange(existingRules);

        _db.WorkflowSteps.RemoveRange(await _db.WorkflowSteps.Where(s => s.WorkflowTemplateId == templateId).ToListAsync());
        await _db.SaveChangesAsync();

        // insert steps
        var stepEntities = req.Steps.Select(s => new WorkflowStep
        {
            WorkflowTemplateId = templateId,
            StepKey = s.StepKey.Trim(),
            Name = s.Name.Trim(),
            WorkflowStepTypeId = stepTypes[s.StepTypeCode],
            SequenceNo = s.SequenceNo,
            IsActive = s.IsActive,
            IsSystemRequired = s.IsSystemRequired
        }).ToList();

        _db.WorkflowSteps.AddRange(stepEntities);
        await _db.SaveChangesAsync();

        var stepKeyToId = stepEntities.ToDictionary(s => s.StepKey, s => s.WorkflowStepId);

        // insert rules
        var rules = req.Steps.Where(s => s.Rule != null).Select(s => new WorkflowStepRule
        {
            WorkflowStepId = stepKeyToId[s.StepKey.Trim()],
            AssignmentModeId = assignmentModes[s.Rule!.AssignmentModeCode],
            RoleId = s.Rule.RoleId,
            DepartmentId = s.Rule.DepartmentId,
            UseRequesterDepartment = s.Rule.UseRequesterDepartment,
            AllowRequesterSelect = s.Rule.AllowRequesterSelect,
            MinApprovers = s.Rule.MinApprovers,
            RequireAll = s.Rule.RequireAll,
            AllowReassign = s.Rule.AllowReassign,
            AllowDelegate = s.Rule.AllowDelegate,
            SLA_Minutes = s.Rule.SLA_Minutes
        }).ToList();

        _db.WorkflowStepRules.AddRange(rules);
        await _db.SaveChangesAsync();

        // insert transitions
        var transitions = (req.Transitions ?? new List<TemplateTransitionDto>())
            .Select(tr => new WorkflowTransition
            {
                WorkflowTemplateId = templateId,
                FromWorkflowStepId = stepKeyToId[tr.FromStepKey.Trim()],
                WorkflowActionTypeId = actionTypes[tr.ActionCode],
                ToWorkflowStepId = stepKeyToId[tr.ToStepKey.Trim()]
            })
            .ToList();

        _db.WorkflowTransitions.AddRange(transitions);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return Ok(new ApiResponse<string> { Data = "Template definition saved." });
    }

    [HttpPost("templates/{templateId:long}/publish")]
    [RequirePermission("workflow_template.publish")]
    public async Task<ActionResult<ApiResponse<string>>> PublishTemplate(long templateId)
    {
        var userId = GetUserIdOrThrow();

        var template = await _db.WorkflowTemplates.FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);
        if (template == null) return NotFound();

        if (template.Status != WorkflowTemplateStatuses.Draft)
            return Conflict(new ApiErrorResponse { Message = "Only DRAFT templates can be published." });

        var stepCount = await _db.WorkflowSteps.CountAsync(s => s.WorkflowTemplateId == templateId);
        if (stepCount == 0)
            return BadRequest(new ApiErrorResponse { Message = "Cannot publish: template has no steps." });

        template.Status = WorkflowTemplateStatuses.Published;
        template.PublishedAt = DateTime.UtcNow;
        template.PublishedByUserId = userId;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<string> { Data = "Template published (locked)." });
    }

    public record CloneTemplateRequest(string NewCode, string NewName);

    [HttpPost("templates/{templateId:long}/clone")]
    [RequirePermission("workflow_template.clone")]
    public async Task<ActionResult<ApiResponse<object>>> CloneTemplate(long templateId, [FromBody] CloneTemplateRequest req)
    {
        var userId = GetUserIdOrThrow();

        var source = await _db.WorkflowTemplates
            .Include(t => t.Steps).ThenInclude(s => s.Rule)
            .Include(t => t.Transitions)
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);

        if (source == null) return NotFound();

        if (await _db.WorkflowTemplates.AnyAsync(t => t.Name == req.NewName))
            return Conflict(new ApiErrorResponse { Message = "Template name must be unique." });

        if (await _db.WorkflowTemplates.AnyAsync(t => t.Code == req.NewCode))
            return Conflict(new ApiErrorResponse { Message = "Template code must be unique." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var clone = new WorkflowTemplate
        {
            Code = req.NewCode.Trim(),
            Name = req.NewName.Trim(),
            Status = WorkflowTemplateStatuses.Draft,
            IsActive = true,
            SourceTemplateId = source.WorkflowTemplateId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkflowTemplates.Add(clone);
        await _db.SaveChangesAsync();

        var stepMap = new Dictionary<long, long>(); // oldStepId -> newStepId

        foreach (var s in source.Steps)
        {
            var newStep = new WorkflowStep
            {
                WorkflowTemplateId = clone.WorkflowTemplateId,
                StepKey = s.StepKey,
                Name = s.Name,
                WorkflowStepTypeId = s.WorkflowStepTypeId,
                SequenceNo = s.SequenceNo,
                IsActive = s.IsActive,
                IsSystemRequired = s.IsSystemRequired
            };

            _db.WorkflowSteps.Add(newStep);
            await _db.SaveChangesAsync();

            stepMap[s.WorkflowStepId] = newStep.WorkflowStepId;

            if (s.Rule != null)
            {
                _db.WorkflowStepRules.Add(new WorkflowStepRule
                {
                    WorkflowStepId = newStep.WorkflowStepId,
                    AssignmentModeId = s.Rule.AssignmentModeId,
                    RoleId = s.Rule.RoleId,
                    DepartmentId = s.Rule.DepartmentId,
                    UseRequesterDepartment = s.Rule.UseRequesterDepartment,
                    AllowRequesterSelect = s.Rule.AllowRequesterSelect,
                    MinApprovers = s.Rule.MinApprovers,
                    RequireAll = s.Rule.RequireAll,
                    AllowReassign = s.Rule.AllowReassign,
                    AllowDelegate = s.Rule.AllowDelegate,
                    SLA_Minutes = s.Rule.SLA_Minutes
                });
                await _db.SaveChangesAsync();
            }
        }

        foreach (var tr in source.Transitions)
        {
            _db.WorkflowTransitions.Add(new WorkflowTransition
            {
                WorkflowTemplateId = clone.WorkflowTemplateId,
                FromWorkflowStepId = stepMap[tr.FromWorkflowStepId],
                WorkflowActionTypeId = tr.WorkflowActionTypeId,
                ToWorkflowStepId = stepMap[tr.ToWorkflowStepId]
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new ApiResponse<object> { Data = new { clone.WorkflowTemplateId } });
    }

    // --------------------------
    // Instances
    // --------------------------

    public record StartInstanceRequest(long TemplateId, string BusinessEntityKey, List<WorkflowManualAssignmentDto>? ManualAssignments = null);

    [HttpPost("instances/start")]
    [RequirePermission("workflow_instance.start")]
    public async Task<ActionResult<ApiResponse<object>>> StartInstance([FromBody] StartInstanceRequest req)
    {
        var userId = GetUserIdOrThrow();

        var template = await _db.WorkflowTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == req.TemplateId);

        if (template == null) return NotFound();
        if (!template.IsActive) return Conflict(new ApiErrorResponse { Message = "Template is inactive." });
        if (template.Status != WorkflowTemplateStatuses.Published)
            return Conflict(new ApiErrorResponse { Message = "You can only start instances from a PUBLISHED template." });

        var instanceId = await _workflowEngine.StartWorkflowAsync(req.TemplateId, req.BusinessEntityKey.Trim(), userId, req.ManualAssignments);

        // Return instanceId; tasks are created by engine (start step)
        return Ok(new ApiResponse<object> { Data = new { workflowInstanceId = instanceId } });
    }

    // --------------------------
    // Tasks
    // --------------------------

    [HttpGet("tasks/my")]
    [RequirePermission("workflow_task.read_my")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetMyTasks([FromQuery] PagedRequest request)
    {
        var userId = GetUserIdOrThrow();

        var query = _db.WorkflowTasks
            .Include(t => t.WorkflowStep)
            .Include(t => t.WorkflowInstance)
            .Include(t => t.WorkflowTaskStatus)
            .Include(t => t.ClaimedByUser)
            .Include(t => t.Assignees)
            .Where(t => t.WorkflowTaskStatus.IsTerminal == false)
            .Where(t => t.ClaimedByUserId == userId || t.Assignees.Any(a => a.UserId == userId))
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(t =>
                t.WorkflowStep.Name.Contains(request.SearchTerm) ||
                t.WorkflowInstance.BusinessEntityKey.Contains(request.SearchTerm));
        }

        var results = await query.OrderByDescending(t => t.CreatedAt).ToPagedResponseAsync(request);

        var mappedData = results.Data.Select(t => new
        {
            id = t.WorkflowTaskId,
            workflowInstanceId = t.WorkflowInstanceId,
            title = t.WorkflowStep.Name + " for " + t.WorkflowInstance.BusinessEntityKey,
            description = "Workflow Task: " + t.WorkflowStep.Name,
            status = t.WorkflowTaskStatus.Code,
            createdAt = t.CreatedAt,
            claimedByUserId = t.ClaimedByUserId,
            claimedByUserName = t.ClaimedByUser?.DisplayName,
            requestId = ExtractRequestId(t.WorkflowInstance.BusinessEntityKey),
            initiatorUserId = t.WorkflowInstance.InitiatorUserId,
            stepName = t.WorkflowStep.Name,
            stepKey = t.WorkflowStep.StepKey
        });

        return Ok(new PagedResponse<IEnumerable<object>>(mappedData, results.PageNumber, results.PageSize, results.TotalRecords));
    }

    [HttpPost("tasks/{id}/claim")]
    [RequirePermission("workflow_task.claim")]
    public async Task<ActionResult<ApiResponse<string>>> ClaimTask(long id)
    {
        var userId = GetUserIdOrThrow();

        try
        {
            await _workflowEngine.ClaimTaskAsync(id, userId);
            return Ok(new ApiResponse<string> { Data = "Task claimed successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiErrorResponse { Message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record WorkflowActionRequest(
        string ActionCode,
        string? Notes = null,
        string? PayloadJson = null,
        long? NextAssigneeUserId = null);

    [HttpPost("tasks/{id}/action")]
    [RequirePermission("workflow_task.action")]
    public async Task<ActionResult<ApiResponse<string>>> ProcessAction(long id, [FromBody] WorkflowActionRequest request)
    {
        var userId = GetUserIdOrThrow();
        var idem = GetIdempotencyHeader();

        // Idempotency quick guard (prevents double-click)
        if (!string.IsNullOrEmpty(idem))
        {
            var already = await _db.WorkflowTaskActions.AnyAsync(a => a.WorkflowTaskId == id && a.IdempotencyKey == idem);
            if (already) return Ok(new ApiResponse<string> { Data = "Duplicate ignored (idempotent)." });
        }

        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowStep)
            .Include(t => t.WorkflowInstance)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == id);

        if (task == null) return NotFound();

        var stepKey = task.WorkflowStep.StepKey;
        var requestId = ExtractRequestId(task.WorkflowInstance.BusinessEntityKey);

        // Your stock logic stays here (controller knows about inventory domain)
        var isFulfillmentStep = stepKey == "FULFILL" || stepKey == "FULFILLMENT";
        var isConfirmationStep = stepKey == "CONFIRM" || stepKey == "CONFIRMATION";
        var isApprovalOrCompletion = request.ActionCode == WorkflowActionCodes.Approve || request.ActionCode == WorkflowActionCodes.Complete;

        if (!string.IsNullOrEmpty(request.PayloadJson) && isFulfillmentStep)
        {
            try
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<FulfillmentPayload>(
                    request.PayloadJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload?.Fulfillments != null && payload.Fulfillments.Any() &&
                    requestId.HasValue && isApprovalOrCompletion)
                {
                    foreach (var fulfillment in payload.Fulfillments)
                    {
                        var movementReq = new StockMovementRequest
                        {
                            WarehouseId = fulfillment.WarehouseId,
                            MovementTypeCode = MovementTypeCodes.Reserve,
                            RequestId = requestId.Value,
                            UserId = userId,
                            Notes = "Fulfillment automated reservation",
                            Lines = new List<StockMovementLineRequest>
                            {
                                new StockMovementLineRequest
                                {
                                    ProductId = fulfillment.ProductId,
                                    QtyDeltaReserved = GetRequestedQty(requestId.Value, fulfillment.ProductId)
                                }
                            }
                        };

                        var idemKey = !string.IsNullOrEmpty(idem) ? $"{idem}:fulfill:{id}:{fulfillment.ProductId}" : $"fulfill:{id}:{fulfillment.ProductId}";
                        await _stockService.PostMovementAsync(movementReq, HttpContext.TraceIdentifier, $"workflow:task:{id}", idemKey);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiErrorResponse { Message = $"Fulfillment payload processing failed: {ex.Message}" });
            }
        }

        if (isConfirmationStep && isApprovalOrCompletion && requestId.HasValue)
        {
            try
            {
                var reservedMovements = await _db.StockMovements
                    .Include(m => m.Lines)
                    .Include(m => m.MovementType)
                    .Where(m => m.ReferenceRequestId == requestId.Value && m.MovementType.Code == MovementTypeCodes.Reserve)
                    .ToListAsync();

                var byWarehouse = reservedMovements.GroupBy(m => m.WarehouseId);
                foreach (var group in byWarehouse)
                {
                    var lines = group.SelectMany(m => m.Lines).GroupBy(l => l.ProductId)
                        .Select(g => new StockMovementLineRequest
                        {
                            ProductId = g.Key,
                            QtyDeltaOnHand = -g.Sum(l => l.QtyDeltaReserved), // Deduct physically
                            QtyDeltaReserved = -g.Sum(l => l.QtyDeltaReserved) // Release reservation
                        }).ToList();

                    var issueReq = new StockMovementRequest
                    {
                        WarehouseId = group.Key,
                        MovementTypeCode = MovementTypeCodes.Issue,
                        RequestId = requestId.Value,
                        UserId = userId,
                        Notes = "Requester confirmation - stock issued",
                        Lines = lines
                    };

                    var idemKey = !string.IsNullOrEmpty(idem) ? $"{idem}:confirm:{id}:{group.Key}" : $"confirm:{id}:{group.Key}";
                    await _stockService.PostMovementAsync(issueReq, HttpContext.TraceIdentifier, $"workflow:task:{id}", idemKey);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiErrorResponse { Message = $"Confirmation issuance failed: {ex.Message}" });
            }
        }

        try
        {
            await _workflowEngine.ProcessActionAsync(id, request.ActionCode, userId, request.Notes, request.PayloadJson, idem, request.NextAssigneeUserId);
            return Ok(new ApiResponse<string> { Data = "Action processed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    // Eligible assignees for the *next* step based on an action from the current task
    // (Used when the current actor chooses who will handle the next step.)
    [HttpGet("tasks/{id}/next-eligible-assignees")]
    [RequirePermission("workflow_task.read_eligible_assignees")]
    public async Task<ActionResult<ApiResponse<object>>> GetNextEligibleUsers(long id, [FromQuery] string actionCode)
    {
        var task = await _db.WorkflowTasks
            .AsNoTracking()
            .Include(t => t.WorkflowInstance)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == id);

        if (task == null)
            return NotFound(new ApiErrorResponse { Message = "Task not found." });

        var actionTypeId = await _db.WorkflowActionTypes
            .Where(a => a.Code == actionCode)
            .Select(a => a.WorkflowActionTypeId)
            .FirstOrDefaultAsync();

        if (actionTypeId == 0)
            return BadRequest(new ApiErrorResponse { Message = $"Unknown actionCode '{actionCode}'." });

        var transition = await _db.WorkflowTransitions
            .AsNoTracking()
            .FirstOrDefaultAsync(tr => tr.WorkflowTemplateId == task.WorkflowInstance.WorkflowTemplateId
                                       && tr.FromWorkflowStepId == task.WorkflowStepId
                                       && tr.WorkflowActionTypeId == actionTypeId);

        if (transition == null)
        {
            // No transition means instance would complete; nothing to assign.
            return Ok(new ApiResponse<object>
            {
                Data = new
                {
                    hasNextStep = false,
                    eligibleUsers = new List<object>()
                }
            });
        }

        var toStep = await _db.WorkflowSteps
            .AsNoTracking()
            .Include(s => s.Rule)
            .FirstOrDefaultAsync(s => s.WorkflowStepId == transition.ToWorkflowStepId);

        if (toStep?.Rule == null)
        {
            return Ok(new ApiResponse<object>
            {
                Data = new
                {
                    hasNextStep = true,
                    toStep = new { toStepId = transition.ToWorkflowStepId, name = toStep?.Name },
                    allowRequesterSelect = false,
                    requireSelection = false,
                    eligibleUsers = new List<object>()
                }
            });
        }

        var rule = toStep.Rule;
        var initiatorUserId = task.WorkflowInstance.InitiatorUserId;

        // Determine effective constraints (mirrors WorkflowEngine resolution logic)
        var modeCode = await _db.WorkflowAssignmentModes
            .Where(m => m.AssignmentModeId == rule.AssignmentModeId)
            .Select(m => m.Code)
            .FirstOrDefaultAsync();

        long? requiredDeptId = rule.DepartmentId;
        HashSet<long>? requiredRoleIds = null;

        if (modeCode == WorkflowAssignmentModeCodes.RequestorDepartment || rule.UseRequesterDepartment)
        {
            requiredDeptId = await _db.UserDepartments
                .AsNoTracking()
                .Where(ud => ud.UserId == initiatorUserId)
                .OrderByDescending(ud => ud.IsPrimary)
                .Select(ud => (long?)ud.DepartmentId)
                .FirstOrDefaultAsync();
        }

        if (modeCode == WorkflowAssignmentModeCodes.RequestorRole || modeCode == WorkflowAssignmentModeCodes.RequestorRoleAndDepartment)
        {
            var initiatorRoleIds = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == initiatorUserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            requiredRoleIds = initiatorRoleIds.ToHashSet();
        }

        if (rule.RoleId.HasValue)
        {
            requiredRoleIds = requiredRoleIds == null
                ? new HashSet<long> { rule.RoleId.Value }
                : requiredRoleIds.Intersect(new[] { rule.RoleId.Value }).ToHashSet();
        }

        var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

        if (requiredRoleIds != null && requiredRoleIds.Any())
            query = query.Where(u => u.Roles.Any(r => requiredRoleIds.Contains(r.RoleId)));

        if (requiredDeptId.HasValue)
            query = query.Where(u => u.Departments.Any(d => d.DepartmentId == requiredDeptId.Value));

        var eligibleUsers = await query.Select(u => new
        {
            u.UserId,
            u.Username,
            u.DisplayName,
            Roles = u.Roles.Select(r => r.Role.Name).ToList()
        }).ToListAsync();

        var preselectedUserIds = await _db.WorkflowInstanceManualAssignments
            .AsNoTracking()
            .Where(x => x.WorkflowInstanceId == task.WorkflowInstanceId && x.WorkflowStepId == toStep.WorkflowStepId)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        var requireSelection = rule.AllowRequesterSelect && !preselectedUserIds.Any();

        return Ok(new ApiResponse<object>
        {
            Data = new
            {
                hasNextStep = true,
                toStep = new { toStepId = toStep.WorkflowStepId, toStep.StepKey, toStep.Name },
                allowRequesterSelect = rule.AllowRequesterSelect,
                requireSelection,
                preselectedUserIds,
                eligibleUsers
            }
        });
    }

    // Eligible assignees
    [HttpGet("tasks/{id}/eligible-assignees")]
    [RequirePermission("workflow_task.read_eligible_assignees")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetEligibleUsers(long id)
    {
        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowStep).ThenInclude(s => s.Rule)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == id);

        if (task == null || task.WorkflowStep.Rule == null)
            return Ok(new ApiResponse<IEnumerable<object>> { Data = new List<object>() });

        var rule = task.WorkflowStep.Rule;

        var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

        if (rule.RoleId.HasValue)
            query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId.Value));

        if (rule.DepartmentId.HasValue)
            query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId.Value));

        var results = await query.Select(u => new
        {
            u.UserId,
            u.Username,
            u.DisplayName,
            Roles = u.Roles.Select(r => r.Role.Name).ToList()
        }).ToListAsync();

        return Ok(new ApiResponse<IEnumerable<object>> { Data = results });
    }

    // Assignment options
    [HttpGet("templates/{templateId:long}/assignment-options")]
    [RequirePermission("workflow_template.read")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetAssignmentOptions(long templateId)
    {
        var template = await _db.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.SequenceNo))
                .ThenInclude(s => s.Rule)
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == templateId);

        if (template == null) return NotFound();

        var assignmentModes = await _db.WorkflowAssignmentModes.ToDictionaryAsync(m => m.AssignmentModeId, m => m.Code);

        var result = new List<object>();

        foreach (var step in template.Steps.OrderBy(s => s.SequenceNo))
        {
            var rule = step.Rule;
            if (rule == null) continue;

            var modeCode = assignmentModes.TryGetValue(rule.AssignmentModeId, out var code) ? code : "";

            var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

            if (rule.RoleId.HasValue)
                query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId.Value));

            if (rule.DepartmentId.HasValue)
                query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId.Value));

            var users = await query.Select(u => new
            {
                u.UserId,
                u.DisplayName,
                Role = u.Roles.Select(r => r.Role.Name).FirstOrDefault(),
                Department = u.Departments.Select(d => d.Department.Name).FirstOrDefault()
            }).ToListAsync();

            // Get role and department names
            string? roleName = null;
            string? deptName = null;

            if (rule.RoleId.HasValue)
            {
                var role = await _db.Roles.FindAsync(rule.RoleId.Value);
                roleName = role?.Name;
            }

            if (rule.DepartmentId.HasValue)
            {
                var dept = await _db.Departments.FindAsync(rule.DepartmentId.Value);
                deptName = dept?.Name;
            }

            var modeName = modeCode switch
            {
                "REQ" => "Requestor (Initiator)",
                "ROLE" => "Role Based",
                "DEPT" => "Department Based",
                "ROLE_DEPT" => "Role and Department Based",
                "SPECIFIC" => "Specific User",
                _ => "Unknown"
            };

            result.Add(new
            {
                step.WorkflowStepId,
                step.Name,
                ModeCode = modeCode,
                AssignmentMode = modeName,
                RoleId = rule.RoleId,
                RoleName = roleName,
                DepartmentId = rule.DepartmentId,
                DepartmentName = deptName,
                IsManual = true,
                EligibleUsers = users.Select(u => new
                {
                    u.UserId,
                    u.DisplayName,
                    Description = $"{u.DisplayName} ({u.Role} - {u.Department})"
                })
            });
        }
        return Ok(new ApiResponse<IEnumerable<object>> { Data = result });
    }

    public class FulfillmentPayload
    {
        public List<FulfillmentItem>? Fulfillments { get; set; }
    }

    public class FulfillmentItem
    {
        public long ProductId { get; set; }
        public long WarehouseId { get; set; }
    }
}
