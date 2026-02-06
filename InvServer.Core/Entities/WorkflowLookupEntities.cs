using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("WORKFLOW_STEP_TYPE")]
public class WorkflowStepType
{
    [Key]
    public long WorkflowStepTypeId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

[Table("WORKFLOW_ACTION_TYPE")]
public class WorkflowActionType
{
    [Key]
    public long WorkflowActionTypeId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsDecision { get; set; }
    public bool IsSystemAction { get; set; }
}

[Table("WORKFLOW_INSTANCE_STATUS")]
public class WorkflowInstanceStatus
{
    [Key]
    public long WorkflowInstanceStatusId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsTerminal { get; set; }
}

[Table("WORKFLOW_TASK_STATUS")]
public class WorkflowTaskStatus
{
    [Key]
    public long WorkflowTaskStatusId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsTerminal { get; set; }
}

[Table("WORKFLOW_ASSIGNMENT_MODE")]
public class WorkflowAssignmentMode
{
    [Key]
    public long AssignmentModeId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

[Table("WORKFLOW_CONDITION_OPERATOR")]
public class WorkflowConditionOperator
{
    [Key]
    public long WorkflowConditionOperatorId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
