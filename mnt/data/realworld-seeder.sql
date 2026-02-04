/* ============================================================
   REAL WORLD SEEDER (USERS + CATALOG + WORKFLOWS + SAMPLE DATA)
   ============================================================ */
SET NOCOUNT ON;

BEGIN TRAN;

---------------------------------------------------------------
-- 1) Departments
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DEPARTMENT WHERE Name='Operations')
INSERT dbo.DEPARTMENT (Name, IsActive) VALUES
('Operations',1),
('Finance',1),
('Procurement',1),
('Warehouse',1),
('IT',1);

DECLARE @DeptOps bigint = (SELECT DepartmentId FROM dbo.DEPARTMENT WHERE Name='Operations');
DECLARE @DeptFin bigint = (SELECT DepartmentId FROM dbo.DEPARTMENT WHERE Name='Finance');
DECLARE @DeptProc bigint = (SELECT DepartmentId FROM dbo.DEPARTMENT WHERE Name='Procurement');
DECLARE @DeptWh bigint = (SELECT DepartmentId FROM dbo.DEPARTMENT WHERE Name='Warehouse');

---------------------------------------------------------------
-- 2) Warehouses
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.WAREHOUSE WHERE Name='Main Store')
INSERT dbo.WAREHOUSE (Name, Location, IsActive) VALUES
('Main Store','HQ',1),
('Branch Store','Branch A',1);

DECLARE @WhMain bigint = (SELECT WarehouseId FROM dbo.WAREHOUSE WHERE Name='Main Store');
DECLARE @WhBranch bigint = (SELECT WarehouseId FROM dbo.WAREHOUSE WHERE Name='Branch Store');

---------------------------------------------------------------
-- 3) Users (realistic)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.[USER] WHERE Username='admin')
INSERT dbo.[USER] (Username, Email, DisplayName, IsActive, CreatedAt) VALUES
('admin','admin@example.com','System Admin',1,SYSUTCDATETIME()),
('alice','alice@example.com','Alice Requester',1,SYSUTCDATETIME()),
('bob','bob@example.com','Bob Supervisor',1,SYSUTCDATETIME()),
('fatou','fatou@example.com','Fatou Finance',1,SYSUTCDATETIME()),
('lamin','lamin@example.com','Lamin Inventory Manager',1,SYSUTCDATETIME()),
('mariama','mariama@example.com','Mariama Storekeeper',1,SYSUTCDATETIME()),
('ebrima','ebrima@example.com','Ebrima Procurement',1,SYSUTCDATETIME()),
('aminata','aminata@example.com','Aminata Auditor',1,SYSUTCDATETIME());

DECLARE @UAdmin bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='admin');
DECLARE @UAlice bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='alice');
DECLARE @UBob bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='bob');
DECLARE @UFatou bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='fatou');
DECLARE @ULamin bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='lamin');
DECLARE @UMariama bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='mariama');
DECLARE @UEbrima bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='ebrima');
DECLARE @UAud bigint = (SELECT UserId FROM dbo.[USER] WHERE Username='aminata');

---------------------------------------------------------------
-- 4) User departments
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.USER_DEPARTMENT WHERE UserId=@UAlice AND DepartmentId=@DeptOps)
INSERT dbo.USER_DEPARTMENT (UserId, DepartmentId, IsPrimary, AssignedAt) VALUES
(@UAlice,@DeptOps,1,SYSUTCDATETIME()),
(@UBob,@DeptOps,1,SYSUTCDATETIME()),
(@UFatou,@DeptFin,1,SYSUTCDATETIME()),
(@ULamin,@DeptWh,1,SYSUTCDATETIME()),
(@UMariama,@DeptWh,1,SYSUTCDATETIME()),
(@UEbrima,@DeptProc,1,SYSUTCDATETIME()),
(@UAdmin,@DeptIT,1,SYSUTCDATETIME()),
(@UAud,@DeptFin,1,SYSUTCDATETIME());

---------------------------------------------------------------
-- 5) Assign roles to users
---------------------------------------------------------------
DECLARE @RAdmin bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Admin');
DECLARE @RRequester bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Requester');
DECLARE @RSuper bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Supervisor');
DECLARE @RFin bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='FinanceOfficer');
DECLARE @RInv bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='InventoryManager');
DECLARE @RStore bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Storekeeper');
DECLARE @RProc bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='ProcurementOfficer');
DECLARE @RAud bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Auditor');

IF NOT EXISTS (SELECT 1 FROM dbo.USER_ROLE WHERE UserId=@UAdmin AND RoleId=@RAdmin)
INSERT dbo.USER_ROLE (UserId, RoleId, AssignedAt) VALUES
(@UAdmin,@RAdmin,SYSUTCDATETIME()),
(@UAlice,@RRequester,SYSUTCDATETIME()),
(@UBob,@RSuper,SYSUTCDATETIME()),
(@UFatou,@RFin,SYSUTCDATETIME()),
(@ULamin,@RInv,SYSUTCDATETIME()),
(@UMariama,@RStore,SYSUTCDATETIME()),
(@UEbrima,@RProc,SYSUTCDATETIME()),
(@UAud,@RAud,SYSUTCDATETIME());

---------------------------------------------------------------
-- 6) Categories + Products
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.CATEGORY WHERE Name='Stationery')
INSERT dbo.CATEGORY (Name, ParentCategoryId, IsActive) VALUES
('Stationery',NULL,1),
('IT Equipment',NULL,1),
('Cleaning',NULL,1);

DECLARE @CatStat bigint = (SELECT CategoryId FROM dbo.CATEGORY WHERE Name='Stationery');
DECLARE @CatIT bigint = (SELECT CategoryId FROM dbo.CATEGORY WHERE Name='IT Equipment');
DECLARE @CatClean bigint = (SELECT CategoryId FROM dbo.CATEGORY WHERE Name='Cleaning');

IF NOT EXISTS (SELECT 1 FROM dbo.PRODUCT WHERE SKU='ST-A4-001')
INSERT dbo.PRODUCT (SKU, Name, CategoryId, UnitOfMeasure, IsActive) VALUES
('ST-A4-001','A4 Paper (Ream)',@CatStat,'ream',1),
('IT-MSE-001','USB Mouse',@CatIT,'piece',1),
('IT-KBD-001','Keyboard',@CatIT,'piece',1),
('IT-TNR-001','Laser Toner',@CatIT,'piece',1),
('CL-BLC-001','Bleach (1L)',@CatClean,'bottle',1);

DECLARE @PA4 bigint = (SELECT ProductId FROM dbo.PRODUCT WHERE SKU='ST-A4-001');
DECLARE @PMouse bigint = (SELECT ProductId FROM dbo.PRODUCT WHERE SKU='IT-MSE-001');
DECLARE @PKeyboard bigint = (SELECT ProductId FROM dbo.PRODUCT WHERE SKU='IT-KBD-001');
DECLARE @PToner bigint = (SELECT ProductId FROM dbo.PRODUCT WHERE SKU='IT-TNR-001');
DECLARE @PBleach bigint = (SELECT ProductId FROM dbo.PRODUCT WHERE SKU='CL-BLC-001');

---------------------------------------------------------------
-- 7) Stock levels (initial snapshot)
---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_LEVEL WHERE WarehouseId=@WhMain AND ProductId=@PA4)
INSERT dbo.STOCK_LEVEL (WarehouseId, ProductId, OnHandQty, ReservedQty, UpdatedAt) VALUES
(@WhMain,@PA4,200,0,SYSUTCDATETIME()),
(@WhMain,@PMouse,50,0,SYSUTCDATETIME()),
(@WhMain,@PKeyboard,25,0,SYSUTCDATETIME()),
(@WhMain,@PToner,2,0,SYSUTCDATETIME()),   -- intentionally low for backorder example
(@WhMain,@PBleach,60,0,SYSUTCDATETIME()),
(@WhBranch,@PA4,50,0,SYSUTCDATETIME()),
(@WhBranch,@PMouse,10,0,SYSUTCDATETIME());

---------------------------------------------------------------
-- 8) WORKFLOW DEFINITIONS (templates) + versions + steps + rules + transitions
--    We'll seed 5 workflows:
--      WF_INV_ISSUE, WF_PROCURE, WF_ADJUST, WF_TRANSFER, WF_RETURN, WF_CATALOG
---------------------------------------------------------------
DECLARE @StepTypeStart bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='START');
DECLARE @StepTypeApproval bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='APPROVAL');
DECLARE @StepTypeFulfill bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='FULFILLMENT');
DECLARE @StepTypeEnd bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='END');

DECLARE @ModeAutoRole bigint = (SELECT AssignmentModeId FROM dbo.WORKFLOW_ASSIGNMENT_MODE WHERE Code='AUTO_ROLE');

DECLARE @ActSubmit bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='SUBMIT');
DECLARE @ActApprove bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='APPROVE');
DECLARE @ActReject bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='REJECT');
DECLARE @ActSendBack bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='SEND_BACK');
DECLARE @ActCancel bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='CANCEL');
DECLARE @ActComplete bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='COMPLETE');

-- Helper to create definition+version if missing
DECLARE @WFIssueDefId bigint, @WFIssueVerId bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_INV_ISSUE')
BEGIN
  INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
  VALUES ('WF_INV_ISSUE','Inventory Issue Request Workflow',1,@UAdmin,SYSUTCDATETIME());
END
SET @WFIssueDefId = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_INV_ISSUE');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@WFIssueDefId AND VersionNo=1)
BEGIN
  INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
  VALUES (@WFIssueDefId,1,1,SYSUTCDATETIME(),@UAdmin);
END
SET @WFIssueVerId = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@WFIssueDefId AND VersionNo=1);

-- Steps for Issue workflow (includes explicit end steps)
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@WFIssueVerId,'START','Start',@StepTypeStart,1,1),
(@WFIssueVerId,'MGR_APPROVAL','Manager Approval',@StepTypeApproval,2,1),
(@WFIssueVerId,'FIN_APPROVAL','Finance Approval',@StepTypeApproval,3,1),
(@WFIssueVerId,'INV_APPROVAL','Inventory Approval',@StepTypeApproval,4,1),
(@WFIssueVerId,'FULFILL','Fulfillment (Storekeeper)',@StepTypeFulfill,5,1),
(@WFIssueVerId,'END_OK','End - Completed',@StepTypeEnd,6,1),
(@WFIssueVerId,'END_REJECT','End - Rejected',@StepTypeEnd,7,1),
(@WFIssueVerId,'END_CANCEL','End - Cancelled',@StepTypeEnd,8,1);

DECLARE @SStart bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='START');
DECLARE @SMgr bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='MGR_APPROVAL');
DECLARE @SFin bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='FIN_APPROVAL');
DECLARE @SInv bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='INV_APPROVAL');
DECLARE @SFul bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='FULFILL');
DECLARE @SEndOk bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='END_OK');
DECLARE @SEndRej bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='END_REJECT');
DECLARE @SEndCan bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND StepKey='END_CANCEL');

-- Step rules (role + dept filters; fulfillment-only reservation happens operationally, not in rule)
-- Manager: Supervisor, use requester's department
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@SMgr)
INSERT dbo.WORKFLOW_STEP_RULE
(WorkflowStepId, AssignmentModeId, RoleId, DepartmentId, UseRequesterDepartment, AllowRequesterSelect,
 MinApprovers, RequireAll, AllowReassign, AllowDelegate, SLA_Minutes)
VALUES (@SMgr,@ModeAutoRole,@RSuper,NULL,1,0,1,0,0,0,1440);

-- Finance: FinanceOfficer, fixed Finance dept
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@SFin)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@SFin,@ModeAutoRole,@RFin,@DeptFin,0,0,1,0,0,0,1440);

-- Inventory approval: InventoryManager, Warehouse dept
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@SInv)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@SInv,@ModeAutoRole,@RInv,@DeptWh,0,0,1,0,0,0,1440);

-- Fulfillment: Storekeeper, Warehouse dept
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@SFul)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@SFul,@ModeAutoRole,@RStore,@DeptWh,0,0,1,0,0,0,1440);

-- START rule (optional; not required but can exist)
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@SStart)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@SStart,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

-- Transitions for issue workflow
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SStart AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId) VALUES
(@WFIssueVerId,@SStart,@ActSubmit,@SMgr);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SMgr AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SMgr,@ActApprove,@SFin);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SMgr AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SMgr,@ActReject,@SEndRej);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SMgr AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SMgr,@ActSendBack,@SStart);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFin AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SFin,@ActApprove,@SInv);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFin AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SFin,@ActReject,@SEndRej);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFin AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SFin,@ActSendBack,@SStart);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SInv AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SInv,@ActApprove,@SFul);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SInv AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SInv,@ActReject,@SEndRej);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SInv AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SInv,@ActSendBack,@SStart);

-- Fulfillment completion ends workflow
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFul AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES
(@WFIssueVerId,@SFul,@ActComplete,@SEndOk);

-- Cancel path (from any step -> END_CANCEL)
-- We'll seed for key steps (START, approvals, fulfillment)
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SMgr AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@WFIssueVerId,@SMgr,@ActCancel,@SEndCan);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFin AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@WFIssueVerId,@SFin,@ActCancel,@SEndCan);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SInv AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@WFIssueVerId,@SInv,@ActCancel,@SEndCan);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@WFIssueVerId AND FromWorkflowStepId=@SFul AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@WFIssueVerId,@SFul,@ActCancel,@SEndCan);

---------------------------------------------------------------
-- 9) Seed a realistic ISSUE request that is fulfilled with reservation (fulfillment-only)
---------------------------------------------------------------
DECLARE @ReqTypeIssue bigint = (SELECT RequestTypeId FROM dbo.INVENTORY_REQUEST_TYPE WHERE Code='ISSUE');
DECLARE @ReqStatInWf bigint = (SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='IN_WORKFLOW');
DECLARE @ReqStatFulfilled bigint = (SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='FULFILLED');
DECLARE @ReqStatReady bigint = (SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='READY');
DECLARE @ReqStatFulfillment bigint = (SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='FULFILLMENT');

DECLARE @InstStatusInProg bigint = (SELECT WorkflowInstanceStatusId FROM dbo.WORKFLOW_INSTANCE_STATUS WHERE Code='IN_PROGRESS');
DECLARE @InstStatusCompleted bigint = (SELECT WorkflowInstanceStatusId FROM dbo.WORKFLOW_INSTANCE_STATUS WHERE Code='COMPLETED');
DECLARE @TaskStatusPending bigint = (SELECT WorkflowTaskStatusId FROM dbo.WORKFLOW_TASK_STATUS WHERE Code='PENDING');
DECLARE @TaskStatusApproved bigint = (SELECT WorkflowTaskStatusId FROM dbo.WORKFLOW_TASK_STATUS WHERE Code='APPROVED');
DECLARE @TaskStatusCompleted bigint = (SELECT WorkflowTaskStatusId FROM dbo.WORKFLOW_TASK_STATUS WHERE Code='COMPLETED');

DECLARE @AssPend bigint = (SELECT AssigneeStatusId FROM dbo.WORKFLOW_TASK_ASSIGNEE_STATUS WHERE Code='PENDING');
DECLARE @AssApp bigint = (SELECT AssigneeStatusId FROM dbo.WORKFLOW_TASK_ASSIGNEE_STATUS WHERE Code='APPROVED');

-- Create request if not exists
DECLARE @Req1Id bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST WHERE RequestNo='IR-2026-0001')
BEGIN
  INSERT dbo.INVENTORY_REQUEST (RequestNo, RequestTypeId, RequestStatusId, WarehouseId, RequestedByUserId, RequestedAt, Notes, WorkflowInstanceId)
  VALUES ('IR-2026-0001',@ReqTypeIssue,@ReqStatInWf,@WhMain,@UAlice,SYSUTCDATETIME(),'Stationery + mouse request',NULL);
END
SET @Req1Id = (SELECT RequestId FROM dbo.INVENTORY_REQUEST WHERE RequestNo='IR-2026-0001');

-- Lines
IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@Req1Id AND ProductId=@PA4)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@Req1Id,@PA4,10,10,0,'A4 paper for ops');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@Req1Id AND ProductId=@PMouse)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@Req1Id,@PMouse,2,2,0,'Mice for new PCs');

DECLARE @Req1LineA4 bigint = (SELECT RequestLineId FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@Req1Id AND ProductId=@PA4);
DECLARE @Req1LineMouse bigint = (SELECT RequestLineId FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@Req1Id AND ProductId=@PMouse);

-- Create workflow instance and attach to request
DECLARE @WfInst1 bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_INSTANCE WHERE BusinessEntityKey='IR-2026-0001')
BEGIN
  INSERT dbo.WORKFLOW_INSTANCE
  (WorkflowDefinitionVersionId, WorkflowInstanceStatusId, InitiatorUserId, BusinessEntityKey, CurrentWorkflowStepId, StartedAt, CompletedAt)
  VALUES (@WFIssueVerId, @InstStatusInProg, @UAlice, 'IR-2026-0001', @SMgr, SYSUTCDATETIME(), NULL);
END
SET @WfInst1 = (SELECT WorkflowInstanceId FROM dbo.WORKFLOW_INSTANCE WHERE BusinessEntityKey='IR-2026-0001');

UPDATE dbo.INVENTORY_REQUEST
SET WorkflowInstanceId=@WfInst1
WHERE RequestId=@Req1Id AND WorkflowInstanceId IS NULL;

-- Manager task (approved)
DECLARE @TaskMgr1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SMgr)
BEGIN
  INSERT dbo.WORKFLOW_TASK (WorkflowInstanceId, WorkflowStepId, WorkflowTaskStatusId, CreatedAt, DueAt, ClaimedByUserId, CompletedAt)
  VALUES (@WfInst1,@SMgr,@TaskStatusApproved,DATEADD(minute,-30,SYSUTCDATETIME()),NULL,NULL,DATEADD(minute,-25,SYSUTCDATETIME()));
END
SET @TaskMgr1 = (SELECT WorkflowTaskId FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SMgr);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ASSIGNEE WHERE WorkflowTaskId=@TaskMgr1 AND UserId=@UBob)
INSERT dbo.WORKFLOW_TASK_ASSIGNEE (WorkflowTaskId, UserId, AssigneeStatusId, AssignedAt, DecidedAt)
VALUES (@TaskMgr1,@UBob,@AssApp,DATEADD(minute,-30,SYSUTCDATETIME()),DATEADD(minute,-25,SYSUTCDATETIME()));

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ACTION WHERE WorkflowTaskId=@TaskMgr1 AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TASK_ACTION (WorkflowTaskId, WorkflowActionTypeId, ActionByUserId, ActionAt, Notes, PayloadJson)
VALUES (@TaskMgr1,@ActApprove,@UBob,DATEADD(minute,-25,SYSUTCDATETIME()),'Approved for ops use',NULL);

-- Finance task (approved)
DECLARE @TaskFin1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SFin)
BEGIN
  INSERT dbo.WORKFLOW_TASK (WorkflowInstanceId, WorkflowStepId, WorkflowTaskStatusId, CreatedAt, DueAt, ClaimedByUserId, CompletedAt)
  VALUES (@WfInst1,@SFin,@TaskStatusApproved,DATEADD(minute,-24,SYSUTCDATETIME()),NULL,NULL,DATEADD(minute,-20,SYSUTCDATETIME()));
END
SET @TaskFin1 = (SELECT WorkflowTaskId FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SFin);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ASSIGNEE WHERE WorkflowTaskId=@TaskFin1 AND UserId=@UFatou)
INSERT dbo.WORKFLOW_TASK_ASSIGNEE (WorkflowTaskId, UserId, AssigneeStatusId, AssignedAt, DecidedAt)
VALUES (@TaskFin1,@UFatou,@AssApp,DATEADD(minute,-24,SYSUTCDATETIME()),DATEADD(minute,-20,SYSUTCDATETIME()));

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ACTION WHERE WorkflowTaskId=@TaskFin1 AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TASK_ACTION (WorkflowTaskId, WorkflowActionTypeId, ActionByUserId, ActionAt, Notes, PayloadJson)
VALUES (@TaskFin1,@ActApprove,@UFatou,DATEADD(minute,-20,SYSUTCDATETIME()),'Budget ok',NULL);

-- Inventory approval (approved)
DECLARE @TaskInv1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SInv)
BEGIN
  INSERT dbo.WORKFLOW_TASK (WorkflowInstanceId, WorkflowStepId, WorkflowTaskStatusId, CreatedAt, DueAt, ClaimedByUserId, CompletedAt)
  VALUES (@WfInst1,@SInv,@TaskStatusApproved,DATEADD(minute,-19,SYSUTCDATETIME()),NULL,NULL,DATEADD(minute,-15,SYSUTCDATETIME()));
END
SET @TaskInv1 = (SELECT WorkflowTaskId FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SInv);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ASSIGNEE WHERE WorkflowTaskId=@TaskInv1 AND UserId=@ULamin)
INSERT dbo.WORKFLOW_TASK_ASSIGNEE (WorkflowTaskId, UserId, AssigneeStatusId, AssignedAt, DecidedAt)
VALUES (@TaskInv1,@ULamin,@AssApp,DATEADD(minute,-19,SYSUTCDATETIME()),DATEADD(minute,-15,SYSUTCDATETIME()));

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ACTION WHERE WorkflowTaskId=@TaskInv1 AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TASK_ACTION (WorkflowTaskId, WorkflowActionTypeId, ActionByUserId, ActionAt, Notes, PayloadJson)
VALUES (@TaskInv1,@ActApprove,@ULamin,DATEADD(minute,-15,SYSUTCDATETIME()),'Stock available. Proceed to fulfillment.',NULL);

-- Fulfillment task (reserve then issue later) — reservation only here ✅
DECLARE @TaskFul1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SFul)
BEGIN
  INSERT dbo.WORKFLOW_TASK (WorkflowInstanceId, WorkflowStepId, WorkflowTaskStatusId, CreatedAt, DueAt, ClaimedByUserId, CompletedAt)
  VALUES (@WfInst1,@SFul,@TaskStatusCompleted,DATEADD(minute,-14,SYSUTCDATETIME()),NULL,@UMariama,DATEADD(minute,-1,SYSUTCDATETIME()));
END
SET @TaskFul1 = (SELECT WorkflowTaskId FROM dbo.WORKFLOW_TASK WHERE WorkflowInstanceId=@WfInst1 AND WorkflowStepId=@SFul);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ASSIGNEE WHERE WorkflowTaskId=@TaskFul1 AND UserId=@UMariama)
INSERT dbo.WORKFLOW_TASK_ASSIGNEE (WorkflowTaskId, UserId, AssigneeStatusId, AssignedAt, DecidedAt)
VALUES (@TaskFul1,@UMariama,@AssApp,DATEADD(minute,-14,SYSUTCDATETIME()),DATEADD(minute,-1,SYSUTCDATETIME()));

UPDATE dbo.INVENTORY_REQUEST
SET RequestStatusId=@ReqStatFulfillment
WHERE RequestId=@Req1Id;

-- Create reservation (fulfillment-only)
DECLARE @ResStatusActive bigint = (SELECT ReservationStatusId FROM dbo.RESERVATION_STATUS WHERE Code='ACTIVE');
DECLARE @Res1Id bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.RESERVATION WHERE ReservationNo='RSV-2026-0001')
BEGIN
  INSERT dbo.RESERVATION (ReservationNo, ReservationStatusId, WarehouseId, RequestId, ReservedByUserId, ReservedAt, ExpiresAt, Notes)
  VALUES ('RSV-2026-0001',@ResStatusActive,@WhMain,@Req1Id,@UMariama,SYSUTCDATETIME(),DATEADD(day,2,SYSUTCDATETIME()),'Reserved for Alice pickup');
END
SET @Res1Id = (SELECT ReservationId FROM dbo.RESERVATION WHERE ReservationNo='RSV-2026-0001');

IF NOT EXISTS (SELECT 1 FROM dbo.RESERVATION_LINE WHERE ReservationId=@Res1Id AND RequestLineId=@Req1LineA4)
INSERT dbo.RESERVATION_LINE (ReservationId, RequestLineId, ProductId, QtyReserved)
VALUES (@Res1Id,@Req1LineA4,@PA4,10);

IF NOT EXISTS (SELECT 1 FROM dbo.RESERVATION_LINE WHERE ReservationId=@Res1Id AND RequestLineId=@Req1LineMouse)
INSERT dbo.RESERVATION_LINE (ReservationId, RequestLineId, ProductId, QtyReserved)
VALUES (@Res1Id,@Req1LineMouse,@PMouse,2);

-- POST RESERVE movement
DECLARE @MvTypeReserve bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='RESERVE');
DECLARE @MvStatusPosted bigint = (SELECT MovementStatusId FROM dbo.INVENTORY_MOVEMENT_STATUS WHERE Code='POSTED');
DECLARE @ReasonReserve bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='RESERVE_FOR_PICKUP');

DECLARE @MvRes1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceReservationId=@Res1Id AND MovementTypeId=@MvTypeReserve)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, ReferenceReservationId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, ReversedMovementId, Notes)
  VALUES
  (@MvTypeReserve,@MvStatusPosted,@ReasonReserve,@WhMain,@Req1Id,@Res1Id,
   @UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),NULL,'Reserve for pickup');
END
SET @MvRes1 = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceReservationId=@Res1Id AND MovementTypeId=@MvTypeReserve);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvRes1 AND ProductId=@PA4)
INSERT dbo.STOCK_MOVEMENT_LINE (StockMovementId, ProductId, QtyDeltaOnHand, QtyDeltaReserved, UnitCost, LineNotes)
VALUES (@MvRes1,@PA4,0,10,NULL,'Reserve A4');

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvRes1 AND ProductId=@PMouse)
INSERT dbo.STOCK_MOVEMENT_LINE (StockMovementId, ProductId, QtyDeltaOnHand, QtyDeltaReserved, UnitCost, LineNotes)
VALUES (@MvRes1,@PMouse,0,2,NULL,'Reserve mouse');

-- Update snapshot reserved qty
UPDATE dbo.STOCK_LEVEL
SET ReservedQty = ReservedQty + 10, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PA4;

UPDATE dbo.STOCK_LEVEL
SET ReservedQty = ReservedQty + 2, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PMouse;

-- Mark request READY
UPDATE dbo.INVENTORY_REQUEST
SET RequestStatusId=@ReqStatReady
WHERE RequestId=@Req1Id;

-- Later: Issue and consume reservation
DECLARE @MvTypeIssue bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='ISSUE');
DECLARE @ReasonIssue bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='CUSTOMER_ISSUE');

DECLARE @MvIssue1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@Req1Id AND MovementTypeId=@MvTypeIssue)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, ReferenceReservationId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, ReversedMovementId, Notes)
  VALUES
  (@MvTypeIssue,@MvStatusPosted,@ReasonIssue,@WhMain,@Req1Id,@Res1Id,
   @UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),NULL,'Issue reserved items');
END
SET @MvIssue1 = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@Req1Id AND MovementTypeId=@MvTypeIssue);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvIssue1 AND ProductId=@PA4)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvIssue1,@PA4,-10,-10,NULL,'Issue A4 (consume reservation)');

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvIssue1 AND ProductId=@PMouse)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvIssue1,@PMouse,-2,-2,NULL,'Issue mouse (consume reservation)');

-- Update snapshot on_hand and reserved
UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty - 10, ReservedQty = ReservedQty - 10, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PA4;

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty - 2, ReservedQty = ReservedQty - 2, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PMouse;

-- Update request lines fulfilled
UPDATE dbo.INVENTORY_REQUEST_LINE
SET QtyFulfilled = QtyApproved
WHERE RequestId=@Req1Id AND QtyApproved IS NOT NULL;

-- Close reservation + request + workflow instance
UPDATE dbo.RESERVATION
SET ReservationStatusId = (SELECT ReservationStatusId FROM dbo.RESERVATION_STATUS WHERE Code='CONSUMED')
WHERE ReservationId=@Res1Id;

UPDATE dbo.INVENTORY_REQUEST
SET RequestStatusId=@ReqStatFulfilled
WHERE RequestId=@Req1Id;

UPDATE dbo.WORKFLOW_INSTANCE
SET WorkflowInstanceStatusId=@InstStatusCompleted, CurrentWorkflowStepId=@SEndOk, CompletedAt=SYSUTCDATETIME()
WHERE WorkflowInstanceId=@WfInst1;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TASK_ACTION WHERE WorkflowTaskId=@TaskFul1 AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TASK_ACTION
(WorkflowTaskId, WorkflowActionTypeId, ActionByUserId, ActionAt, Notes, PayloadJson)
VALUES (@TaskFul1,@ActComplete,@UMariama,SYSUTCDATETIME(),'Fulfillment complete; issued items',NULL);

---------------------------------------------------------------
-- 10) Second Issue Request: toner shortage -> waiting_for_stock -> receipt -> fulfillment
---------------------------------------------------------------
DECLARE @Req2Id bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST WHERE RequestNo='IR-2026-0002')
BEGIN
  INSERT dbo.INVENTORY_REQUEST (RequestNo, RequestTypeId, RequestStatusId, WarehouseId, RequestedByUserId, RequestedAt, Notes, WorkflowInstanceId)
  VALUES ('IR-2026-0002',@ReqTypeIssue,@ReqStatInWf,@WhMain,@UAlice,SYSUTCDATETIME(),'Need toner urgently',NULL);
END
SET @Req2Id = (SELECT RequestId FROM dbo.INVENTORY_REQUEST WHERE RequestNo='IR-2026-0002');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@Req2Id AND ProductId=@PToner)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@Req2Id,@PToner,5,5,0,'Toner for printers');

-- Simulate: at fulfillment stock not available -> waiting_for_stock + procurement receipt
UPDATE dbo.INVENTORY_REQUEST
SET RequestStatusId = (SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='WAITING_FOR_STOCK')
WHERE RequestId=@Req2Id;

-- Receive stock (receipt movement)
DECLARE @MvTypeReceipt bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='RECEIPT');
DECLARE @ReasonReceipt bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='PURCHASE_RECEIPT');

DECLARE @MvRec2 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE Notes='Receipt for IR-2026-0002 toner' AND WarehouseId=@WhMain AND MovementTypeId=@MvTypeReceipt)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, ReferenceReservationId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, ReversedMovementId, Notes)
  VALUES
  (@MvTypeReceipt,@MvStatusPosted,@ReasonReceipt,@WhMain,@Req2Id,NULL,
   @UEbrima,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),NULL,'Receipt for IR-2026-0002 toner');
END
SET @MvRec2 = (SELECT TOP 1 StockMovementId FROM dbo.STOCK_MOVEMENT WHERE Notes='Receipt for IR-2026-0002 toner' ORDER BY StockMovementId DESC);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvRec2 AND ProductId=@PToner)
INSERT dbo.STOCK_MOVEMENT_LINE (StockMovementId, ProductId, QtyDeltaOnHand, QtyDeltaReserved, UnitCost, LineNotes)
VALUES (@MvRec2,@PToner,10,0,NULL,'Restock toner (unit cost later)');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty + 10, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PToner;

-- Now fulfill immediately (no reservation since fulfill now)
DECLARE @MvIssue2 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@Req2Id AND MovementTypeId=@MvTypeIssue)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, ReferenceReservationId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, ReversedMovementId, Notes)
  VALUES
  (@MvTypeIssue,@MvStatusPosted,@ReasonIssue,@WhMain,@Req2Id,NULL,
   @UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),NULL,'Issue toner after restock');
END
SET @MvIssue2 = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@Req2Id AND MovementTypeId=@MvTypeIssue);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvIssue2 AND ProductId=@PToner)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvIssue2,@PToner,-5,0,NULL,'Issue toner (no reservation used)');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty - 5, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PToner;

UPDATE dbo.INVENTORY_REQUEST_LINE
SET QtyFulfilled = QtyApproved
WHERE RequestId=@Req2Id;

UPDATE dbo.INVENTORY_REQUEST
SET RequestStatusId=@ReqStatFulfilled
WHERE RequestId=@Req2Id;

---------------------------------------------------------------
-- 11) One Adjustment (damage write-off)
---------------------------------------------------------------
DECLARE @ReqTypeAdj bigint = (SELECT RequestTypeId FROM dbo.INVENTORY_REQUEST_TYPE WHERE Code='ADJUSTMENT');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST WHERE RequestNo='ADJ-2026-0001')
INSERT dbo.INVENTORY_REQUEST (RequestNo, RequestTypeId, RequestStatusId, WarehouseId, RequestedByUserId, RequestedAt, Notes, WorkflowInstanceId)
VALUES ('ADJ-2026-0001',@ReqTypeAdj,(SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='APPROVED'),@WhMain,@ULamin,SYSUTCDATETIME(),'Damaged bleach bottles',NULL);

DECLARE @AdjReqId bigint = (SELECT RequestId FROM dbo.INVENTORY_REQUEST WHERE RequestNo='ADJ-2026-0001');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@AdjReqId AND ProductId=@PBleach)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@AdjReqId,@PBleach,2,2,2,'Write-off 2 damaged bottles');

DECLARE @MvTypeAdjOut bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='ADJUSTMENT_OUT');
DECLARE @ReasonDamage bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='DAMAGE');

DECLARE @MvAdj1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@AdjReqId AND MovementTypeId=@MvTypeAdjOut)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, Notes)
  VALUES
  (@MvTypeAdjOut,@MvStatusPosted,@ReasonDamage,@WhMain,@AdjReqId,
   @ULamin,SYSUTCDATETIME(),@ULamin,SYSUTCDATETIME(),'Damage write-off');
END
SET @MvAdj1 = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@AdjReqId AND MovementTypeId=@MvTypeAdjOut);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvAdj1 AND ProductId=@PBleach)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvAdj1,@PBleach,-2,0,NULL,'Damaged bleach write-off');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty - 2, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PBleach;

---------------------------------------------------------------
-- 12) One Transfer (Main -> Branch) for A4
---------------------------------------------------------------
DECLARE @ReqTypeTr bigint = (SELECT RequestTypeId FROM dbo.INVENTORY_REQUEST_TYPE WHERE Code='TRANSFER');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST WHERE RequestNo='TR-2026-0001')
INSERT dbo.INVENTORY_REQUEST (RequestNo, RequestTypeId, RequestStatusId, WarehouseId, RequestedByUserId, RequestedAt, Notes, WorkflowInstanceId)
VALUES ('TR-2026-0001',@ReqTypeTr,(SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='APPROVED'),@WhMain,@ULamin,SYSUTCDATETIME(),'Transfer A4 to Branch Store',NULL);

DECLARE @TrReqId bigint = (SELECT RequestId FROM dbo.INVENTORY_REQUEST WHERE RequestNo='TR-2026-0001');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@TrReqId AND ProductId=@PA4)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@TrReqId,@PA4,20,20,20,'Move stock to branch');

DECLARE @MvTypeTrOut bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='TRANSFER_OUT');
DECLARE @MvTypeTrIn bigint  = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='TRANSFER_IN');
DECLARE @ReasonTransfer bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='WAREHOUSE_TRANSFER');

-- Transfer out
DECLARE @MvTrOut bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@TrReqId AND MovementTypeId=@MvTypeTrOut)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, Notes)
  VALUES
  (@MvTypeTrOut,@MvStatusPosted,@ReasonTransfer,@WhMain,@TrReqId,@UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),'Transfer out to Branch Store');
END
SET @MvTrOut = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@TrReqId AND MovementTypeId=@MvTypeTrOut);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvTrOut AND ProductId=@PA4)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvTrOut,@PA4,-20,0,NULL,'Transfer out A4');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty - 20, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PA4;

-- Transfer in
DECLARE @MvTrIn bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE Notes='Transfer in from Main Store' AND WarehouseId=@WhBranch AND ReferenceRequestId=@TrReqId AND MovementTypeId=@MvTypeTrIn)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId, CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, Notes)
  VALUES
  (@MvTypeTrIn,@MvStatusPosted,@ReasonTransfer,@WhBranch,@TrReqId,@UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),'Transfer in from Main Store');
END
SET @MvTrIn = (SELECT TOP 1 StockMovementId FROM dbo.STOCK_MOVEMENT WHERE WarehouseId=@WhBranch AND ReferenceRequestId=@TrReqId AND MovementTypeId=@MvTypeTrIn ORDER BY StockMovementId DESC);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvTrIn AND ProductId=@PA4)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvTrIn,@PA4,20,0,NULL,'Transfer in A4');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty + 20, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhBranch AND ProductId=@PA4;

---------------------------------------------------------------
-- 13) One Return (Return IN - mouse returned)
---------------------------------------------------------------
DECLARE @ReqTypeRet bigint = (SELECT RequestTypeId FROM dbo.INVENTORY_REQUEST_TYPE WHERE Code='RETURN');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST WHERE RequestNo='RT-2026-0001')
INSERT dbo.INVENTORY_REQUEST (RequestNo, RequestTypeId, RequestStatusId, WarehouseId, RequestedByUserId, RequestedAt, Notes, WorkflowInstanceId)
VALUES ('RT-2026-0001',@ReqTypeRet,(SELECT RequestStatusId FROM dbo.INVENTORY_REQUEST_STATUS WHERE Code='APPROVED'),@WhMain,@UMariama,SYSUTCDATETIME(),'Mouse returned in good condition',NULL);

DECLARE @RetReqId bigint = (SELECT RequestId FROM dbo.INVENTORY_REQUEST WHERE RequestNo='RT-2026-0001');

IF NOT EXISTS (SELECT 1 FROM dbo.INVENTORY_REQUEST_LINE WHERE RequestId=@RetReqId AND ProductId=@PMouse)
INSERT dbo.INVENTORY_REQUEST_LINE (RequestId, ProductId, QtyRequested, QtyApproved, QtyFulfilled, LineNotes)
VALUES (@RetReqId,@PMouse,1,1,1,'Return 1 mouse');

DECLARE @MvTypeReturnIn bigint = (SELECT MovementTypeId FROM dbo.INVENTORY_MOVEMENT_TYPE WHERE Code='RETURN_IN');
DECLARE @ReasonReturn bigint = (SELECT ReasonCodeId FROM dbo.INVENTORY_REASON_CODE WHERE Code='RETURN_FROM_USER');

DECLARE @MvRet1 bigint;
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@RetReqId AND MovementTypeId=@MvTypeReturnIn)
BEGIN
  INSERT dbo.STOCK_MOVEMENT
  (MovementTypeId, MovementStatusId, ReasonCodeId, WarehouseId, ReferenceRequestId,
   CreatedByUserId, CreatedAt, PostedByUserId, PostedAt, Notes)
  VALUES
  (@MvTypeReturnIn,@MvStatusPosted,@ReasonReturn,@WhMain,@RetReqId,
   @UMariama,SYSUTCDATETIME(),@UMariama,SYSUTCDATETIME(),'Return in');
END
SET @MvRet1 = (SELECT StockMovementId FROM dbo.STOCK_MOVEMENT WHERE ReferenceRequestId=@RetReqId AND MovementTypeId=@MvTypeReturnIn);

IF NOT EXISTS (SELECT 1 FROM dbo.STOCK_MOVEMENT_LINE WHERE StockMovementId=@MvRet1 AND ProductId=@PMouse)
INSERT dbo.STOCK_MOVEMENT_LINE VALUES
(@MvRet1,@PMouse,1,0,NULL,'Return mouse into store');

UPDATE dbo.STOCK_LEVEL
SET OnHandQty = OnHandQty + 1, UpdatedAt=SYSUTCDATETIME()
WHERE WarehouseId=@WhMain AND ProductId=@PMouse;

COMMIT TRAN;
