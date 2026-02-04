using InvServer.Core.Constants;
using InvServer.Core.Entities;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InvServer.Tests;

public class WorkflowEngineTests
{
    private InvDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new InvDbContext(options);
    }

    [Fact]
    public async Task ClaimTaskAsync_ShouldSucceed_WhenTaskIsUnclaimed()
    {
        // Arrange
        using var db = CreateContext();
        var engine = new WorkflowEngine(db);
        
        var task = new WorkflowTask { WorkflowTaskId = 1, ClaimedByUserId = null };
        db.WorkflowTasks.Add(task);
        await db.SaveChangesAsync();

        // Act
        await engine.ClaimTaskAsync(1, 100);

        // Assert
        var updatedTask = await db.WorkflowTasks.FindAsync(1L);
        Assert.NotNull(updatedTask);
        Assert.Equal(100, updatedTask.ClaimedByUserId);
        Assert.NotNull(updatedTask.ClaimedAt);
    }

    [Fact]
    public async Task ClaimTaskAsync_ShouldThrow_WhenAlreadyClaimed()
    {
        // Arrange
        using var db = CreateContext();
        var engine = new WorkflowEngine(db);
        
        var task = new WorkflowTask { WorkflowTaskId = 1, ClaimedByUserId = 200 };
        db.WorkflowTasks.Add(task);
        await db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ClaimTaskAsync(1, 100));
    }
    [Fact]
    public async Task ProcessActionAsync_ShouldSucceed_AndCompleteStep()
    {
        // Arrange
        using var db = CreateContext();
        var engine = new WorkflowEngine(db);

        // Seed lookups
        db.WorkflowTaskStatuses.Add(new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Pending, WorkflowTaskStatusId = 1 });
        db.WorkflowTaskStatuses.Add(new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Completed, WorkflowTaskStatusId = 2 });
        db.WorkflowActionTypes.Add(new WorkflowActionType { Code = WorkflowActionCodes.Approve, WorkflowActionTypeId = 1 });
        db.WorkflowTaskAssigneeStatuses.Add(new WorkflowTaskAssigneeStatus { Code = "COMPLETED", AssigneeStatusId = 1 });
        await db.SaveChangesAsync();

        var instance = new WorkflowInstance { WorkflowInstanceId = 1, BusinessEntityKey = "REQ-001" };
        var step = new WorkflowStep { WorkflowStepId = 1, Name = "Step 1", StepKey = "STEP1" };
        var task = new WorkflowTask 
        { 
            WorkflowTaskId = 1, 
            WorkflowInstance = instance, 
            WorkflowStepId = 1, 
            WorkflowTaskStatusId = 1 
        };
        task.Assignees.Add(new WorkflowTaskAssignee { UserId = 100, AssigneeStatusId = 2 }); // Pending
        
        db.WorkflowSteps.Add(step);
        db.WorkflowInstances.Add(instance);
        db.WorkflowTasks.Add(task);
        await db.SaveChangesAsync();

        // Act
        await engine.ProcessActionAsync(1, WorkflowActionCodes.Approve, 100, "Approved it.");

        // Assert
        var updatedTask = await db.WorkflowTasks.Include(t => t.Actions).FirstOrDefaultAsync(t => t.WorkflowTaskId == 1);
        Assert.NotNull(updatedTask);
        Assert.Equal(2, updatedTask.WorkflowTaskStatusId); // Completed
        Assert.Single(updatedTask.Actions);
        Assert.Equal("Approved it.", updatedTask.Actions.First().Notes);
    }
}
