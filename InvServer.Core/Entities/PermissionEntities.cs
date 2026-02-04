using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("PERMISSION")]
public class Permission
{
    [Key]
    public long PermissionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

[Table("ROLE_PERMISSION")]
public class RolePermission
{
    [Key]
    public long RolePermissionId { get; set; }

    public long RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    public long PermissionId { get; set; }
    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public long? GrantedByUserId { get; set; }

    public ICollection<RolePermissionScope> Scopes { get; set; } = new List<RolePermissionScope>();
}

[Table("ROLE_PERMISSION_SCOPE")]
public class RolePermissionScope
{
    [Key]
    public long RolePermissionScopeId { get; set; }

    public long RolePermissionId { get; set; }
    [ForeignKey(nameof(RolePermissionId))]
    public RolePermission RolePermission { get; set; } = null!;

    public long AccessScopeTypeId { get; set; }
    // Note: AccessScopeType is a lookup handled via strings in code usually but we have a table
    
    public long? DepartmentId { get; set; }
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    public long? WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}

[Table("ACCESS_SCOPE_TYPE")]
public class AccessScopeType
{
    [Key]
    public long AccessScopeTypeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}
