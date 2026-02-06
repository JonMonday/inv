using Azure.Core;
using InvServer.Api.Filters;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Extensions;
using InvServer.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("workflow_mutation")]
public class AdminController : ControllerBase
{
    private readonly InvDbContext _db;
    private readonly IAuditService _auditService;

    public AdminController(InvDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet("roles")]
    [RequirePermission("role.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetRoles([FromQuery] PagedRequest request)
    {
        var query = _db.Roles.AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(r => r.Name.Contains(request.SearchTerm) || r.Code.Contains(request.SearchTerm));
        }

        var projectedQuery = query.OrderBy(r => r.Name)
            .Select(r => new
            {
                r.RoleId,
                r.Name,
                r.Code,
                r.Description,
                r.IsActive,
                UserCount = _db.UserRoles.Count(ur => ur.RoleId == r.RoleId)
            });

        var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
    }

    [HttpGet("permissions")]
    [RequirePermission("permission.read")]
    public async Task<ActionResult<PagedResponse<List<object>>>> GetPermissions([FromQuery] PagedRequest request)
    {
        var query = _db.Permissions.AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => p.Code.Contains(request.SearchTerm) || p.Name.Contains(request.SearchTerm));
        }

        var projectedQuery = query.OrderBy(p => p.Code)
            .Select(p => new
            {
                p.PermissionId,
                p.Code,
                p.Name,
                p.Description,
                p.IsActive
            });

    var paged = await projectedQuery.ToPagedResponseAsync(request);
        return Ok(paged);
}

[HttpGet("roles/{id}")]
[RequirePermission("role.read")]
public async Task<ActionResult<ApiResponse<object>>> GetRole(long id)
{
    var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == id);
    if (role == null) return NotFound();
    return Ok(new ApiResponse<object> { Data = role });
}

[HttpPost("roles")]
[RequirePermission("role.create")]
public async Task<ActionResult<ApiResponse<long>>> CreateRole([FromBody] CreateRoleRequest request)
{
    var role = new Role { Name = request.Name, Code = request.Code, IsActive = true };
    _db.Roles.Add(role);
    await _db.SaveChangesAsync();
    return Ok(new ApiResponse<long> { Data = role.RoleId });
}

[HttpPut("roles/{id}")]
[RequirePermission("role.update")]
public async Task<ActionResult<ApiResponse<string>>> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
{
    var role = await _db.Roles.FindAsync(id);
    if (role == null) return NotFound();

    role.Name = request.Name;
    role.Code = request.Code;
    await _db.SaveChangesAsync();

    return Ok(new ApiResponse<string> { Data = "Role updated." });
}

[HttpGet("users")]
[RequirePermission("user.read")]
public async Task<ActionResult<PagedResponse<List<object>>>> GetUsers([FromQuery] PagedRequest request)
{
    var query = _db.Users
        .Include(u => u.Roles).ThenInclude(ur => ur.Role)
        .AsNoTracking();

    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        query = query.Where(u =>
            u.Username.Contains(request.SearchTerm) ||
            u.DisplayName.Contains(request.SearchTerm) ||
            u.Email.Contains(request.SearchTerm));
    }

    var projectedQuery = query.OrderBy(u => u.Username)
        .Select(u => new
        {
            u.UserId,
            u.Username,
            u.Email,
            u.DisplayName,
            u.IsActive,
            Roles = u.Roles.Select(r => r.Role.Name)
        });

    var paged = await projectedQuery.ToPagedResponseAsync(request);
    return Ok(paged);
}

[HttpGet("users/{id}")]
[RequirePermission("user.read")]
public async Task<ActionResult<ApiResponse<object>>> GetUser(long id)
{
    var user = await _db.Users
        .Include(u => u.Roles).ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.UserId == id);

    if (user == null) return NotFound();

    var result = new
    {
        user.UserId,
        user.Username,
        user.Email,
        user.DisplayName,
        user.IsActive,
        Roles = user.Roles.Select(r => r.Role.Name),
        RoleIds = user.Roles.Select(r => r.RoleId)
    };

    return Ok(new ApiResponse<object> { Data = result });
}

[HttpPost("users")]
[RequirePermission("user.create")]
public async Task<ActionResult<ApiResponse<long>>> CreateUser([FromBody] CreateUserRequest request)
{
    if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        return BadRequest(new ApiErrorResponse { Message = "Username already exists." });

    var user = new User
    {
        Username = request.Username,
        Email = request.Email,
        DisplayName = request.DisplayName,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        PasswordHash = "HASHED_DEFAULT"
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    if (request.RoleIds != null && request.RoleIds.Any())
    {
        foreach (var rid in request.RoleIds)
        {
            _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid });
        }
        await _db.SaveChangesAsync();
    }

    return Ok(new ApiResponse<long> { Data = user.UserId });
}

[HttpPut("users/{id}")]
[RequirePermission("user.update")]
public async Task<ActionResult<ApiResponse<string>>> UpdateUser(long id, [FromBody] UpdateUserRequest request)
{
    var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == id);
    if (user == null) return NotFound();

    user.Email = request.Email;
    user.DisplayName = request.DisplayName;
    user.IsActive = request.IsActive;

    if (request.RoleIds != null)
    {
        _db.UserRoles.RemoveRange(user.Roles);
        foreach (var rid in request.RoleIds)
        {
            _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid });
        }
        user.PermVersion++;
    }

    await _db.SaveChangesAsync();
    return Ok(new ApiResponse<string> { Data = "User updated." });
}

[HttpPost("roles/{id}/permissions")]
[RequirePermission("role_permission.grant")]
public async Task<ActionResult<ApiResponse<string>>> UpdateRolePermissions(long id, [FromBody] List<long> permissionIds)
{
    var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.RoleId == id);
    if (role == null) return NotFound();

    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    long.TryParse(userIdStr, out var userId);

    var oldPermissions = role.RolePermissions.Select(p => p.PermissionId).ToList();

    _db.RolePermissions.RemoveRange(role.RolePermissions);
    foreach (var pid in permissionIds)
    {
        role.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = pid });
    }

    await _db.SaveChangesAsync();

    await _auditService.LogChangeAsync(userId, "UPDATE_ROLE_PERMISSIONS",
        new { RoleId = id, Permissions = oldPermissions },
        new { RoleId = id, Permissions = permissionIds });

    return Ok(new ApiResponse<string> { Data = "Permissions updated." });
}

[HttpGet("departments")]
[RequirePermission("department.read")]
public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetDepartments()
{
    var depts = await _db.Departments.AsNoTracking().ToListAsync();
    return Ok(new ApiResponse<IEnumerable<object>> { Data = depts });
}

[HttpPost("users/{id}/reset-password")]
[RequirePermission("user.reset_password")]
public async Task<ActionResult<ApiResponse<string>>> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
{
    var user = await _db.Users.FindAsync(id);
    if (user == null) return NotFound();

    user.PasswordHash = "HASHED_" + request.NewPassword;
    user.PermVersion++;

    await _db.SaveChangesAsync();
    await _auditService.LogChangeAsync(0, "ADMIN_PASSWORD_RESET", new { UserId = id }, new { Action = "Reset" });

    return Ok(new ApiResponse<string> { Data = "Password reset successfully." });
}

[HttpGet("audit-logs")]
[RequirePermission("audit_log.read")]
public async Task<ActionResult<PagedResponse<List<object>>>> GetAuditLogs([FromQuery] PagedRequest request)
{
    var query = _db.AuditLogs
        .Include(l => l.User)
        .AsNoTracking();

    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        query = query.Where(l => l.Action.Contains(request.SearchTerm) ||
                                 (l.User != null && l.User.Username.Contains(request.SearchTerm)));
    }

    var projectedQuery = query.OrderByDescending(l => l.CreatedAtUtc)
        .Select(l => new
        {
            l.AuditLogId,
            Action = l.Action,
            Timestamp = l.CreatedAtUtc,
            PerformedBy = l.User != null ? l.User.Username : "Unknown",
            Payload = l.PayloadJson
        });

    var paged = await projectedQuery.ToPagedResponseAsync(request);
    return Ok(paged);
}
}

public record ResetPasswordRequest(string NewPassword);
public record CreateRoleRequest(string Name, string Code);
public record UpdateRoleRequest(string Name, string Code);
public record CreateUserRequest(string Username, string Email, string DisplayName, List<long>? RoleIds);
public record UpdateUserRequest(string Email, string DisplayName, bool IsActive, List<long>? RoleIds);
