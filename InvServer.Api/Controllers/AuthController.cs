using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private bool ValidateOrigin()
    {
        var origin = Request.Headers["Origin"].ToString();
        var referer = Request.Headers["Referer"].ToString();
        var host = Request.Headers["Host"].ToString();

        // In production, compare against WHITELIST
        if (string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(referer)) return false;
        return true;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(request, ip, userAgent);
        if (result == null)
            return Unauthorized(new ApiErrorResponse { Message = "Invalid username or password." });

        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);

        return Ok(new ApiResponse<AuthResponse> { Data = result with { RefreshToken = "" } }); // Hide from body if using cookie
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth_refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh()
    {
        if (!ValidateOrigin()) return BadRequest("Invalid origin.");
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.RefreshTokenAsync(refreshToken, ip, userAgent);
        if (result == null)
            return Unauthorized();

        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);

        return Ok(new ApiResponse<AuthResponse> { Data = result with { RefreshToken = "" } });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!ValidateOrigin()) return BadRequest("Invalid origin.");
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }

        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetMe()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdStr, out var userId))
        {
            var user = await _authService.GetCurrentUserAsync(userId);
            if (user != null)
            {
                return Ok(new ApiResponse<object>
                {
                    Data = new
                    {
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.DisplayName,
                        Roles = user.Roles.Select(r => r.Role.Name),
                        Departments = user.Departments.Select(d => d.Department.Name)
                    }
                });
            }
        }
        return NotFound();
    }

    private void SetRefreshTokenCookie(string token, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
