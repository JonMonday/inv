using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("INVENTORY_REQUEST_STATUS")]
public class InventoryRequestStatus
{
    [Key]
    public long RequestStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
}

[Table("WORKFLOW_TASK_ASSIGNEE_STATUS")]
public class WorkflowTaskAssigneeStatus
{
    [Key]
    public long AssigneeStatusId { get; set; }
    public bool IsTerminal { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[Table("INVENTORY_MOVEMENT_TYPE")]
public class InventoryMovementType
{
    [Key]
    public long MovementTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[Table("INVENTORY_MOVEMENT_STATUS")]
public class InventoryMovementStatus
{
    [Key]
    public long MovementStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
}

[Table("INVENTORY_REASON_CODE")]
public class InventoryReasonCode
{
    [Key]
    public long ReasonCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; }
}

[Table("INVENTORY_REQUEST_TYPE")]
public class InventoryRequestType
{
    [Key]
    public long RequestTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

[Table("RESERVATION_STATUS")]
public class ReservationStatus
{
    [Key]
    public long ReservationStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
}
