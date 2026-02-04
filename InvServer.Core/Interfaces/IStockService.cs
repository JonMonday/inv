namespace InvServer.Core.Interfaces;

public interface IStockService
{
    Task<long> PostMovementAsync(StockMovementRequest request, string? correlationId = null, string? routeKey = null, string? idempotencyKey = null);
}

public class StockMovementRequest
{
    public string MovementTypeCode { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public long WarehouseId { get; set; }
    public long? RequestId { get; set; }
    public long? ReservationId { get; set; }
    public long UserId { get; set; }
    public string? Notes { get; set; }
    public List<StockMovementLineRequest> Lines { get; set; } = new();
}

public class StockMovementLineRequest
{
    public long ProductId { get; set; }
    public decimal QtyDeltaOnHand { get; set; }
    public decimal QtyDeltaReserved { get; set; }
    public decimal? UnitCost { get; set; }
    public string? LineNotes { get; set; }
}
