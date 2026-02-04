using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("STOCK_MOVEMENT")]
public class StockMovement
{
    [Key]
    public long StockMovementId { get; set; }

    public long MovementTypeId { get; set; }
    [ForeignKey(nameof(MovementTypeId))]
    public InventoryMovementType MovementType { get; set; } = null!;

    public long MovementStatusId { get; set; }

    public long? ReasonCodeId { get; set; }

    public long WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public long? ReferenceRequestId { get; set; }
    [ForeignKey(nameof(ReferenceRequestId))]
    public InventoryRequest? ReferenceRequest { get; set; }

    public long? ReferenceReservationId { get; set; }
    [ForeignKey(nameof(ReferenceReservationId))]
    public Reservation? ReferenceReservation { get; set; }

    public long CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long? PostedByUserId { get; set; }
    [ForeignKey(nameof(PostedByUserId))]
    public User? PostedByUser { get; set; }

    public DateTime? PostedAt { get; set; }

    public long? ReversedMovementId { get; set; }

    public string? Notes { get; set; }

    public ICollection<StockMovementLine> Lines { get; set; } = new List<StockMovementLine>();
}

[Table("STOCK_MOVEMENT_LINE")]
public class StockMovementLine
{
    [Key]
    public long StockMovementLineId { get; set; }

    public long StockMovementId { get; set; }
    [ForeignKey(nameof(StockMovementId))]
    public StockMovement StockMovement { get; set; } = null!;

    public long ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public decimal QtyDeltaOnHand { get; set; } = 0;
    public decimal QtyDeltaReserved { get; set; } = 0;

    public decimal? UnitCost { get; set; }

    public string? LineNotes { get; set; }
}
