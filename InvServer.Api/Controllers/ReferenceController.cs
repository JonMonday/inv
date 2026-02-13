using InvServer.Core.Entities;
using InvServer.Core.Models;
using InvServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferenceController : ControllerBase
{
    private readonly InvDbContext _db;

    public ReferenceController(InvDbContext db)
    {
        _db = db;
    }

    private async Task<ActionResult> HandleReference<TEntity, TKey>(
        IQueryable<TEntity> query,
        PagedRequest request,
        Func<string, Expression<Func<TEntity, bool>>> searchFilterFactory,
        Expression<Func<TEntity, TKey>> orderBy,
        Expression<Func<TEntity, ReferenceItemDto>> selector) where TEntity : class
    {
        Response.Headers.Append("X-Ref-Controller", "v2-HandleReference");
        query = query.AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(searchFilterFactory(term));
        }

        query = query.OrderBy(orderBy);

        var totalRecords = await query.CountAsync();
        var pageSize = request.PageSize > 0 ? request.PageSize : 100;
        var data = await query
            .Skip(request.Skip)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync();

        return Ok(new PagedResponse<List<ReferenceItemDto>>(data, request.PageNumber, pageSize, totalRecords));
    }

    [HttpGet("stock/levels")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetStockLevels([FromQuery] long? warehouseId, [FromQuery] long? productId)
    {
        var query = _db.StockLevels
            .Include(s => s.Warehouse)
            .Include(s => s.Product)
            .OrderBy(s => s.Product.Name)
            .AsQueryable();

        if (warehouseId.HasValue && warehouseId > 0) 
            query = query.Where(s => s.WarehouseId == warehouseId);
            
        if (productId.HasValue && productId > 0) 
            query = query.Where(s => s.ProductId == productId);

        var data = await query.Select(s => new {
            s.StockLevelId,
            s.WarehouseId,
            WarehouseName = s.Warehouse.Name,
            s.ProductId,
            ProductName = s.Product.Name,
            ProductSku = s.Product.SKU,
            s.OnHandQty,
            s.ReservedQty,
            AvailableQty = s.OnHandQty - s.ReservedQty,
            ProductReorderLevel = s.Product.ReorderLevel,
            s.UpdatedAt
        }).ToListAsync();

        return Ok(new ApiResponse<object> { Data = data });
    }

    [HttpGet("{type}")]
    public async Task<ActionResult<PagedResponse<List<ReferenceItemDto>>>> GetReferenceData(string type, [FromQuery] PagedRequest request)
    {
        return type.ToLower() switch
        {
            // Inventory
            "inventory-request-status" => await HandleReference(_db.InventoryRequestStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.RequestStatusId, x.Code, x.Name, "", x.IsTerminal)),
            
            "inventory-request-type" => await HandleReference(_db.InventoryRequestTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.RequestTypeId, x.Code, x.Name, "", x.IsActive)),
            
            "inventory-movement-type" => await HandleReference(_db.InventoryMovementTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.MovementTypeId, x.Code, x.Name, "", true)),
            
            "inventory-movement-status" => await HandleReference(_db.InventoryMovementStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.MovementStatusId, x.Code, x.Name, "", x.IsTerminal)),
            
            "inventory-reason-code" => await HandleReference(_db.InventoryReasonCodes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.ReasonCodeId, x.Code, x.Name, "", x.IsActive)),
            
            "reservation-status" => await HandleReference(_db.ReservationStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.ReservationStatusId, x.Code, x.Name, "", x.IsTerminal)),
            
            "products" => await HandleReference(_db.Products, request, 
                term => x => x.Name.ToLower().Contains(term) || x.SKU.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.ProductId, x.SKU, x.Name, "", true)),
            
            "warehouses" => await HandleReference(_db.Warehouses, request, 
                term => x => x.Name.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WarehouseId, x.Name, x.Name, x.Location ?? "", true)),
            
            "categories" => await HandleReference(_db.Categories, request, 
                term => x => x.Name.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.CategoryId, x.Name, x.Name, "", true)),

            "units-of-measure" => await HandleReference(_db.UnitsOfMeasure, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.UnitOfMeasureId, x.Code, x.Name, "", x.IsActive)),

            // Workflow
            "workflow-step-type" => await HandleReference(_db.WorkflowStepTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WorkflowStepTypeId, x.Code, x.Name, "", true)),
            
            "workflow-action-type" => await HandleReference(_db.WorkflowActionTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WorkflowActionTypeId, x.Code, x.Name, "", true)),
            
            "workflow-assignment-mode" => await HandleReference(_db.WorkflowAssignmentModes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.AssignmentModeId, x.Code, x.Name, "", true)),
            
            "workflow-condition-operator" => await HandleReference(_db.WorkflowConditionOperators, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WorkflowConditionOperatorId, x.Code, x.Name, "", true)),
            
            "workflow-instance-status" => await HandleReference(_db.WorkflowInstanceStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WorkflowInstanceStatusId, x.Code, x.Name, "", x.IsTerminal)),
            
            "workflow-task-status" => await HandleReference(_db.WorkflowTaskStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.WorkflowTaskStatusId, x.Code, x.Name, "", x.IsTerminal)),
            
            "workflow-task-assignee-status" => await HandleReference(_db.WorkflowTaskAssigneeStatuses, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.AssigneeStatusId, x.Code, x.Name, "", true)),

            // System
            "access-scope-type" => await HandleReference(_db.AccessScopeTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.AccessScopeTypeId, x.Code, x.Name, "", true)),
            
            "departments" => await HandleReference(_db.Departments, request, 
                term => x => x.Name.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(x.DepartmentId, x.Name, x.Name, "", true)),

            "security-event-type" => await HandleReference(_db.SecurityEventTypes, request, 
                term => x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term), 
                x => x.Name, x => new ReferenceItemDto(0, x.Code, x.Name, "", true)),

            _ => NotFound(new ApiErrorResponse { Message = $"Reference type '{type}' not found." })
        };
    }
}

public record ReferenceItemDto(long Id, string Code, string Name, string Description, bool IsActiveOrTerminal);
