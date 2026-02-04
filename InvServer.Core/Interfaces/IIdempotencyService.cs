namespace InvServer.Core.Interfaces;

public interface IIdempotencyService
{
    Task<IdempotencyResult> CheckOrInsertAsync(string key, long userId, string routeKey);
    Task UpdateResponseAsync(long idempotencyKeyId, int statusCode, string? responseJson);
}

public class IdempotencyResult
{
    public bool Exists { get; set; }
    public long? IdempotencyKeyId { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseJson { get; set; }
}
