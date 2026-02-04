using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("USER")]
public class User
{
    [Key]
    public long UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;
    public int PermVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserDepartment> Departments { get; set; } = new List<UserDepartment>();
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}

[Table("AUTH_SESSION")]
public class AuthSession
{
    [Key]
    public long AuthSessionId { get; set; }

    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string RefreshTokenHash { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DeviceInfo { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
}

[Table("USER_DEPARTMENT")]
public class UserDepartment
{
    [Key]
    public long UserDepartmentId { get; set; }

    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public long DepartmentId { get; set; }
    [ForeignKey(nameof(DepartmentId))]
    public Department Department { get; set; } = null!;

    public bool IsPrimary { get; set; } = false;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

[Table("USER_ROLE")]
public class UserRole
{
    [Key]
    public long UserRoleId { get; set; }

    public long UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public long RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
