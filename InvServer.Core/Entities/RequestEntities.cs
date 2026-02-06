using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("INVENTORY_REQUEST")]
public class InventoryRequest
{
    [Key]
    public long RequestId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequestNo { get; set; } = string.Empty;

    public long RequestTypeId { get; set; }

    public long RequestStatusId { get; set; }
    [ForeignKey(nameof(RequestStatusId))]
    public InventoryRequestStatus Status { get; set; } = null!;

    public long WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public long RequestedByUserId { get; set; }
    [ForeignKey(nameof(RequestedByUserId))]
    public User RequestedByUser { get; set; } = null!;

    public long DepartmentId { get; set; }
    [ForeignKey(nameof(DepartmentId))]
    public Department Department { get; set; } = null!;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public long? WorkflowInstanceId { get; set; }
    [ForeignKey(nameof(WorkflowInstanceId))]
    public WorkflowInstance? WorkflowInstance { get; set; }

    public long? WorkflowTemplateId { get; set; }
    [ForeignKey(nameof(WorkflowTemplateId))]
    public WorkflowTemplate? WorkflowTemplate { get; set; }

    public ICollection<InventoryRequestLine> Lines { get; set; } = new List<InventoryRequestLine>();
}

[Table("INVENTORY_REQUEST_LINE")]
public class InventoryRequestLine
{
    [Key]
    public long RequestLineId { get; set; }

    public long RequestId { get; set; }
    [ForeignKey(nameof(RequestId))]
    public InventoryRequest Request { get; set; } = null!;

    public long ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public decimal QtyRequested { get; set; }
    public decimal? QtyApproved { get; set; }
    public decimal QtyFulfilled { get; set; } = 0;

    public string? LineNotes { get; set; }
}

[Table("RESERVATION")]
public class Reservation
{
    [Key]
    public long ReservationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReservationNo { get; set; } = string.Empty;

    public long ReservationStatusId { get; set; }
    [ForeignKey(nameof(ReservationStatusId))]
    public ReservationStatus ReservationStatus { get; set; } = null!;

    public long WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public long? RequestId { get; set; }
    [ForeignKey(nameof(RequestId))]
    public InventoryRequest? Request { get; set; }

    public long ReservedByUserId { get; set; }
    [ForeignKey(nameof(ReservedByUserId))]
    public User ReservedByUser { get; set; } = null!;

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<ReservationLine> Lines { get; set; } = new List<ReservationLine>();
}

[Table("RESERVATION_LINE")]
public class ReservationLine
{
    [Key]
    public long ReservationLineId { get; set; }

    public long ReservationId { get; set; }
    [ForeignKey(nameof(ReservationId))]
    public Reservation Reservation { get; set; } = null!;

    public long? RequestLineId { get; set; }
    [ForeignKey(nameof(RequestLineId))]
    public InventoryRequestLine? RequestLine { get; set; }

    public long ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public decimal QtyReserved { get; set; }
}
