using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

public static class WorkflowTemplateStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Archived = "ARCHIVED";
}

[Table("WORKFLOW_TEMPLATE")]
public class WorkflowTemplate
{
    [Key]
    public long WorkflowTemplateId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = WorkflowTemplateStatuses.Draft;

    public bool IsActive { get; set; } = true;

    public long? SourceTemplateId { get; set; }
    [ForeignKey(nameof(SourceTemplateId))]
    public WorkflowTemplate? SourceTemplate { get; set; }

    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long? PublishedByUserId { get; set; }
    public DateTime? PublishedAt { get; set; }

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}

[Table("WORKFLOW_STEP")]
public class WorkflowStep
{
    [Key]
    public long WorkflowStepId { get; set; }

    public long WorkflowTemplateId { get; set; }
    [ForeignKey(nameof(WorkflowTemplateId))]
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;

    [Required, MaxLength(50)]
    public string StepKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
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
    [ForeignKey(nameof(AssignmentModeId))]
    public WorkflowAssignmentMode AssignmentMode { get; set; } = null!;

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

    public long WorkflowTemplateId { get; set; }
    [ForeignKey(nameof(WorkflowTemplateId))]
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;

    public long FromWorkflowStepId { get; set; }
    [ForeignKey(nameof(FromWorkflowStepId))]
    public WorkflowStep FromStep { get; set; } = null!;

    public long WorkflowActionTypeId { get; set; }
    [ForeignKey(nameof(WorkflowActionTypeId))]
    public WorkflowActionType ActionType { get; set; } = null!;

    public long ToWorkflowStepId { get; set; }
    [ForeignKey(nameof(ToWorkflowStepId))]
    public WorkflowStep ToStep { get; set; } = null!;
}
