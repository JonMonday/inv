using InvServer.Api.Filters;
using InvServer.Core.Entities;
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
public class CatalogController : ControllerBase
{
    private readonly InvDbContext _db;

    public CatalogController(InvDbContext db)
    {
        _db = db;
    }

    [HttpGet("products")]
    [RequirePermission("product.read")]
    public async Task<ActionResult<PagedResponse<List<ProductListDto>>>> GetProducts([FromQuery] PagedRequest request)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .OrderBy(p => p.Name)
            .Select(p => new ProductListDto(
                p.ProductId,
                p.SKU,
                p.Name,
                p.Category != null ? p.Category.Name : "Uncategorized",
                p.UnitOfMeasure.Code,
                p.ReorderLevel
            ));

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }

        var result = await query.ToPagedResponseAsync(request);
        return Ok(result);
    }

    [HttpPost("categories")]
    [RequirePermission("category.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateCategory([FromBody] CategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = true
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<long> { Data = category.CategoryId });
    }

    [HttpPost("products")]
    [RequirePermission("product.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateProduct([FromBody] ProductDto dto)
    {
        if (await _db.Products.AnyAsync(p => p.SKU == dto.SKU))
            return BadRequest(new ApiErrorResponse { Message = $"Product with SKU {dto.SKU} already exists." });

        var product = new Product
        {
            SKU = dto.SKU,
            Name = dto.Name,
            CategoryId = dto.CategoryId,
            UnitOfMeasureId = dto.UnitOfMeasureId,
            ReorderLevel = dto.ReorderLevel,
            IsActive = true
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Initialize Stock Levels for all warehouses
        var warehouses = await _db.Warehouses.ToListAsync();
        foreach (var w in warehouses)
        {
            _db.StockLevels.Add(new StockLevel
            {
                WarehouseId = w.WarehouseId,
                ProductId = product.ProductId,
                OnHandQty = 0,
                ReservedQty = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<long> { Data = product.ProductId });
    }

    [HttpPost("warehouses")]
    [RequirePermission("warehouse.create")]
    public async Task<ActionResult<ApiResponse<long>>> CreateWarehouse([FromBody] WarehouseDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Location = dto.Location,
            IsActive = true
        };

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();

        // Initialize Stock Levels for all products
        var products = await _db.Products.ToListAsync();
        foreach (var p in products)
        {
            _db.StockLevels.Add(new StockLevel
            {
                WarehouseId = warehouse.WarehouseId,
                ProductId = p.ProductId,
                OnHandQty = 0,
                ReservedQty = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<long> { Data = warehouse.WarehouseId });
    }

    [HttpPatch("products/{id}/reorder-level")]
    [RequirePermission("product.update")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateReorderLevel(long id, [FromBody] UpdateReorderLevelDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new ApiErrorResponse { Message = "Product not found." });

        product.ReorderLevel = dto.ReorderLevel;
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<bool> { Data = true });
    }
}

public record CategoryDto(string Name, long? ParentCategoryId);
public record ProductDto(string SKU, string Name, long? CategoryId, long UnitOfMeasureId, decimal ReorderLevel);
public record WarehouseDto(string Name, string? Location);
public record UpdateReorderLevelDto(decimal ReorderLevel);
public record ProductListDto(long Id, string SKU, string Name, string CategoryName, string UnitOfMeasureCode, decimal ReorderLevel);
