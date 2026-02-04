namespace InvServer.Core.Constants;

public static class MovementTypeCodes
{
    public const string Receipt = "RECEIPT";
    public const string Issue = "ISSUE";
    public const string ConsumeReserve = "CONSUME_RESERVE";
    public const string Reserve = "RESERVE";
    public const string Release = "RELEASE";
    public const string TransferOut = "TRANSFER_OUT";
    public const string TransferIn = "TRANSFER_IN";
    public const string AdjustmentIn = "ADJUSTMENT_IN";
    public const string AdjustmentOut = "ADJUSTMENT_OUT";
    public const string ReturnIn = "RETURN_IN";
    public const string ReturnOut = "RETURN_OUT";
}

public static class MovementStatusCodes
{
    public const string Draft = "DRAFT";
    public const string Posted = "POSTED";
    public const string Reversed = "REVERSED";
}

public static class RequestStatusCodes
{
    public const string Draft = "DRAFT";
    public const string InWorkflow = "IN_WORKFLOW";
    public const string Approved = "APPROVED";
    public const string Fulfillment = "FULFILLMENT";
    public const string Ready = "READY";
    public const string Fulfilled = "FULFILLED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
    public const string WaitingForStock = "WAITING_FOR_STOCK";
}

public static class WorkflowActionCodes
{
    public const string Submit = "SUBMIT";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string SendBack = "SEND_BACK";
    public const string Cancel = "CANCEL";
    public const string Claim = "CLAIM";
    public const string Complete = "COMPLETE";
}

public static class WorkflowTaskStatusCodes
{
    public const string Pending = "PENDING";
    public const string Available = "AVAILABLE";
    public const string Claimed = "CLAIMED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
    public const string Completed = "COMPLETED";
}

public static class WorkflowStepTypeCodes
{
    public const string Start = "START";
    public const string Review = "REVIEW";
    public const string Approval = "APPROVAL";
    public const string Fulfillment = "FULFILLMENT";
    public const string End = "END";
}

public static class WorkflowInstanceStatusCodes
{
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string Cancelled = "CANCELLED";
    public const string Terminated = "TERMINATED";
}

public static class WorkflowAssignmentModeCodes
{
    public const string Role = "ROLE";
    public const string Department = "DEPT";
    public const string RequestorDepartment = "REQ_DEPT";
    public const string Requestor = "REQ";
    public const string RequestorRole = "REQ_ROLE";
    public const string RoleAndDepartment = "ROLE_DEPT";
    public const string RequestorRoleAndDepartment = "REQ_ROLE_DEPT";
}

public static class WorkflowTaskAssigneeStatusCodes
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Removed = "REMOVED";
    public const string Delegated = "DELEGATED";
}
