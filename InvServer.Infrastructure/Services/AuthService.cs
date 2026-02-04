using BCrypt.Net;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace InvServer.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly InvDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(InvDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        var session = new AuthSession
        {
            UserId = user.UserId,
            RefreshTokenHash = refreshTokenHash,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AuthSessions.Add(session);
        await _db.SaveChangesAsync();

        // Audit Event
        var audit = new AuditLog
        {
            UserId = user.UserId,
            EventType = "SECURITY_EVENT",
            Action = "LOGIN_SUCCESS",
            PayloadJson = $"{{\"ip\": \"{ipAddress}\", \"device\": \"{deviceInfo}\"}}",
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken, session.ExpiresAtUtc);
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo)
    {
        var hash = HashToken(refreshToken);
        var session = await _db.AuthSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && s.RevokedAtUtc == null && s.ExpiresAtUtc > DateTime.UtcNow);

        if (session == null || !session.User.IsActive)
        {
            // Potential theft - could revoke all user sessions here
            return null;
        }

        // Rotate Refresh Token
        session.RevokedAtUtc = DateTime.UtcNow;
        
        var user = session.User;
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newHash = HashToken(newRefreshToken);

        var newSession = new AuthSession
        {
            UserId = user.UserId,
            RefreshTokenHash = newHash,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        _db.AuthSessions.Add(newSession);
        
        // Audit Event for Rotation
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.UserId,
            EventType = "SECURITY_EVENT",
            Action = "TOKEN_ROTATED",
            PayloadJson = $"{{\"ip\": \"{ipAddress}\", \"device\": \"{deviceInfo}\"}}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return new AuthResponse(newAccessToken, newRefreshToken, newSession.ExpiresAtUtc);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var session = await _db.AuthSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash);
        if (session != null)
        {
            session.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<User?> GetCurrentUserAsync(long userId)
    {
        return await _db.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Role)
            .Include(u => u.Departments)
                .ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
