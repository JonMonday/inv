using InvServer.Core.Entities;
using InvServer.Core.Models;

namespace InvServer.Core.Interfaces;

public interface IWorkflowEngine
{
    Task<long> StartWorkflowAsync(string workflowCode, string businessEntityKey, long initiatorUserId, List<WorkflowManualAssignmentDto>? manualAssignments = null, long? versionId = null);
    Task ProcessActionAsync(long taskId, string actionCode, long userId, string? notes = null, string? payloadJson = null);
    Task ClaimTaskAsync(long taskId, long userId);
}
