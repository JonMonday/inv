using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("SECURITY_EVENT_TYPE")]
public class SecurityEventType
{
    [Key]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

[Table("IDEMPOTENCY_KEY")]
public class IdempotencyKey
{
    [Key]
    public long IdempotencyKeyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string RouteKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    public long UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "PROCESSING";

    public long? MovementId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public string? ResponseJson { get; set; }

    public int? ResponseStatusCode { get; set; }
}

[Table("AUDIT_LOG")]
public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [MaxLength(50)]
    public string? CorrelationId { get; set; }

    public long? UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EntityTable { get; set; }

    [MaxLength(50)]
    public string? EntityId { get; set; }

    [MaxLength(50)]
    public string? Action { get; set; }

    public string? PayloadJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
