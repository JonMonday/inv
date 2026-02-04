using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using InvServer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InvServer.Tests;

public class AuthTests
{
    private InvDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<InvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new InvDbContext(options);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateAndRevokeOldOne()
    {
        // Arrange
        var db = GetDbContext();
        var jwtMock = new Mock<IJwtService>();
        
        var user = new User { Username = "test", Email = "test@test.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var authService = new AuthService(db, jwtMock.Object);
        jwtMock.SetupSequence(j => j.GenerateRefreshToken())
            .Returns("refresh-token-1")
            .Returns("refresh-token-2");
        jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("new-access-token");

        // Create initial session
        var originalRefreshToken = "original-refresh-token";
        // Auth service hashes tokens, so we need to use its internal hashing or just mock the hash if we could.
        // But AuthService is what we are testing. Let's use a helper or make HashToken internal.
        // For now, I'll use the AuthService to login first.
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password");
        await db.SaveChangesAsync();

        var loginResult = await authService.LoginAsync(new LoginRequest("test", "password"), "127.0.0.1", "test");
        var firstRefreshToken = loginResult!.RefreshToken;

        // Act
        var refreshResult = await authService.RefreshTokenAsync(firstRefreshToken, "127.0.0.1", "test");

        // Assert
        Assert.NotNull(refreshResult);
        Assert.NotEqual(firstRefreshToken, refreshResult.RefreshToken);
        
        var oldSession = await db.AuthSessions.FirstOrDefaultAsync(s => s.RevokedAtUtc != null);
        Assert.NotNull(oldSession);
        
        var activeSession = await db.AuthSessions.FirstOrDefaultAsync(s => s.RevokedAtUtc == null);
        Assert.NotNull(activeSession);
    }
}
