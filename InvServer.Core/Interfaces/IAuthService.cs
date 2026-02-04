using InvServer.Core.Entities;

namespace InvServer.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo);
    Task LogoutAsync(string refreshToken);
    Task<User?> GetCurrentUserAsync(long userId);
}

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    long? ValidateAccessToken(string token, out int permVersion);
}

public record LoginRequest(string Username, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
