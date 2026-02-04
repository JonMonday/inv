using InvServer.Core.Entities;

namespace InvServer.Core.Interfaces;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(long userId, string permissionCode, AccessScope? requiredScope = null, long? entityId = null);
    Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(long userId);
    Task<ScopeFilter> GetScopeFilterAsync(long userId, string permissionCode);
}

public record ScopeFilter(AccessScope Scope, IEnumerable<long>? AllowedIds);

public enum AccessScope
{
    OWN = 1,
    DEPT = 2,
    WAREHOUSE = 3,
    GLOBAL = 4
}

public record UserPermission(string PermissionCode, AccessScope Scope, long? DepartmentId, long? WarehouseId);
