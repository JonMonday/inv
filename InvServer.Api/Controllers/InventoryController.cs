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
using IAppAuthorizationService = InvServer.Core.Interfaces.IAuthorizationService;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly InvDbContext _db;
    private readonly IStockService _stockService;
    private readonly IAppAuthorizationService _authService;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IAuditService _auditService;

    public InventoryController(InvDbContext db, IStockService stockService, IAppAuthorizationService authService, IWorkflowEngine workflowEngine, IAuditService auditService)
    {
        _db = db;
        _stockService = stockService;
        _authService = authService;
        _workflowEngine = workflowEngine;
        _auditService = auditService;
    }

    [HttpGet("requests")]
    [RequirePermission("inventory_request.read")]
    public async Task<ActionResult<PagedResponse<List<InventoryRequestSummaryDto>>>> GetRequests([FromQuery] PagedRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var filter = await _authService.GetScopeFilterAsync(userId, "inventory_request.read");
        var query = _db.InventoryRequests.AsNoTracking();

        if (filter.Scope == AccessScope.GLOBAL) { }
        else if (filter.Scope == AccessScope.WAREHOUSE) query = query.Where(r => filter.AllowedIds!.Contains(r.WarehouseId));
        else if (filter.Scope == AccessScope.DEPT) query = query.Where(r => filter.AllowedIds!.Contains(r.DepartmentId));
        else query = query.Where(r => r.RequestedByUserId == userId);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(r => r.RequestNo.Contains(request.SearchTerm) || (r.Notes != null && r.Notes.Contains(request.SearchTerm)));
        }

        query = query.OrderByDescending(r => r.RequestedAt);

        var paged = await query
            .Select(r => new InventoryRequestSummaryDto
            {
                RequestId = r.RequestId,
                RequestNo = r.RequestNo,
                WarehouseName = r.Warehouse.Name,
                WarehouseId = r.WarehouseId,
                DepartmentId = r.DepartmentId,
                DepartmentName = r.Department.Name,
                StatusCode = r.Status.Code,
                StatusLabel = r.Status.Name,
                RequestedAt = r.RequestedAt,
                CurrentAssignee = r.WorkflowInstance != null 
                    ? r.WorkflowInstance.Tasks
                        .Where(t => t.WorkflowTaskStatus.Code == "PENDING" || t.WorkflowTaskStatus.Code == "AVAILABLE" || t.WorkflowTaskStatus.Code == "CLAIMED")
                        .SelectMany(t => t.Assignees)
                        .Select(a => a.User.DisplayName)
                        .FirstOrDefault() ?? ""
                    : ""
            })
            .ToPagedResponseAsync(request);

        return Ok(paged);
    }

    [HttpGet("requests/{requestId}/fulfillment-details")]
    [RequirePermission("inventory_request.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetFulfillmentDetails(long requestId)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Include(r => r.Warehouse)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        // Explicitly load lines if for some reason Include didn't catch them
        if (request.Lines == null || !request.Lines.Any())
        {
            request.Lines = await _db.InventoryRequestLines
                .Include(l => l.Product)
                .Where(l => l.RequestId == requestId)
                .ToListAsync();
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        
        // Get stock levels for all products in this request across all warehouses
        var stockLevels = await _db.StockLevels
            .Include(s => s.Warehouse)
            .Where(s => productIds.Contains(s.ProductId))
            .Select(s => new {
                productId = s.ProductId,
                warehouseId = s.WarehouseId,
                warehouseName = s.Warehouse.Name,
                onHandQty = s.OnHandQty,
                reservedQty = s.ReservedQty,
                availableQty = s.OnHandQty - s.ReservedQty
            })
            .ToListAsync();

        var data = new {
            requestId = request.RequestId,
            requestNo = request.RequestNo,
            warehouseId = request.WarehouseId,
            lines = request.Lines.Select(l => new {
                productId = l.ProductId,
                productName = l.Product.Name,
                qtyRequested = l.QtyRequested,
                stock = stockLevels.Where(s => s.productId == l.ProductId).ToList()
            }).ToList()
        };

        return Ok(new ApiResponse<object> { Data = data });
    }

    [HttpGet("requests/{requestId}")]
    [RequirePermission("inventory_request.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetRequest(long requestId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
                .ThenInclude(l => l.Product)
            .Include(r => r.Status)
            .Include(r => r.Warehouse)
            .Include(r => r.Department)
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.CurrentStep)
            .ThenInclude(s => s.StepType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        // Scope check
        var filter = await _authService.GetScopeFilterAsync(userId, "inventory_request.read");
        bool allowed = filter.Scope switch
        {
            AccessScope.GLOBAL => true,
            AccessScope.WAREHOUSE => filter.AllowedIds!.Contains(request.WarehouseId),
            AccessScope.DEPT => filter.AllowedIds!.Contains(request.DepartmentId),
            AccessScope.OWN => request.RequestedByUserId == userId,
            _ => false
        };

        if (!allowed) return Forbid();

        return Ok(new ApiResponse<object> { Data = request });
    }

    [HttpPost("requests")]
    [RequirePermission("inventory_request.create")]
    [EnableRateLimiting("workflow_mutation")]
    public async Task<ActionResult<ApiResponse<long>>> CreateRequest([FromBody] InventoryRequestDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var request = new InventoryRequest
        {
            RequestNo = $"REQ-{DateTime.UtcNow.Ticks}",
            RequestTypeId = dto.RequestTypeId,
            RequestStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.Draft),
            WarehouseId = dto.WarehouseId,
            DepartmentId = dto.DepartmentId,
            RequestedByUserId = userId,
            RequestedAt = DateTime.UtcNow,
            WorkflowTemplateId = dto.WorkflowTemplateId,
            Notes = dto.Notes
        };

        foreach (var line in dto.Lines)
        {
            request.Lines.Add(new InventoryRequestLine
            {
                ProductId = line.ProductId,
                QtyRequested = line.Quantity
            });
        }

        _db.InventoryRequests.Add(request);
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<long> { Data = request.RequestId });
    }

    [HttpPut("requests/{requestId}")]
    [RequirePermission("inventory_request.update_draft")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateRequest(long requestId, [FromBody] InventoryRequestDto dto)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        var draftStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.Draft);
        if (request.RequestStatusId != draftStatusId)
            return BadRequest(new ApiErrorResponse { Message = "Only draft requests can be updated." });

        request.RequestTypeId = dto.RequestTypeId;
        request.WarehouseId = dto.WarehouseId;
        request.DepartmentId = dto.DepartmentId;
        request.WorkflowTemplateId = dto.WorkflowTemplateId;
        request.Notes = dto.Notes;

        // Sync lines
        request.Lines.Clear();
        foreach (var line in dto.Lines)
        {
            request.Lines.Add(new InventoryRequestLine
            {
                ProductId = line.ProductId,
                QtyRequested = line.Quantity
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<string> { Data = "Updated successfully." });
    }

    [HttpGet("requests/{requestId}/history")]
    [RequirePermission("inventory_request.read")]
    public async Task<ActionResult<ApiResponse<object>>> GetHistory(long requestId)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.Tasks)
            .ThenInclude(t => t.Actions)
            .ThenInclude(a => a.ActionByUser)
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.Tasks)
            .ThenInclude(t => t.WorkflowStep)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();
        if (request.WorkflowInstance == null) return Ok(new ApiResponse<object> { Data = new List<object>() });

        var history = request.WorkflowInstance.Tasks
            .OrderBy(t => t.CreatedAt)
            .Select(t => new
            {
                t.WorkflowTaskId,
                StepName = t.WorkflowStep.Name,
                t.CreatedAt,
                t.CompletedAt,
                Actions = t.Actions.Select(a => new
                {
                    a.ActionAt,
                    PerformedBy = a.ActionByUser.Username,
                    a.Notes,
                    a.PayloadJson
                })
            });

        return Ok(new ApiResponse<object> { Data = history });
    }

    [HttpPost("requests/{requestId}/submit")]
    [RequirePermission("inventory_request.submit")]
    [EnableRateLimiting("workflow_mutation")]
    public async Task<ActionResult<ApiResponse<long>>> SubmitRequest(long requestId, [FromBody] SubmitRequestDto? dto)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
            .Include(r => r.WorkflowTemplate)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();
        
        var draftStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.Draft);
        if (request.RequestStatusId != draftStatusId)
            return BadRequest(new ApiErrorResponse { Message = "Only draft requests can be submitted." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        long.TryParse(userIdStr, out var userId);

        var workflowCode = request.WorkflowTemplate?.Code ?? "INV_REQ_FLOW";
        var instanceId = await _workflowEngine.StartWorkflowAsync(
            request.WorkflowTemplateId!.Value,
            $"INV_REQ:{request.RequestId}",
            userId,
            dto?.ManualAssignments
        );


        request.WorkflowInstanceId = instanceId;
        request.RequestStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.InWorkflow);

        await _db.SaveChangesAsync();
        await _auditService.LogChangeAsync(userId, "SUBMIT_REQUEST", new { id = requestId, status = "DRAFT" }, new { id = requestId, status = "IN_WORKFLOW" });

        // Auto-advance the 'START' step if it exists
        var task = await _db.WorkflowTasks
            .Include(t => t.WorkflowStep)
            .Where(t => t.WorkflowInstanceId == instanceId && 
                       (t.WorkflowStep.StepKey == WorkflowStepTypeCodes.Start || 
                        t.WorkflowStep.StepKey == "START" || 
                        t.WorkflowStep.StepKey == "SUBMISSION"))
            .FirstOrDefaultAsync();

        if (task != null)
        {
            await _workflowEngine.ProcessActionAsync(task.WorkflowTaskId, WorkflowActionCodes.Submit, userId, "Auto-submitted on creation");
        }

        return Ok(new ApiResponse<long> { Data = instanceId });
    }

    [HttpPost("requests/{requestId}/cancel")]
    [RequirePermission("inventory_request.cancel")]
    public async Task<ActionResult<ApiResponse<string>>> CancelRequest(long requestId)
    {
        var request = await _db.InventoryRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (request == null) return NotFound();

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        long.TryParse(userIdStr, out var userId);

        var draftStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.Draft);
        var inWorkflowStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.InWorkflow);

        if (request.RequestStatusId != draftStatusId && request.RequestStatusId != inWorkflowStatusId)
            return BadRequest(new ApiErrorResponse { Message = "Cannot cancel in current state." });

        // Check if ANY active (non-terminal) reservations exist specifically for this request
        var hasActiveReservations = await _db.Reservations
            .Include(r => r.ReservationStatus)
            .AnyAsync(r => r.RequestId == requestId && !r.ReservationStatus.IsTerminal);
            
        if (hasActiveReservations) 
            return BadRequest(new ApiErrorResponse { Message = "Active reservations exist for this request. Release them first." });

        var oldStatus = request.RequestStatusId;
        request.RequestStatusId = await GetStatusIdAsync("INVENTORY_REQUEST_STATUS", RequestStatusCodes.Cancelled);
        await _db.SaveChangesAsync();
        
        await _auditService.LogChangeAsync(userId, "CANCEL_REQUEST", new { id = requestId, status = oldStatus }, new { id = requestId, status = "CANCELLED" });

        return Ok(new ApiResponse<string> { Data = "Cancelled" });
    }

    [HttpPost("requests/{requestId}/fulfillment/reserve")]
    [RequirePermission("reservation.create")]
    [EnableRateLimiting("fulfillment")]
    public async Task<ActionResult<ApiResponse<long>>> Reserve(long requestId)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.CurrentStep)
            .ThenInclude(s => s.StepType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        // Enforce Workflow Step Type check
        if (request.WorkflowInstance?.CurrentStep?.StepType?.Code != WorkflowStepTypeCodes.Fulfillment)
            return BadRequest(new ApiErrorResponse { Message = "Request is not in a fulfillment step." });

        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest(new ApiErrorResponse { Message = "X-Idempotency-Key header is required for fulfillment operations." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var movementReq = new StockMovementRequest {
            WarehouseId = request.WarehouseId,
            MovementTypeCode = MovementTypeCodes.Reserve,
            RequestId = request.RequestId,
            UserId = userId,
            Lines = request.Lines.Select(l => new StockMovementLineRequest {
                ProductId = l.ProductId,
                QtyDeltaReserved = l.QtyRequested
            }).ToList()
        };

        var routeKey = $"{Request.Method}:{Request.Path}";
        var correlationId = Request.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrEmpty(correlationId)) correlationId = HttpContext.TraceIdentifier;

        var movementId = await _stockService.PostMovementAsync(movementReq, correlationId, routeKey, idempotencyKey);
        return Ok(new ApiResponse<long> { Data = movementId });
    }

    [HttpPost("requests/{requestId}/fulfillment/release")]
    [RequirePermission("reservation.release")]
    public async Task<ActionResult<ApiResponse<long>>> Release(long requestId)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.CurrentStep)
            .ThenInclude(s => s.StepType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
            
        if (request == null) return NotFound();

        // Enforce Workflow Step Type check
        if (request.WorkflowInstance?.CurrentStep?.StepType?.Code != WorkflowStepTypeCodes.Fulfillment)
            return BadRequest(new ApiErrorResponse { Message = "Request is not in a fulfillment step." });

        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest(new ApiErrorResponse { Message = "X-Idempotency-Key header is required for fulfillment operations." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var movementReq = new StockMovementRequest {
            WarehouseId = request.WarehouseId,
            MovementTypeCode = MovementTypeCodes.Release,
            RequestId = request.RequestId,
            UserId = userId,
            Lines = request.Lines.Select(l => new StockMovementLineRequest {
                ProductId = l.ProductId,
                QtyDeltaReserved = -l.QtyRequested
            }).ToList()
        };

        var routeKey = $"{Request.Method}:{Request.Path}";
        var correlationId = Request.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrEmpty(correlationId)) correlationId = HttpContext.TraceIdentifier;

        var movementId = await _stockService.PostMovementAsync(movementReq, correlationId, routeKey, idempotencyKey);
        return Ok(new ApiResponse<long> { Data = movementId });
    }

    [HttpPost("requests/{requestId}/fulfillment/issue")]
    [RequirePermission("stock_movement.create")]
    public async Task<ActionResult<ApiResponse<long>>> Issue(long requestId)
    {
        var request = await _db.InventoryRequests
            .Include(r => r.Lines)
            .Include(r => r.WorkflowInstance)
            .ThenInclude(i => i.CurrentStep)
            .ThenInclude(s => s.StepType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        // Enforce Workflow Step Type check
        if (request.WorkflowInstance!.CurrentStep!.StepType!.Code != WorkflowStepTypeCodes.Fulfillment)
            return BadRequest(new ApiErrorResponse { Message = "Request is not in a fulfillment step." });

        // Check if there are active reservations to determine movement type
        var hasActiveReservations = await _db.Reservations
            .Include(r => r.ReservationStatus)
            .AnyAsync(r => r.RequestId == requestId && !r.ReservationStatus!.IsTerminal);

        var idempotencyKey = Request.Headers["X-Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest(new ApiErrorResponse { Message = "X-Idempotency-Key header is required for fulfillment operations." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var movementReq = new StockMovementRequest {
            WarehouseId = request.WarehouseId,
            MovementTypeCode = hasActiveReservations ? MovementTypeCodes.ConsumeReserve : MovementTypeCodes.Issue,
            RequestId = request.RequestId,
            UserId = userId,
            Lines = request.Lines.Select(l => new StockMovementLineRequest {
                ProductId = l.ProductId,
                // If consuming reserve, we reduce OnHand and Reserved.
                // If plain Issue, we reduce only OnHand.
                QtyDeltaOnHand = -l.QtyRequested,
                QtyDeltaReserved = hasActiveReservations ? -l.QtyRequested : 0
            }).ToList()
        };

        var routeKey = $"{Request.Method}:{Request.Path}";
        var correlationId = Request.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrEmpty(correlationId)) correlationId = HttpContext.TraceIdentifier;

        var movementId = await _stockService.PostMovementAsync(movementReq, correlationId, routeKey, idempotencyKey);
        return Ok(new ApiResponse<long> { Data = movementId });
    }

    private async Task<long> GetStatusIdAsync(string table, string code)
    {
        var status = await _db.InventoryRequestStatuses.FirstOrDefaultAsync(s => s.Code == code);
        if (status == null) throw new InvalidOperationException($"Status code '{code}' not found in {table}.");
        return status.RequestStatusId;
    }
}

public record InventoryRequestDto(
    long RequestTypeId,
    long WarehouseId,
    long DepartmentId,
    string? Notes,
    long? WorkflowTemplateId,
    List<InventoryRequestLineDto> Lines,
    List<WorkflowManualAssignmentDto>? ManualAssignments = null
);


public record InventoryRequestLineDto(long ProductId, decimal Quantity);
public record SubmitRequestDto(List<WorkflowManualAssignmentDto>? ManualAssignments);

public class InventoryRequestSummaryDto
{
    public long RequestId { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? CurrentAssignee { get; set; }
}

