using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace InvServer.Tests;

public class AuthorizationTests
{
    private InvDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<InvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new InvDbContext(options);
    }

    [Fact]
    public async Task HasPermission_GlobalScope_ShouldReturnTrue()
    {
        // Arrange
        var db = GetDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var authService = new AuthorizationService(db, cache);

        var user = new User { UserId = 1, Username = "admin", PermVersion = 1 };
        db.Users.Add(user);

        var role = new Role { RoleId = 1, Name = "Admin", Code = "ADMIN" };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });

        var perm = new Permission { PermissionId = 1, Code = "test.perm", Name = "Test" };
        db.Permissions.Add(perm);
        db.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = 1 });

        await db.SaveChangesAsync();

        // Act
        var result = await authService.HasPermissionAsync(1, "test.perm");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasPermission_WarehouseScope_ShouldValidateId()
    {
        // Arrange
        var db = GetDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var authService = new AuthorizationService(db, cache);

        var user = new User { UserId = 2, Username = "wh_mgr", PermVersion = 1 };
        db.Users.Add(user);

        var role = new Role { RoleId = 2, Name = "WHManager", Code = "WH_MGR" };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = 2, RoleId = 2 });

        var perm = new Permission { PermissionId = 2, Code = "stock.view", Name = "View Stock" };
        db.Permissions.Add(perm);
        
        var rp = new RolePermission { RoleId = 2, PermissionId = 2 };
        db.RolePermissions.Add(rp);
        await db.SaveChangesAsync(); 

        var ast = new AccessScopeType { AccessScopeTypeId = 1, Code = "WAREHOUSE", Name = "Warehouse" };
        db.AccessScopeTypes.Add(ast);
        db.RolePermissionScopes.Add(new RolePermissionScope 
        { 
            RolePermissionId = rp.RolePermissionId, 
            AccessScopeTypeId = 1,
            WarehouseId = 101 // Specific warehouse
        });

        await db.SaveChangesAsync();

        // Act & Assert
        Assert.True(await authService.HasPermissionAsync(2, "stock.view", AccessScope.WAREHOUSE, 101));
        Assert.False(await authService.HasPermissionAsync(2, "stock.view", AccessScope.WAREHOUSE, 999));
    }

    [Fact]
    public async Task PermVersion_ShouldInvalidateCache()
    {
        // Arrange
        var db = GetDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var authService = new AuthorizationService(db, cache);

        var user = new User { UserId = 3, Username = "user3", PermVersion = 1 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 1. First call - should return false (no permissions)
        var result1 = await authService.HasPermissionAsync(3, "any.perm");
        Assert.False(result1);

        // 2. Grant permission but DON'T increment PermVersion
        var role = new Role { RoleId = 3, Name = "Role3", Code = "ROLE3" };
        db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = 3, RoleId = 3 });
        var perm = new Permission { PermissionId = 3, Code = "any.perm", Name = "Any" };
        db.Permissions.Add(perm);
        db.RolePermissions.Add(new RolePermission { RoleId = 3, PermissionId = 3 });
        await db.SaveChangesAsync();

        // 3. Second call - should STILL return false due to cache (perm_3_1)
        var result2 = await authService.HasPermissionAsync(3, "any.perm");
        Assert.False(result2);

        // 4. Increment PermVersion
        user.PermVersion = 2;
        await db.SaveChangesAsync();

        // 5. Third call - should return true (cache miss for perm_3_2)
        var result3 = await authService.HasPermissionAsync(3, "any.perm");
        Assert.True(result3);
    }
}
