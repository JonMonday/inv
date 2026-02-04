namespace InvServer.Core.Models;

public record WorkflowManualAssignmentDto(long WorkflowStepId, List<long> UserIds);
