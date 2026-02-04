using InvServer.Core.Interfaces;
using InvServer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace InvServer.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IFilterFactory
{
    public string PermissionCode { get; }
    public AccessScope? MinimumScope { get; }

    public RequirePermissionAttribute(string permissionCode, AccessScope minimumScope = AccessScope.OWN)
    {
        PermissionCode = permissionCode;
        MinimumScope = minimumScope;
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<PermissionFilter>();
    }
}

public class PermissionFilter : IAsyncActionFilter
{
    private readonly IAuthorizationService _authService;

    public PermissionFilter(IAuthorizationService authService)
    {
        _authService = authService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Get Attribute from Action or Controller
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            await next();
            return;
        }

        // 2. Resolve User
        var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdStr, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 3. Check Permission
        // For simple attribute checks, we don't know the entityId yet (e.g. WarehouseId from route).
        // If a specific scope check needs an ID, we'd need to extract it from context.
        var hasPermission = await _authService.HasPermissionAsync(userId, attribute.PermissionCode);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new ApiErrorResponse 
            { 
                Message = $"Forbidden: Missing permission {attribute.PermissionCode}",
                CorrelationId = context.HttpContext.Items["CorrelationId"]?.ToString()
            }) 
            { 
                StatusCode = StatusCodes.Status403Forbidden 
            };
            return;
        }

        await next();
    }
}
