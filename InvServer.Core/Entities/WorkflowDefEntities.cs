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
