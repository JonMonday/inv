using InvServer.Api.Controllers;
using InvServer.Core.Constants;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IAppAuthorizationService = InvServer.Core.Interfaces.IAuthorizationService;

namespace InvServer.Tests;

public class FulfillmentTests
{
    private (InvDbContext, Mock<IStockService>, Mock<IWorkflowEngine>, Mock<IAuditService>, Mock<IAppAuthorizationService>) CreateMocks()
    {
        var options = new DbContextOptionsBuilder<InvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new InvDbContext(options);
        var stock = new Mock<IStockService>();
        var workflow = new Mock<IWorkflowEngine>();
        var audit = new Mock<IAuditService>();
        var auth = new Mock<IAppAuthorizationService>();
        return (db, stock, workflow, audit, auth);
    }

    [Fact]
    public async Task Reserve_ShouldReturnBadRequest_IfNotInFulfillmentStep()
    {
        // Arrange
        var (db, stock, workflow, audit, auth) = CreateMocks();
        
        // Seed status
        db.InventoryRequestStatuses.Add(new InventoryRequestStatus { Code = RequestStatusCodes.Draft, RequestStatusId = 1 });
        db.InventoryRequestStatuses.Add(new InventoryRequestStatus { Code = RequestStatusCodes.Fulfillment, RequestStatusId = 2 });
        await db.SaveChangesAsync();

        var request = new InventoryRequest { RequestId = 1, RequestStatusId = 1 }; // Draft
        db.InventoryRequests.Add(request);
        await db.SaveChangesAsync();

        var controller = new InventoryController(db, stock.Object, auth.Object, workflow.Object, audit.Object);

        // Act
        var result = await controller.Reserve(1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
