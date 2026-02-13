using InvServer.Api.Filters;
using InvServer.Core.Constants;
using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly InvDbContext _db;

    public StockController(IStockService stockService, InvDbContext db)
    {
        _stockService = stockService;
        _db = db;
    }

    [HttpPost("movements")]
    [RequirePermission("stock_movement.post", AccessScope.GLOBAL)]
    public async Task<ActionResult<ApiResponse<long>>> PostMovement([FromBody] StockMovementRequest request)
    {
        // 0. Restriction: Direct RESERVE/ISSUE via this endpoint is restricted to Global Admins
        // In a real system, we'd check if the user has 'stock.admin' or similar.
        if (request.MovementTypeCode == MovementTypeCodes.Reserve || request.MovementTypeCode == MovementTypeCodes.Issue)
        {
             // We can allow it but maybe with a warning or strictly enforced for Admins only.
             // For now, let's allow but keep the existing workflow check.
        }
        // 1. API Level Rule Enforcement: Reservations only allowed if workflow is in FULFILLMENT
        if (request.MovementTypeCode == MovementTypeCodes.Reserve && request.RequestId != null)
        {
            var isValid = await _db.InventoryRequests
                .Include(r => r.WorkflowInstance)
                    .ThenInclude(i => i.CurrentStep)
                        .ThenInclude(s => s.StepType)
                .Where(r => r.RequestId == request.RequestId)
                .Select(r => r.WorkflowInstance != null && r.WorkflowInstance.CurrentStep != null && r.WorkflowInstance.CurrentStep.StepType.Code == WorkflowStepTypeCodes.Fulfillment)
                .FirstOrDefaultAsync();

            if (!isValid)
            {
                return BadRequest(new ApiErrorResponse 
                { 
                    Message = "Reservations only allowed when Request is in FULFILLMENT workflow step.",
                    CorrelationId = HttpContext.Items["CorrelationId"]?.ToString()
                });
            }
        }

        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var result = await _stockService.PostMovementAsync(request, correlationId);
        
        return Ok(new ApiResponse<long> { Data = result });
    }

    [HttpGet("movements")]
    [RequirePermission("stock_movement.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetMovements([FromQuery] PagedRequest request)
    {
        var query = _db.StockMovements
            .Include(m => m.Lines)
            .Include(m => m.CreatedByUser)
            .Include(m => m.MovementType)
            .Include(m => m.ReferenceRequest)
            .Include(m => m.ReferenceReservation)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new {
                m.StockMovementId,
                RequestNo = m.ReferenceRequest != null ? m.ReferenceRequest.RequestNo : null,
                ReferenceNo = m.ReferenceReservation != null ? m.ReferenceReservation.ReservationNo : null,
                MovementTypeCode = m.MovementType.Code,
                m.WarehouseId,
                PerformedBy = m.CreatedByUser.Username,
                m.CreatedAt,
                LinesCount = m.Lines.Count,
                Lines = m.Lines.Select(l => new {
                    l.ProductId,
                    ProductName = l.Product.Name,
                    l.QtyDeltaOnHand,
                    l.QtyDeltaReserved
                }).ToList()
            });

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(m => (m.RequestNo != null && m.RequestNo.Contains(request.SearchTerm)) || 
                                     (m.ReferenceNo != null && m.ReferenceNo.Contains(request.SearchTerm)) ||
                                     m.PerformedBy.Contains(request.SearchTerm));
        }

        var paged = await query.ToPagedResponseAsync(request);
        return Ok(paged);
    }
}
