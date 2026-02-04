using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("WORKFLOW_DEFINITION")]
public class WorkflowDefinition
{
    [Key]
    public long WorkflowDefinitionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkflowDefinitionVersion> Versions { get; set; } = new List<WorkflowDefinitionVersion>();
}

[Table("WORKFLOW_DEFINITION_VERSION")]
public class WorkflowDefinitionVersion
{
    [Key]
    public long WorkflowDefinitionVersionId { get; set; }

    public long WorkflowDefinitionId { get; set; }
    [ForeignKey(nameof(WorkflowDefinitionId))]
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public int VersionNo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime PublishedAt { get; set; }

    public long? PublishedByUserId { get; set; }

    public string DefinitionJson { get; set; } = string.Empty;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}

[Table("WORKFLOW_STEP")]
public class WorkflowStep
{
    [Key]
    public long WorkflowStepId { get; set; }

    public long WorkflowDefinitionVersionId { get; set; }
    [ForeignKey(nameof(WorkflowDefinitionVersionId))]
    public WorkflowDefinitionVersion WorkflowDefinitionVersion { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string StepKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public long WorkflowStepTypeId { get; set; }
    [ForeignKey(nameof(WorkflowStepTypeId))]
    public WorkflowStepType StepType { get; set; } = null!;

    public int SequenceNo { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemRequired { get; set; } = false;

    public WorkflowStepRule? Rule { get; set; }
}

[Table("WORKFLOW_STEP_RULE")]
public class WorkflowStepRule
{
    [Key]
    public long WorkflowStepRuleId { get; set; }

    public long WorkflowStepId { get; set; }
    [ForeignKey(nameof(WorkflowStepId))]
    public WorkflowStep WorkflowStep { get; set; } = null!;

    public long AssignmentModeId { get; set; }

    public long? RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    public long? DepartmentId { get; set; }
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    public bool UseRequesterDepartment { get; set; } = false;
    public bool AllowRequesterSelect { get; set; } = false;

    public int MinApprovers { get; set; } = 1;
    public bool RequireAll { get; set; } = false;
    public bool AllowReassign { get; set; } = true;
    public bool AllowDelegate { get; set; } = true;

    public int? SLA_Minutes { get; set; }
}

[Table("WORKFLOW_TRANSITION")]
public class WorkflowTransition
{
    [Key]
    public long WorkflowTransitionId { get; set; }

    public long WorkflowDefinitionVersionId { get; set; }
    [ForeignKey(nameof(WorkflowDefinitionVersionId))]
    public WorkflowDefinitionVersion WorkflowDefinitionVersion { get; set; } = null!;

    public long FromWorkflowStepId { get; set; }
    [ForeignKey(nameof(FromWorkflowStepId))]
    public WorkflowStep FromStep { get; set; } = null!;

    public long WorkflowActionTypeId { get; set; }

    public long ToWorkflowStepId { get; set; }
    [ForeignKey(nameof(ToWorkflowStepId))]
    public WorkflowStep ToStep { get; set; } = null!;
}
