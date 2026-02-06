using InvServer.Core.Models;

namespace InvServer.Core.Interfaces;

public interface IWorkflowEngine
{
    Task<long> StartWorkflowAsync(long templateId, string businessEntityKey, long initiatorUserId,
        List<WorkflowManualAssignmentDto>? manualAssignments = null);

    Task ProcessActionAsync(long taskId, string actionCode, long userId, string? notes = null, string? payloadJson = null, string? idempotencyKey = null, long? nextAssigneeUserId = null);

    Task ClaimTaskAsync(long taskId, long userId);
}
