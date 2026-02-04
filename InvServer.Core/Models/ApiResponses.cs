namespace InvServer.Core.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? CorrelationId { get; set; }
}

public class ApiErrorResponse
{
    public bool Success => false;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
