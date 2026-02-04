using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InvServer.Infrastructure.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly InvDbContext _db;
    private readonly IMemoryCache _cache;

    public AuthorizationService(InvDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, AccessScope? requiredScope = null, long? entityId = null)
    {
        // 1. Get User's PermVersion (required for cache key)
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.PermVersion })
            .FirstOrDefaultAsync();

        if (user == null) return false;

        // 2. Get/Set cached permissions
        var permissions = await GetUserPermissionsCachedAsync(userId, user.PermVersion);

        // 3. Check for permission matching the code
        var matchedPermissions = permissions.Where(p => p.PermissionCode == permissionCode).ToList();
        if (!matchedPermissions.Any()) return false;

        // 4. Default check (if no scope passed, just matching code is enough)
        if (requiredScope == null) return true;

        // 5. Scope-based logic
        // We evaluate if user has ANY grant that satisfies the required scope or better
        foreach (var perm in matchedPermissions)
        {
            // GLOBAL always satisfies
            if (perm.Scope == AccessScope.GLOBAL) return true;

            // WAREHOUSE check
            if (requiredScope == AccessScope.WAREHOUSE && perm.Scope == AccessScope.WAREHOUSE)
            {
                if (perm.WarehouseId == entityId) return true;
            }

            // DEPT check
            if (requiredScope == AccessScope.DEPT && perm.Scope == AccessScope.DEPT)
            {
                if (perm.DepartmentId == entityId) return true;
            }

            // OWN check
            if (requiredScope == AccessScope.OWN && perm.Scope == AccessScope.OWN)
            {
                // Logic for 'OWN' usually happens at the controller/query level via Entity.CreatedByUserId == userId
                // But we return true here to signify the USER IS GRANTED 'OWN' access for this permission
                return true; 
            }
        }

        return false;
    }

    public async Task<ScopeFilter> GetScopeFilterAsync(long userId, string permissionCode)
    {
        var user = await _db.Users.AsNoTracking().Where(u => u.UserId == userId).Select(u => new { u.PermVersion }).FirstOrDefaultAsync();
        if (user == null) return new ScopeFilter(AccessScope.OWN, new List<long>());

        var permissions = await GetUserPermissionsCachedAsync(userId, user.PermVersion);
        var matched = permissions.Where(p => p.PermissionCode == permissionCode).ToList();

        if (!matched.Any()) return new ScopeFilter(AccessScope.OWN, new List<long>());

        // If user has GLOBAL, that's the highest
        if (matched.Any(p => p.Scope == AccessScope.GLOBAL))
            return new ScopeFilter(AccessScope.GLOBAL, null);

        // If user has WAREHOUSE, collect warehouse IDs
        var warehouseIds = matched.Where(p => p.Scope == AccessScope.WAREHOUSE && p.WarehouseId != null).Select(p => p.WarehouseId!.Value).ToList();
        if (warehouseIds.Any())
            return new ScopeFilter(AccessScope.WAREHOUSE, warehouseIds);

        // If user has DEPT, collect department IDs
        var deptIds = matched.Where(p => p.Scope == AccessScope.DEPT && p.DepartmentId != null).Select(p => p.DepartmentId!.Value).ToList();
        if (deptIds.Any())
            return new ScopeFilter(AccessScope.DEPT, deptIds);

        // Fallback to OWN
        return new ScopeFilter(AccessScope.OWN, new List<long> { userId });
    }

    public async Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(long userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Enumerable.Empty<UserPermission>();
        
        return await GetUserPermissionsCachedAsync(userId, user.PermVersion);
    }

    private async Task<IEnumerable<UserPermission>> GetUserPermissionsCachedAsync(long userId, int permVersion)
    {
        string cacheKey = $"perm_{userId}_{permVersion}";

        if (!_cache.TryGetValue(cacheKey, out IEnumerable<UserPermission>? permissions) || permissions == null)
        {
            // Cache miss - Fetch from DB
            permissions = await FetchPermissionsFromDbAsync(userId);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, permissions, cacheOptions);
        }

        return permissions;
    }

    private async Task<IEnumerable<UserPermission>> FetchPermissionsFromDbAsync(long userId)
    {
        // Query to get all distinct permissions across all roles for a user, including scopes
        var query = from ur in _db.UserRoles
                    join r in _db.Roles on ur.RoleId equals r.RoleId
                    join rp in _db.RolePermissions on r.RoleId equals rp.RoleId
                    join p in _db.Permissions on rp.PermissionId equals p.PermissionId
                    join rps in _db.RolePermissionScopes on rp.RolePermissionId equals rps.RolePermissionId into scopes
                    from s in scopes.DefaultIfEmpty()
                    join ast in _db.AccessScopeTypes on s.AccessScopeTypeId equals ast.AccessScopeTypeId into astScopes
                    from astType in astScopes.DefaultIfEmpty()
                    where ur.UserId == userId && r.IsActive && p.IsActive
                    select new UserPermission(
                        p.Code,
                        astType != null ? MapScope(astType.Code) : AccessScope.GLOBAL, // Default to GLOBAL if no scope row
                        s != null ? s.DepartmentId : null,
                        s != null ? s.WarehouseId : null
                    );

        return await query.ToListAsync();
    }

    private static AccessScope MapScope(string code) => code.ToUpper() switch
    {
        "OWN" => AccessScope.OWN,
        "DEPT" => AccessScope.DEPT,
        "WAREHOUSE" => AccessScope.WAREHOUSE,
        "GLOBAL" => AccessScope.GLOBAL,
        _ => AccessScope.GLOBAL
    };
}
