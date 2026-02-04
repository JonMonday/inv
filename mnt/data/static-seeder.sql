/* ============================================================
   STATIC SEEDER (LOOKUPS + ROLES + PERMISSIONS)
   ============================================================ */
SET NOCOUNT ON;

BEGIN TRAN;

---------------------------------------------------------------
-- 1) Access scopes (GLOBAL/DEPARTMENT/WAREHOUSE/OWN)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ACCESS_SCOPE_TYPE WHERE Code = 'GLOBAL')
INSERT dbo.ACCESS_SCOPE_TYPE (Code, Name) VALUES
('GLOBAL','Global access'),
('DEPARTMENT','Department-scoped access'),
('WAREHOUSE','Warehouse-scoped access'),
('OWN','Own records only');

---------------------------------------------------------------
-- 2) Security event types (audit for RBAC changes)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SECURITY_EVENT_TYPE WHERE Code = 'ROLE_ASSIGNED')
INSERT dbo.SECURITY_EVENT_TYPE (Code, Name) VALUES
('ROLE_ASSIGNED','Role assigned to user'),
('ROLE_REMOVED','Role removed from user'),
('PERMISSION_GRANTED_TO_ROLE','Permission granted to role'),
('PERMISSION_REVOKED_FROM_ROLE','Permission revoked from role'),
('USER_PERMISSION_OVERRIDE_SET','User permission override set'),
('USER_PERMISSION_OVERRIDE_REMOVED','User permission override removed');

---------------------------------------------------------------
-- 3) Inventory request types + statuses
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_TYPE WHERE Code = 'ISSUE')
INSERT dbo.INVENTORY_REQUEST_TYPE (Code, Name, IsActive) VALUES
('ISSUE','Issue Items Request',1),
('PURCHASE_REQ','Purchase Requisition',1),
('TRANSFER','Warehouse Transfer Request',1),
('ADJUSTMENT','Stock Adjustment Request',1),
('RETURN','Return Request',1);

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code = 'DRAFT')
INSERT dbo.INVENTORY_REQUEST_STATUS (Code, Name, IsTerminal) VALUES
('DRAFT','Draft',0),
('IN_WORKFLOW','In Workflow',0),
('APPROVED','Approved',0),
('FULFILLMENT','In Fulfillment',0),
('READY','Ready for Pickup/Delivery',0),
('FULFILLED','Fulfilled',1),
('REJECTED','Rejected',1),
('CANCELLED','Cancelled',1),
('WAITING_FOR_STOCK','Waiting for Stock',0),
('PARTIAL','Partially Fulfilled',0);

---------------------------------------------------------------
-- 4) Reservation status
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.RESERVATION_STATUS WHERE Code = 'ACTIVE')
INSERT dbo.RESERVATION_STATUS (Code, Name, IsTerminal) VALUES
('ACTIVE','Active',0),
('RELEASED','Released',1),
('CONSUMED','Consumed',1),
('EXPIRED','Expired',1),
('CANCELLED','Cancelled',1);

---------------------------------------------------------------
-- 5) Stock movement lookup tables (types, statuses, reason codes)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_MOVEMENT_STATUS WHERE Code = 'DRAFT')
INSERT dbo.INVENTORY_MOVEMENT_STATUS (Code, Name, IsTerminal) VALUES
('DRAFT','Draft',0),
('POSTED','Posted',1),
('REVERSED','Reversed',1);

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code = 'RECEIPT')
INSERT dbo.INVENTORY_MOVEMENT_TYPE (Code, Name) VALUES
('RECEIPT','Stock Receipt (Increase On-hand)'),
('ISSUE','Stock Issue (Decrease On-hand)'),
('RESERVE','Reserve Stock (Increase Reserved)'),
('RELEASE','Release Reservation (Decrease Reserved)'),
('TRANSFER_OUT','Transfer Out (Decrease Source On-hand)'),
('TRANSFER_IN','Transfer In (Increase Destination On-hand)'),
('ADJUSTMENT_IN','Adjustment In (Increase On-hand)'),
('ADJUSTMENT_OUT','Adjustment Out (Decrease On-hand)'),
('RETURN_IN','Return In (Increase On-hand)'),
('RETURN_OUT','Return Out (Decrease On-hand)');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REASON_CODE WHERE Code = 'PURCHASE_RECEIPT')
INSERT dbo.INVENTORY_REASON_CODE (Code, Name, RequiresApproval, IsActive) VALUES
('PURCHASE_RECEIPT','Purchase Receipt / GRN',0,1),
('CUSTOMER_ISSUE','Issue Items to Requester/Customer',0,1),
('RESERVE_FOR_PICKUP','Reserve Items for Pickup/Delivery',0,1),
('RELEASE_RESERVATION','Release Reserved Items',0,1),
('WAREHOUSE_TRANSFER','Warehouse Transfer',0,1),
('DAMAGE','Damaged Stock Write-off',1,1),
('THEFT','Theft / Loss Write-off',1,1),
('STOCKTAKE_GAIN','Stocktake Gain',1,1),
('STOCKTAKE_LOSS','Stocktake Loss',1,1),
('RETURN_FROM_USER','Return from User/Customer',0,1),
('RETURN_TO_SUPPLIER','Return to Supplier',1,1),
('MANUAL_CORRECTION','Manual Correction',1,1);

---------------------------------------------------------------
-- 6) Workflow lookups (step types, assignment modes, actions, operators, statuses)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_TYPE WHERE Code = 'START')
INSERT dbo.WORKFLOW_STEP_TYPE (Code, Name) VALUES
('START','Start'),
('REVIEW','Review'),
('APPROVAL','Approval'),
('FULFILLMENT','Fulfillment'),
('END','End');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_ASSIGNMENT_MODE WHERE Code = 'AUTO_ROLE')
INSERT dbo.WORKFLOW_ASSIGNMENT_MODE (Code, Name) VALUES
('AUTO_ROLE','Auto assign by role (optional dept filter)'),
('ROLE_POOL_CLAIM','Role pool claim'),
('SPECIFIC_USERS','Specific users'),
('REQUESTER_SELECTS','Requester selects assignees'),
('ADMIN_ASSIGN','Admin assigns');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code = 'SUBMIT')
INSERT dbo.WORKFLOW_ACTION_TYPE (Code, Name, IsDecision, IsSystemAction) VALUES
('SUBMIT','Submit',0,0),
('APPROVE','Approve',1,0),
('REJECT','Reject',1,0),
('SEND_BACK','Send Back',1,0),
('CANCEL','Cancel',1,0),
('CLAIM','Claim',0,0),
('REASSIGN','Reassign',0,0),
('DELEGATE','Delegate',0,0),
('COMPLETE','Complete',1,0),
('COMMENT','Comment',0,0);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_CONDITION_OPERATOR WHERE Code = 'EQ')
INSERT dbo.WORKFLOW_CONDITION_OPERATOR (Code, Name) VALUES
('EQ','Equals'),
('NEQ','Not Equals'),
('GT','Greater Than'),
('GTE','Greater Than Or Equal'),
('LT','Less Than'),
('LTE','Less Than Or Equal'),
('IN','In'),
('NOT_IN','Not In'),
('CONTAINS','Contains');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_INSTANCE_STATUS WHERE Code = 'DRAFT')
INSERT dbo.WORKFLOW_INSTANCE_STATUS (Code, Name, IsTerminal) VALUES
('DRAFT','Draft',0),
('SUBMITTED','Submitted',0),
('IN_PROGRESS','In Progress',0),
('APPROVED','Approved',0),
('REJECTED','Rejected',1),
('CANCELLED','Cancelled',1),
('COMPLETED','Completed',1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_STATUS WHERE Code = 'PENDING')
INSERT dbo.WORKFLOW_TASK_STATUS (Code, Name, IsTerminal) VALUES
('PENDING','Pending',0),
('AVAILABLE','Available',0),
('CLAIMED','Claimed',0),
('APPROVED','Approved',1),
('REJECTED','Rejected',1),
('CANCELLED','Cancelled',1),
('COMPLETED','Completed',1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ASSIGNEE_STATUS WHERE Code = 'PENDING')
INSERT dbo.WORKFLOW_TASK_ASSIGNEE_STATUS (Code, Name) VALUES
('PENDING','Pending'),
('APPROVED','Approved'),
('REJECTED','Rejected'),
('REMOVED','Removed'),
('DELEGATED','Delegated');

---------------------------------------------------------------
-- 7) Roles
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ROLE WHERE Name = 'Admin')
INSERT dbo.ROLE (Name, IsActive) VALUES
('Admin',1),
('Requester',1),
('Supervisor',1),
('FinanceOfficer',1),
('InventoryManager',1),
('Storekeeper',1),
('ProcurementOfficer',1),
('Auditor',1);

---------------------------------------------------------------
-- 8) Permissions (starter set)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISSION WHERE Code = 'inventory.request.create')
INSERT dbo.PERMISSION (Code, Name, Description, IsActive) VALUES
-- Inventory Requests
('inventory.request.create','Create inventory request','Create draft requests',1),
('inventory.request.edit_draft','Edit draft request','Edit only drafts created by user',1),
('inventory.request.submit','Submit request','Submit request to workflow',1),
('inventory.request.cancel','Cancel request','Cancel a request (subject to rules)',1),
('inventory.request.view','View requests','View requests within scope',1),

-- Workflow runtime
('workflow.task.view','View workflow tasks','View tasks within scope',1),
('workflow.task.claim','Claim workflow tasks','Claim a pooled task',1),
('workflow.task.approve','Approve workflow task','Approve tasks assigned to you',1),
('workflow.task.reject','Reject workflow task','Reject tasks assigned to you',1),
('workflow.task.send_back','Send back workflow task','Send back to requester',1),
('workflow.task.reassign','Reassign workflow task','Reassign tasks (admin/manager)',1),
('workflow.task.delegate','Delegate workflow task','Delegate tasks',1),

-- Workflow definition
('workflow.definition.manage','Manage workflow definitions','Create/update workflow templates',1),

-- Fulfillment actions
('inventory.reservation.create','Create reservation','Reserve stock (fulfillment only)',1),
('inventory.reservation.release','Release reservation','Release reserved stock',1),
('inventory.issue.post','Post issue movement','Issue stock (decrease on-hand)',1),
('inventory.receipt.post','Post receipt movement','Receive stock (increase on-hand)',1),
('inventory.transfer.post','Post transfer movement','Transfer stock between warehouses',1),
('inventory.adjustment.post','Post adjustment movement','Adjust stock up/down',1),
('inventory.return.post','Post return movement','Process returns in/out',1),

-- Catalog governance
('catalog.category.manage','Manage categories','Create/update categories',1),
('catalog.product.manage','Manage products','Create/update products',1),

-- RBAC admin
('admin.user.manage','Manage users','Create/update users',1),
('admin.role.manage','Manage roles','Create/update roles',1),
('admin.permission.manage','Manage permissions','Grant/revoke permissions',1);

---------------------------------------------------------------
-- 9) Role -> Permission grants + scopes
---------------------------------------------------------------
DECLARE @ScopeGlobalId bigint = (SELECT AccessScopeTypeId FROM dbo.ACCESS_SCOPE_TYPE WHERE Code='GLOBAL');
DECLARE @ScopeDeptId   bigint = (SELECT AccessScopeTypeId FROM dbo.ACCESS_SCOPE_TYPE WHERE Code='DEPARTMENT');
DECLARE @ScopeWhId     bigint = (SELECT AccessScopeTypeId FROM dbo.ACCESS_SCOPE_TYPE WHERE Code='WAREHOUSE');
DECLARE @ScopeOwnId    bigint = (SELECT AccessScopeTypeId FROM dbo.ACCESS_SCOPE_TYPE WHERE Code='OWN');

DECLARE @RoleAdmin bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Admin');
DECLARE @RoleRequester bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Requester');
DECLARE @RoleSupervisor bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Supervisor');
DECLARE @RoleFinance bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='FinanceOfficer');
DECLARE @RoleInvMgr bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='InventoryManager');
DECLARE @RoleStore bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Storekeeper');
DECLARE @RoleProc bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='ProcurementOfficer');
DECLARE @RoleAuditor bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Auditor');

-- Helper: grant permission to role if missing
DECLARE @p TABLE (PermissionCode varchar(200), RoleName varchar(50), ScopeCode varchar(30));

INSERT @p VALUES
-- Requester
('inventory.request.create','Requester','OWN'),
('inventory.request.edit_draft','Requester','OWN'),
('inventory.request.submit','Requester','OWN'),
('inventory.request.cancel','Requester','OWN'),
('inventory.request.view','Requester','OWN'),

-- Supervisor
('inventory.request.view','Supervisor','DEPARTMENT'),
('workflow.task.view','Supervisor','DEPARTMENT'),
('workflow.task.approve','Supervisor','DEPARTMENT'),
('workflow.task.reject','Supervisor','DEPARTMENT'),
('workflow.task.send_back','Supervisor','DEPARTMENT'),

-- Finance
('inventory.request.view','FinanceOfficer','DEPARTMENT'),
('workflow.task.view','FinanceOfficer','DEPARTMENT'),
('workflow.task.approve','FinanceOfficer','DEPARTMENT'),
('workflow.task.reject','FinanceOfficer','DEPARTMENT'),
('workflow.task.send_back','FinanceOfficer','DEPARTMENT'),

-- Inventory Manager
('inventory.request.view','InventoryManager','WAREHOUSE'),
('workflow.task.view','InventoryManager','WAREHOUSE'),
('workflow.task.approve','InventoryManager','WAREHOUSE'),
('workflow.task.reject','InventoryManager','WAREHOUSE'),
('workflow.task.send_back','InventoryManager','WAREHOUSE'),
('workflow.task.reassign','InventoryManager','WAREHOUSE'),

-- Storekeeper (Fulfillment + stock ops)
('inventory.request.view','Storekeeper','WAREHOUSE'),
('workflow.task.view','Storekeeper','WAREHOUSE'),
('workflow.task.claim','Storekeeper','WAREHOUSE'),
('inventory.reservation.create','Storekeeper','WAREHOUSE'),
('inventory.reservation.release','Storekeeper','WAREHOUSE'),
('inventory.issue.post','Storekeeper','WAREHOUSE'),
('inventory.receipt.post','Storekeeper','WAREHOUSE'),
('inventory.return.post','Storekeeper','WAREHOUSE'),

-- Procurement
('inventory.request.view','ProcurementOfficer','DEPARTMENT'),
('workflow.task.view','ProcurementOfficer','DEPARTMENT'),
('workflow.task.approve','ProcurementOfficer','DEPARTMENT'),
('workflow.task.reject','ProcurementOfficer','DEPARTMENT'),
('inventory.receipt.post','ProcurementOfficer','DEPARTMENT'),

-- Auditor (read-only global)
('inventory.request.view','Auditor','GLOBAL'),
('workflow.task.view','Auditor','GLOBAL'),

-- Admin (everything global)
('admin.user.manage','Admin','GLOBAL'),
('admin.role.manage','Admin','GLOBAL'),
('admin.permission.manage','Admin','GLOBAL'),
('workflow.definition.manage','Admin','GLOBAL'),
('catalog.category.manage','Admin','GLOBAL'),
('catalog.product.manage','Admin','GLOBAL'),
('inventory.adjustment.post','Admin','GLOBAL'),
('inventory.transfer.post','Admin','GLOBAL'),
('inventory.request.view','Admin','GLOBAL'),
('workflow.task.view','Admin','GLOBAL'),
('workflow.task.reassign','Admin','GLOBAL');

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT PermissionCode, RoleName, ScopeCode FROM @p;

DECLARE @PermissionCode varchar(200), @RoleName varchar(50), @ScopeCode varchar(30);
OPEN cur;
FETCH NEXT FROM cur INTO @PermissionCode, @RoleName, @ScopeCode;

WHILE @@FETCH_STATUS = 0
BEGIN
  DECLARE @RoleId bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name=@RoleName);
  DECLARE @PermId bigint = (SELECT PermissionId FROM dbo.PERMISSION WHERE Code=@PermissionCode);
  DECLARE @ScopeId bigint = (SELECT AccessScopeTypeId FROM dbo.ACCESS_SCOPE_TYPE WHERE Code=@ScopeCode);

  IF NOT EXISTS (
    SELECT 1 FROM dbo.ROLE_PERMISSION
    WHERE RoleId=@RoleId AND PermissionId=@PermId
  )
    INSERT dbo.ROLE_PERMISSION (RoleId, PermissionId, GrantedAt, GrantedByUserId)
    VALUES (@RoleId, @PermId, SYSUTCDATETIME(), NULL);

  DECLARE @RolePermId bigint =
    (SELECT RolePermissionId FROM dbo.ROLE_PERMISSION WHERE RoleId=@RoleId AND PermissionId=@PermId);

  IF NOT EXISTS (
    SELECT 1 FROM dbo.ROLE_PERMISSION_SCOPE
    WHERE RolePermissionId=@RolePermId AND AccessScopeTypeId=@ScopeId
  )
    INSERT dbo.ROLE_PERMISSION_SCOPE (RolePermissionId, AccessScopeTypeId, DepartmentId, WarehouseId)
    VALUES (@RolePermId, @ScopeId, NULL, NULL);

  FETCH NEXT FROM cur INTO @PermissionCode, @RoleName, @ScopeCode;
END

CLOSE cur;
DEALLOCATE cur;

COMMIT TRAN;
