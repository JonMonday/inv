using InvServer.Api.Filters;
using InvServer.Core.Constants;
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

    [HttpGet("tasks/my")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetMyTasks([FromQuery] PagedRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

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
            query = query.Where(t => t.WorkflowStep.Name.Contains(request.SearchTerm) || t.WorkflowInstance.BusinessEntityKey.Contains(request.SearchTerm));
        }

        var results = await query.OrderByDescending(t => t.CreatedAt).ToPagedResponseAsync(request);
        
        // Map to anonymous object for UI
        var mappedData = results.Data.Select(t => new {
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
            stepName = t.WorkflowStep.Name
        });

        return Ok(new PagedResponse<IEnumerable<object>>(mappedData, results.PageNumber, results.PageSize, results.TotalRecords));
    }

    private static long? ExtractRequestId(string businessKey)
    {
        if (string.IsNullOrEmpty(businessKey) || !businessKey.StartsWith("INV_REQ:")) return null;
        if (long.TryParse(businessKey.Substring(8), out var id)) return id;
        return null;
    }

    [HttpPost("tasks/{id}/claim")]
    public async Task<ActionResult<ApiResponse<string>>> ClaimTask(long id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

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

    [HttpPost("tasks/{id}/action")]
    public async Task<ActionResult<ApiResponse<string>>> ProcessAction(long id, [FromBody] WorkflowActionRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        // Get current task to check step name
        var currentTask = await _db.WorkflowTasks
            .Include(t => t.WorkflowStep)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == id);
        
        var stepKey = currentTask?.WorkflowStep?.StepKey;
        var requestId = ExtractRequestIdFromTask(id);

        // Special handling for Fulfillment logic if payload contains fulfillment data
        if (!string.IsNullOrEmpty(request.PayloadJson) && stepKey == "FULFILL")
        {
            Console.WriteLine($"🔍 DEBUG: Received PayloadJson: {request.PayloadJson}");
            Console.WriteLine($"🔍 DEBUG: StepKey: {stepKey}, ActionCode: {request.ActionCode}");
            try
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<FulfillmentPayload>(request.PayloadJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"🔍 DEBUG: Deserialized payload, Fulfillments count: {payload?.Fulfillments?.Count ?? 0}");
                if (payload?.Fulfillments != null && payload.Fulfillments.Any())
                {
                    if (requestId.HasValue && request.ActionCode == WorkflowActionCodes.Approve)
                    {
                        // Group by warehouse to post movements
                        var byWarehouse = payload.Fulfillments.GroupBy(f => f.WarehouseId);
                        foreach (var group in byWarehouse)
                        {
                            var movementReq = new StockMovementRequest
                            {
                                WarehouseId = group.Key,
                                MovementTypeCode = MovementTypeCodes.Reserve,
                                RequestId = requestId.Value,
                                UserId = userId,
                                Notes = "Fulfillment automated reservation",
                                Lines = group.Select(f => new StockMovementLineRequest
                                {
                                    ProductId = f.ProductId,
                                    QtyDeltaReserved = GetRequestedQty(requestId.Value, f.ProductId)
                                }).ToList()
                            };
                            
                            // Post movement (using task ID as idempotency context variant)
                            await _stockService.PostMovementAsync(movementReq, HttpContext.TraceIdentifier, $"workflow:task:{id}", $"fulfill:{id}:{group.Key}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and continue or handle error
                Console.WriteLine($"Fulfillment payload processing failed: {ex.Message}");
            }
        }
        
        // Handle Requestor Confirmation - Issue the reserved stock
        if (stepKey == "CONFIRM" && request.ActionCode == WorkflowActionCodes.Approve && requestId.HasValue)
        {
            try
            {
                // Get all reserved stock movements for this request
                var reservedMovements = await _db.StockMovements
                    .Include(m => m.Lines)
                    .Where(m => m.ReferenceRequestId == requestId.Value && m.MovementType.Code == MovementTypeCodes.Reserve)
                    .ToListAsync();

                // Group by warehouse and issue the stock
                var byWarehouse = reservedMovements.GroupBy(m => m.WarehouseId);
                foreach (var group in byWarehouse)
                {
                    var lines = group.SelectMany(m => m.Lines).GroupBy(l => l.ProductId)
                        .Select(g => new StockMovementLineRequest
                        {
                            ProductId = g.Key,
                            QtyDeltaOnHand = -g.Sum(l => l.QtyDeltaReserved), // Negative to reduce on-hand
                            QtyDeltaReserved = -g.Sum(l => l.QtyDeltaReserved) // Negative to reduce reserved
                        }).ToList();

                    var issueReq = new StockMovementRequest
                    {
                        WarehouseId = group.Key,
                        MovementTypeCode = MovementTypeCodes.Issue,
                        RequestId = requestId.Value,
                        UserId = userId,
                        Notes = "Requestor confirmation - stock issued",
                        Lines = lines
                    };

                    await _stockService.PostMovementAsync(issueReq, HttpContext.TraceIdentifier, $"workflow:task:{id}", $"confirm:{id}:{group.Key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Confirmation issuance failed: {ex.Message}");
            }
        }

        try
        {
            await _workflowEngine.ProcessActionAsync(id, request.ActionCode, userId, request.Notes, request.PayloadJson);
            return Ok(new ApiResponse<string> { Data = "Action processed successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    private long? ExtractRequestIdFromTask(long taskId)
    {
        var task = _db.WorkflowTasks
            .Include(t => t.WorkflowInstance)
            .FirstOrDefault(t => t.WorkflowTaskId == taskId);
        return ExtractRequestId(task?.WorkflowInstance?.BusinessEntityKey ?? "");
    }

    private decimal GetRequestedQty(long requestId, long productId)
    {
        return _db.InventoryRequestLines
            .Where(l => l.RequestId == requestId && l.ProductId == productId)
            .Select(l => l.QtyRequested)
            .FirstOrDefault();
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
    
    [HttpGet("templates")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetTemplates([FromQuery] PagedRequest request)
    {
        var query = _db.WorkflowDefinitionVersions
            .Include(v => v.WorkflowDefinition)
            .Where(x => x.WorkflowDefinition.IsActive)
            .Select(v => new {
                Id = v.WorkflowDefinitionId,
                VersionId = v.WorkflowDefinitionVersionId,
                v.WorkflowDefinition.Code,
                Name = v.WorkflowDefinition.Name + " - v" + v.VersionNo,
                v.VersionNo,
                v.IsActive,
                v.PublishedAt
            });

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) || x.Code.Contains(request.SearchTerm));
        }

        var paged = await query.OrderBy(x => x.Name).ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("tasks/{id}/eligible-assignees")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetEligibleUsers(long id)
    {
        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowStep)
            .ThenInclude(s => s.Rule)
            .Include(t => t.WorkflowInstance)
            .FirstOrDefaultAsync(t => t.WorkflowTaskId == id);

        if (task == null || task.WorkflowStep.Rule == null) 
            return Ok(new ApiResponse<IEnumerable<object>> { Data = new List<object>() });

        var rule = task.WorkflowStep.Rule;
        var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

        if (rule.RoleId.HasValue)
        {
            query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId.Value));
        }

        if (rule.DepartmentId.HasValue)
        {
            query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId.Value));
        }

        // Simplification for prototype: list matched users
        var results = await query
            .Select(u => new { 
                u.UserId, 
                u.Username, 
                u.DisplayName,
                Roles = u.Roles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync();
        return Ok(new ApiResponse<IEnumerable<object>> { Data = results });
    }

    [HttpGet("templates/versions/{versionId}/assignment-options")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetAssignmentOptions(long versionId)
    {
        var version = await _db.WorkflowDefinitionVersions
            .Include(v => v.Steps.OrderBy(s => s.SequenceNo))
            .ThenInclude(s => s.Rule)
            .Where(v => v.WorkflowDefinitionVersionId == versionId)
            .FirstOrDefaultAsync();

        if (version == null) return NotFound();

        var assignmentModes = await _db.WorkflowAssignmentModes.ToDictionaryAsync(m => m.AssignmentModeId, m => m.Code);

        var result = new List<object>();

        foreach (var step in version.Steps.OrderBy(s => s.SequenceNo)) 
        {
            var rule = step.Rule;
            var modeCode = (rule != null && assignmentModes.ContainsKey(rule.AssignmentModeId)) 
                ? assignmentModes[rule.AssignmentModeId] 
                : (step.SequenceNo == 0 ? WorkflowAssignmentModeCodes.Requestor : "");

            if (string.IsNullOrEmpty(modeCode) && rule == null) continue;

            // Basic manual selection modes
            bool isManual = modeCode == WorkflowAssignmentModeCodes.Role || 
                            modeCode == WorkflowAssignmentModeCodes.Department || 
                            modeCode == WorkflowAssignmentModeCodes.RoleAndDepartment ||
                            modeCode == WorkflowAssignmentModeCodes.Requestor ||
                            (rule?.AllowRequesterSelect ?? false);

            var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

            if (rule?.RoleId.HasValue ?? false)
            {
                query = query.Where(u => u.Roles.Any(r => r.RoleId == rule.RoleId.Value));
            }

            if (rule?.DepartmentId.HasValue ?? false)
            {
                query = query.Where(u => u.Departments.Any(d => d.DepartmentId == rule.DepartmentId.Value));
            }

            var users = await query
                .Select(u => new 
                { 
                    u.UserId, 
                    u.DisplayName,
                    Role = u.Roles.Select(r => r.Role.Name).FirstOrDefault(),
                    Department = u.Departments.Select(d => d.Department.Name).FirstOrDefault()
                })
                .ToListAsync();

            var modeName = await _db.WorkflowAssignmentModes
                .Where(m => m.Code == modeCode)
                .Select(m => m.Name)
                .FirstOrDefaultAsync() ?? (modeCode == WorkflowAssignmentModeCodes.Requestor ? "Requestor (Initiator)" : modeCode);

            var configuredRole = (rule?.RoleId.HasValue ?? false)
                ? await _db.Roles.Where(r => r.RoleId == rule.RoleId.Value).Select(r => r.Name).FirstOrDefaultAsync() 
                : null;
            
            var configuredDept = (rule?.DepartmentId.HasValue ?? false)
                ? await _db.Departments.Where(d => d.DepartmentId == rule.DepartmentId.Value).Select(d => d.Name).FirstOrDefaultAsync() 
                : null;

            result.Add(new
            {
                step.WorkflowStepId,
                step.Name,
                ModeCode = modeCode,
                AssignmentMode = modeName,
                RoleName = configuredRole,
                DepartmentName = configuredDept,
                IsManual = isManual,
                EligibleUsers = users.Select(u => new {
                    u.UserId,
                    u.DisplayName,
                    Description = $"{u.DisplayName} ({u.Role} - {u.Department})"
                })
            });
        }

        return Ok(new ApiResponse<IEnumerable<object>> { Data = result });
    }
}

public record WorkflowActionRequest(string ActionCode, string? Notes = null, string? PayloadJson = null);
