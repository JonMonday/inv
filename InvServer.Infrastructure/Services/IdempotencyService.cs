using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly InvDbContext _db;

    public IdempotencyService(InvDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyResult> CheckOrInsertAsync(string key, long userId, string routeKey)
    {
        // Using a transaction to ensure atomic check and insert
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.IdempotencyKeys
                .FromSqlRaw("SELECT * FROM dbo.IDEMPOTENCY_KEY WITH (UPDLOCK, HOLDLOCK) WHERE [Key] = {0} AND UserId = {1} AND RouteKey = {2}",
                    key, userId, routeKey)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return new IdempotencyResult
                {
                    Exists = true,
                    IdempotencyKeyId = existing.IdempotencyKeyId,
                    StatusCode = existing.ResponseStatusCode,
                    ResponseJson = existing.ResponseJson
                };
            }

            var newKey = new IdempotencyKey
            {
                Key = key,
                UserId = userId,
                RouteKey = routeKey,
                Status = "PROCESSING",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24) // Default expiry
            };

            _db.IdempotencyKeys.Add(newKey);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new IdempotencyResult
            {
                Exists = false,
                IdempotencyKeyId = newKey.IdempotencyKeyId
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateResponseAsync(long idempotencyKeyId, int statusCode, string? responseJson)
    {
        var key = await _db.IdempotencyKeys.FindAsync(idempotencyKeyId);
        if (key != null)
        {
            key.ResponseStatusCode = statusCode;
            key.ResponseJson = responseJson;
            await _db.SaveChangesAsync();
        }
    }
}
