using InvServer.Core.Constants;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;

namespace InvServer.Tests;

public class IdempotencyIntegrationTests : IDisposable
{
    private readonly InvDbContext _db;
    private readonly StockService _stockService;
    private const string ConnectionString = "Server=localhost;Database=InvServerIntegrationTests;User Id=sa;Password=Your_Password123;TrustServerCertificate=True";

    public IdempotencyIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InvDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        _db = new InvDbContext(options);
        
        // Ensure clean state
        _db.Database.EnsureDeleted();
        _db.Database.EnsureCreated();

        _stockService = new StockService(_db);
        SeedBaseData();
    }

    private void SeedBaseData()
    {
        _db.InventoryMovementTypes.Add(new InventoryMovementType { Code = MovementTypeCodes.Issue, Name = "Issue" });
        _db.InventoryMovementStatuses.Add(new InventoryMovementStatus { Code = MovementStatusCodes.Posted, Name = "Posted" });
        _db.Warehouses.Add(new Warehouse { WarehouseId = 1, Name = "Main Warehouse" });
        _db.Products.Add(new Product { ProductId = 1, Name = "Test Product", SKU = "SKU001", UnitOfMeasure = "EA" });
        _db.StockLevels.Add(new StockLevel { WarehouseId = 1, ProductId = 1, OnHandQty = 100, ReservedQty = 0, UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostMovement_ShouldBeIdempotent_WhenSameKeyProvided()
    {
        // Arrange
        var request = new StockMovementRequest
        {
            MovementTypeCode = MovementTypeCodes.Issue,
            WarehouseId = 1,
            UserId = 1,
            Lines = new List<StockMovementLineRequest>
            {
                new StockMovementLineRequest { ProductId = 1, QtyDeltaOnHand = -10 }
            }
        };

        var idempotencyKey = Guid.NewGuid().ToString();
        var routeKey = "POST:/api/inventory/requests/1/fulfillment/issue";

        // Act
        var firstMovementId = await _stockService.PostMovementAsync(request, "corr-1", routeKey, idempotencyKey);
        var secondMovementId = await _stockService.PostMovementAsync(request, "corr-1", routeKey, idempotencyKey);

        // Assert
        Assert.Equal(firstMovementId, secondMovementId);
        
        var movementCount = await _db.StockMovements.CountAsync(m => m.ReferenceRequestId == request.RequestId);
        // Note: MovementId is always returned, but only one record should exist if correctly implemented
        // In our case, the first call creates it, the second re-returns the same ID.
        var totalMovements = await _db.StockMovements.CountAsync();
        Assert.Equal(1, totalMovements);

        var stock = await _db.StockLevels.FirstAsync(s => s.ProductId == 1);
        Assert.Equal(90, stock.OnHandQty); // Only reduced once
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateStockLevel_ShouldLockRow_EnsuringCorrectQuantity()
    {
        // This test simulates concurrent updates to the same stock level row
        // Arrange
        var request = new StockMovementRequest
        {
            MovementTypeCode = MovementTypeCodes.Issue,
            WarehouseId = 1,
            UserId = 1,
            Lines = new List<StockMovementLineRequest>
            {
                new StockMovementLineRequest { ProductId = 1, QtyDeltaOnHand = -1 }
            }
        };

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var ik = Guid.NewGuid().ToString();
            tasks.Add(Task.Run(async () => {
                // Using different idempotency keys to ensure multiple movements are attempted
                using var scopeDb = new InvDbContext(new DbContextOptionsBuilder<InvDbContext>().UseSqlServer(ConnectionString).Options);
                var service = new StockService(scopeDb);
                await service.PostMovementAsync(request, Guid.NewGuid().ToString(), "route-" + ik, ik);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stock = await _db.StockLevels.FirstAsync(s => s.ProductId == 1);
        Assert.Equal(90, stock.OnHandQty); // 100 - (10 * 1)
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
