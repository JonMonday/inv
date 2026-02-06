using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("WORKFLOW_INSTANCE")]
public class WorkflowInstance
{
    [Key]
    public long WorkflowInstanceId { get; set; }

    public long WorkflowTemplateId { get; set; }
    [ForeignKey(nameof(WorkflowTemplateId))]
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;

    public long WorkflowInstanceStatusId { get; set; }
    [ForeignKey(nameof(WorkflowInstanceStatusId))]
    public WorkflowInstanceStatus WorkflowInstanceStatus { get; set; } = null!;

    public long InitiatorUserId { get; set; }
    [ForeignKey(nameof(InitiatorUserId))]
    public User InitiatorUser { get; set; } = null!;

    [Required, MaxLength(100)]
    public string BusinessEntityKey { get; set; } = string.Empty;

    public long? CurrentWorkflowStepId { get; set; }
    [ForeignKey(nameof(CurrentWorkflowStepId))]
    public WorkflowStep? CurrentStep { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<WorkflowTask> Tasks { get; set; } = new List<WorkflowTask>();
}

[Table("WORKFLOW_TASK")]
public class WorkflowTask
{
    [Key]
    public long WorkflowTaskId { get; set; }

    public long WorkflowInstanceId { get; set; }
    [ForeignKey(nameof(WorkflowInstanceId))]
    public WorkflowInstance WorkflowInstance { get; set; } = null!;

    public long WorkflowStepId { get; set; }
    [ForeignKey(nameof(WorkflowStepId))]
    public WorkflowStep WorkflowStep { get; set; } = null!;

    public long WorkflowTaskStatusId { get; set; }
    [ForeignKey(nameof(WorkflowTaskStatusId))]
    public WorkflowTaskStatus WorkflowTaskStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }

    public long? ClaimedByUserId { get; set; }
    [ForeignKey(nameof(ClaimedByUserId))]
    public User? ClaimedByUser { get; set; }

    public DateTime? ClaimedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<WorkflowTaskAssignee> Assignees { get; set; } = new List<WorkflowTaskAssignee>();
    public ICollection<WorkflowTaskAction> Actions { get; set; } = new List<WorkflowTaskAction>();
}

[Table("WORKFLOW_TASK_ASSIGNEE")]
public class WorkflowTaskAssignee
{
    [Key]
    public long WorkflowTaskAssigneeId { get; set; }

    public long WorkflowTaskId { get; set; }
    [ForeignKey(nameof(WorkflowTaskId))]
    public WorkflowTask WorkflowTask { get; set; } = null!;

    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public long AssigneeStatusId { get; set; }
    [ForeignKey(nameof(AssigneeStatusId))]
    public WorkflowTaskAssigneeStatus AssigneeStatus { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}

[Table("WORKFLOW_TASK_ACTION")]
public class WorkflowTaskAction
{
    [Key]
    public long WorkflowTaskActionId { get; set; }

    public long WorkflowTaskId { get; set; }
    [ForeignKey(nameof(WorkflowTaskId))]
    public WorkflowTask WorkflowTask { get; set; } = null!;

    public long WorkflowActionTypeId { get; set; }
    [ForeignKey(nameof(WorkflowActionTypeId))]
    public WorkflowActionType ActionType { get; set; } = null!;

    public long ActionByUserId { get; set; }
    [ForeignKey(nameof(ActionByUserId))]
    public User ActionByUser { get; set; } = null!;

    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
    public string? PayloadJson { get; set; }

    [MaxLength(200)]
    public string? IdempotencyKey { get; set; }
}

[Table("WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT")]
public class WorkflowInstanceManualAssignment
{
    [Key]
    public long ManualAssignmentId { get; set; }

    public long WorkflowInstanceId { get; set; }
    [ForeignKey(nameof(WorkflowInstanceId))]
    public WorkflowInstance WorkflowInstance { get; set; } = null!;

    public long WorkflowStepId { get; set; }
    [ForeignKey(nameof(WorkflowStepId))]
    public WorkflowStep WorkflowStep { get; set; } = null!;

    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
