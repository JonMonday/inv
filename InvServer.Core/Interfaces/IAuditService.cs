namespace InvServer.Core.Interfaces;

public interface IAuditService
{
    Task LogChangeAsync(long userId, string action, object oldVal, object newVal);
}
