using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using InvServer.Infrastructure;
using System.Text.Json;

namespace InvServer.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly InvDbContext _db;
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "Secret", "Token", "RefreshTokenHash", "SecurityStamp"
    };

    public AuditService(InvDbContext db)
    {
        _db = db;
    }

    public async Task LogChangeAsync(long userId, string action, object oldVal, object newVal)
    {
        var diff = new Dictionary<string, object?>();
        var properties = oldVal.GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (SensitiveFields.Contains(prop.Name)) continue;

            var v1 = prop.GetValue(oldVal);
            var p2 = newVal.GetType().GetProperty(prop.Name);
            var v2 = p2?.GetValue(newVal);

            if (!Equals(v1, v2))
            {
                diff[prop.Name] = new { Old = v1, New = v2 };
            }
        }

        if (diff.Count > 0)
        {
            var log = new AuditLog
            {
                UserId = userId,
                EventType = "DATA_CHANGE",
                Action = action,
                PayloadJson = JsonSerializer.Serialize(diff),
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
