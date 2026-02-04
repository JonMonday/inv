using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using InvServer.Core.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace InvServer.Api.Filters;

public class IdempotencyAttribute : TypeFilterAttribute
{
    public IdempotencyAttribute() : base(typeof(IdempotencyFilter)) { }
}

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IIdempotencyService _idempotencyService;

    public IdempotencyFilter(IIdempotencyService idempotencyService)
    {
        _idempotencyService = idempotencyService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var keyValues))
        {
            await next();
            return;
        }

        var key = keyValues.ToString();
        var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
        {
            await next();
            return;
        }

        var routeKey = $"{context.HttpContext.Request.Method}:{context.HttpContext.Request.Path}";
        
        var result = await _idempotencyService.CheckOrInsertAsync(key, userId, routeKey);

        if (result.Exists && result.StatusCode != null)
        {
            context.Result = new ContentResult
            {
                StatusCode = result.StatusCode,
                Content = result.ResponseJson,
                ContentType = "application/json"
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult && !result.Exists)
        {
            var json = JsonSerializer.Serialize(objectResult.Value);
            await _idempotencyService.UpdateResponseAsync(result.IdempotencyKeyId!.Value, objectResult.StatusCode ?? 200, json);
        }
    }
}
