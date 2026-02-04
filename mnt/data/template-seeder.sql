/* ============================================================
   WORKFLOW TEMPLATE SEEDER (6 templates fully defined)
   SQL Server - Idempotent
   Requires: lookup tables + roles already seeded (static seeder)
   ============================================================ */

SET NOCOUNT ON;
BEGIN TRAN;

---------------------------------------------------------------
-- Lookup IDs (step types, modes, actions)
---------------------------------------------------------------
DECLARE
  @StepTypeStart   bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='START'),
  @StepTypeReview  bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='REVIEW'),
  @StepTypeApproval bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='APPROVAL'),
  @StepTypeFulfill bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='FULFILLMENT'),
  @StepTypeEnd     bigint = (SELECT WorkflowStepTypeId FROM dbo.WORKFLOW_STEP_TYPE WHERE Code='END'),

  @ModeAutoRole    bigint = (SELECT AssignmentModeId FROM dbo.WORKFLOW_ASSIGNMENT_MODE WHERE Code='AUTO_ROLE'),

  @ActSubmit       bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='SUBMIT'),
  @ActApprove      bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='APPROVE'),
  @ActReject       bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='REJECT'),
  @ActSendBack     bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='SEND_BACK'),
  @ActCancel       bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='CANCEL'),
  @ActComplete     bigint = (SELECT WorkflowActionTypeId FROM dbo.WORKFLOW_ACTION_TYPE WHERE Code='COMPLETE');

---------------------------------------------------------------
-- Role IDs
---------------------------------------------------------------
DECLARE
  @RAdmin      bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Admin'),
  @RRequester  bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Requester'),
  @RSuper      bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Supervisor'),
  @RFinance    bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='FinanceOfficer'),
  @RInvMgr     bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='InventoryManager'),
  @RStore      bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Storekeeper'),
  @RProc       bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='ProcurementOfficer'),
  @RAuditor    bigint = (SELECT RoleId FROM dbo.ROLE WHERE Name='Auditor');

---------------------------------------------------------------
-- CreatedBy/PublishedBy (use first Admin user if exists)
---------------------------------------------------------------
DECLARE @SeedUserId bigint = (SELECT TOP 1 U.UserId FROM dbo.[USER] U
                             JOIN dbo.USER_ROLE UR ON UR.UserId=U.UserId
                             WHERE UR.RoleId=@RAdmin
                             ORDER BY U.UserId);

IF @SeedUserId IS NULL
  SET @SeedUserId = (SELECT TOP 1 UserId FROM dbo.[USER] ORDER BY UserId);

IF @SeedUserId IS NULL
BEGIN
  -- If no users exist yet, we still seed templates with NULL for created/published
  -- (Assumes CreatedByUserId/PublishedByUserId allow NULL; if not, create admin user first.)
  SET @SeedUserId = NULL;
END

/* ============================================================
   Helper pattern:
   - Create definition
   - Create version v1
   - Insert steps
   - Insert rules
   - Insert transitions
   ============================================================ */

---------------------------------------------------------------
-- WORKFLOW 1: Inventory Issue (WF_INV_ISSUE)
---------------------------------------------------------------
DECLARE @DefIssue bigint, @VerIssue bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_INV_ISSUE')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_INV_ISSUE','Inventory Issue Request Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefIssue = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_INV_ISSUE');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefIssue AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefIssue,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerIssue = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefIssue AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerIssue,'START','Start',@StepTypeStart,1,1),
(@VerIssue,'MGR_APPROVAL','Manager Approval',@StepTypeApproval,2,1),
(@VerIssue,'FIN_APPROVAL','Finance Approval',@StepTypeApproval,3,1),
(@VerIssue,'INV_APPROVAL','Inventory Approval',@StepTypeApproval,4,1),
(@VerIssue,'FULFILL','Fulfillment (Storekeeper)',@StepTypeFulfill,5,1),
(@VerIssue,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerIssue,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerIssue,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @IS_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='START'),
  @IS_MGR   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='MGR_APPROVAL'),
  @IS_FIN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='FIN_APPROVAL'),
  @IS_INV   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='INV_APPROVAL'),
  @IS_FUL   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='FULFILL'),
  @IS_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='END_OK'),
  @IS_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='END_REJECT'),
  @IS_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerIssue AND StepKey='END_CANCEL');

-- Rules
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@IS_START)
INSERT dbo.WORKFLOW_STEP_RULE
(WorkflowStepId, AssignmentModeId, RoleId, DepartmentId, UseRequesterDepartment, AllowRequesterSelect,
 MinApprovers, RequireAll, AllowReassign, AllowDelegate, SLA_Minutes)
VALUES (@IS_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@IS_MGR)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@IS_MGR,@ModeAutoRole,@RSuper,NULL,1,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@IS_FIN)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@IS_FIN,@ModeAutoRole,@RFinance,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@IS_INV)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@IS_INV,@ModeAutoRole,@RInvMgr,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@IS_FUL)
INSERT dbo.WORKFLOW_STEP_RULE VALUES
(@IS_FUL,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_START,@ActSubmit,@IS_MGR);

-- Approvals chain
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_MGR AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_MGR,@ActApprove,@IS_FIN);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FIN AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FIN,@ActApprove,@IS_INV);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_INV AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_INV,@ActApprove,@IS_FUL);

-- Reject / send back
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_MGR AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_MGR,@ActReject,@IS_REJ);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FIN AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FIN,@ActReject,@IS_REJ);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_INV AND WorkflowActionTypeId=@ActReject)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_INV,@ActReject,@IS_REJ);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_MGR AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_MGR,@ActSendBack,@IS_START);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FIN AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FIN,@ActSendBack,@IS_START);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_INV AND WorkflowActionTypeId=@ActSendBack)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_INV,@ActSendBack,@IS_START);

-- Fulfillment complete
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FUL AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FUL,@ActComplete,@IS_OK);

-- Cancel from key steps
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_MGR AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_MGR,@ActCancel,@IS_CAN);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FIN AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FIN,@ActCancel,@IS_CAN);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_INV AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_INV,@ActCancel,@IS_CAN);
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerIssue AND FromWorkflowStepId=@IS_FUL AND WorkflowActionTypeId=@ActCancel)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerIssue,@IS_FUL,@ActCancel,@IS_CAN);

---------------------------------------------------------------
-- WORKFLOW 2: Procurement (WF_PROCURE)
-- PR -> Finance -> Procurement Approval -> Supplier Selection -> PO -> GRN -> End
---------------------------------------------------------------
DECLARE @DefProc bigint, @VerProc bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_PROCURE')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_PROCURE','Procurement / Purchase Requisition Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefProc = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_PROCURE');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefProc AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefProc,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerProc = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefProc AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerProc,'START','Start',@StepTypeStart,1,1),
(@VerProc,'FIN_CHECK','Finance Check / Budget',@StepTypeApproval,2,1),
(@VerProc,'PROC_APPROVAL','Procurement Approval',@StepTypeApproval,3,1),
(@VerProc,'SUPPLIER_SELECT','Supplier Selection',@StepTypeReview,4,1),
(@VerProc,'PO_CREATE','Create Purchase Order',@StepTypeReview,5,1),
(@VerProc,'GRN_RECEIPT','Goods Receipt (GRN)',@StepTypeFulfill,6,1),
(@VerProc,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerProc,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerProc,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @PR_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='START'),
  @PR_FIN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='FIN_CHECK'),
  @PR_APP   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='PROC_APPROVAL'),
  @PR_SUP   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='SUPPLIER_SELECT'),
  @PR_PO    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='PO_CREATE'),
  @PR_GRN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='GRN_RECEIPT'),
  @PR_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='END_OK'),
  @PR_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='END_REJECT'),
  @PR_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerProc AND StepKey='END_CANCEL');

-- Rules
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_START)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_FIN)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_FIN,@ModeAutoRole,@RFinance,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_APP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_APP,@ModeAutoRole,@RProc,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_SUP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_SUP,@ModeAutoRole,@RProc,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_PO)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_PO,@ModeAutoRole,@RProc,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@PR_GRN)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@PR_GRN,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_START,@ActSubmit,@PR_FIN);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_FIN AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_FIN,@ActApprove,@PR_APP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_APP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_APP,@ActApprove,@PR_SUP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_SUP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_SUP,@ActApprove,@PR_PO);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_PO AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_PO,@ActApprove,@PR_GRN);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerProc AND FromWorkflowStepId=@PR_GRN AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerProc,@PR_GRN,@ActComplete,@PR_OK);

-- Reject / send back
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerProc, X.FromStep, X.ActionId, X.ToStep
FROM (VALUES
(@PR_FIN,@ActReject,@PR_REJ),
(@PR_APP,@ActReject,@PR_REJ),
(@PR_SUP,@ActReject,@PR_REJ),
(@PR_PO,@ActReject,@PR_REJ),
(@PR_FIN,@ActSendBack,@PR_START),
(@PR_APP,@ActSendBack,@PR_START),
(@PR_SUP,@ActSendBack,@PR_START),
(@PR_PO,@ActSendBack,@PR_START)
) X(FromStep,ActionId,ToStep)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerProc AND WT.FromWorkflowStepId=X.FromStep AND WT.WorkflowActionTypeId=X.ActionId
);

-- Cancel from key steps
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerProc, S.StepId, @ActCancel, @PR_CAN
FROM (VALUES (@PR_FIN),(@PR_APP),(@PR_SUP),(@PR_PO),(@PR_GRN)) S(StepId)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerProc AND WT.FromWorkflowStepId=S.StepId AND WT.WorkflowActionTypeId=@ActCancel
);

---------------------------------------------------------------
-- WORKFLOW 3: Adjustment (WF_ADJUST)
-- Start -> Supervisor Approval -> Inventory Manager Approval -> Post Adjustment -> End
---------------------------------------------------------------
DECLARE @DefAdj bigint, @VerAdj bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_ADJUST')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_ADJUST','Stock Adjustment Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefAdj = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_ADJUST');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefAdj AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefAdj,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerAdj = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefAdj AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerAdj,'START','Start',@StepTypeStart,1,1),
(@VerAdj,'SUP_APPROVAL','Supervisor Approval',@StepTypeApproval,2,1),
(@VerAdj,'INV_APPROVAL','Inventory Manager Approval',@StepTypeApproval,3,1),
(@VerAdj,'POST_ADJ','Post Adjustment (Ledger)',@StepTypeFulfill,4,1),
(@VerAdj,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerAdj,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerAdj,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @AD_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='START'),
  @AD_SUP   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='SUP_APPROVAL'),
  @AD_INV   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='INV_APPROVAL'),
  @AD_POST  bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='POST_ADJ'),
  @AD_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='END_OK'),
  @AD_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='END_REJECT'),
  @AD_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerAdj AND StepKey='END_CANCEL');

-- Rules
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@AD_START)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@AD_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@AD_SUP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@AD_SUP,@ModeAutoRole,@RSuper,NULL,1,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@AD_INV)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@AD_INV,@ModeAutoRole,@RInvMgr,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@AD_POST)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@AD_POST,@ModeAutoRole,@RInvMgr,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerAdj AND FromWorkflowStepId=@AD_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerAdj,@AD_START,@ActSubmit,@AD_SUP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerAdj AND FromWorkflowStepId=@AD_SUP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerAdj,@AD_SUP,@ActApprove,@AD_INV);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerAdj AND FromWorkflowStepId=@AD_INV AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerAdj,@AD_INV,@ActApprove,@AD_POST);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerAdj AND FromWorkflowStepId=@AD_POST AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerAdj,@AD_POST,@ActComplete,@AD_OK);

-- Reject / send back
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerAdj, X.FromStep, X.ActionId, X.ToStep
FROM (VALUES
(@AD_SUP,@ActReject,@AD_REJ),
(@AD_INV,@ActReject,@AD_REJ),
(@AD_SUP,@ActSendBack,@AD_START),
(@AD_INV,@ActSendBack,@AD_START)
) X(FromStep,ActionId,ToStep)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerAdj AND WT.FromWorkflowStepId=X.FromStep AND WT.WorkflowActionTypeId=X.ActionId
);

-- Cancel
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerAdj, S.StepId, @ActCancel, @AD_CAN
FROM (VALUES (@AD_SUP),(@AD_INV),(@AD_POST)) S(StepId)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerAdj AND WT.FromWorkflowStepId=S.StepId AND WT.WorkflowActionTypeId=@ActCancel
);

---------------------------------------------------------------
-- WORKFLOW 4: Transfer (WF_TRANSFER)
-- Start -> Source Approval -> Dispatch -> Receive -> End
---------------------------------------------------------------
DECLARE @DefTr bigint, @VerTr bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_TRANSFER')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_TRANSFER','Warehouse Transfer Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefTr = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_TRANSFER');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefTr AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefTr,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerTr = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefTr AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerTr,'START','Start',@StepTypeStart,1,1),
(@VerTr,'SOURCE_APPROVAL','Source Warehouse Approval',@StepTypeApproval,2,1),
(@VerTr,'DISPATCH','Dispatch / Transfer Out',@StepTypeFulfill,3,1),
(@VerTr,'RECEIVE','Receive / Transfer In',@StepTypeFulfill,4,1),
(@VerTr,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerTr,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerTr,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @TR_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='START'),
  @TR_APP   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='SOURCE_APPROVAL'),
  @TR_OUT   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='DISPATCH'),
  @TR_IN    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='RECEIVE'),
  @TR_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='END_OK'),
  @TR_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='END_REJECT'),
  @TR_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerTr AND StepKey='END_CANCEL');

-- Rules
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@TR_START)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@TR_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@TR_APP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@TR_APP,@ModeAutoRole,@RInvMgr,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@TR_OUT)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@TR_OUT,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@TR_IN)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@TR_IN,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerTr AND FromWorkflowStepId=@TR_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerTr,@TR_START,@ActSubmit,@TR_APP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerTr AND FromWorkflowStepId=@TR_APP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerTr,@TR_APP,@ActApprove,@TR_OUT);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerTr AND FromWorkflowStepId=@TR_OUT AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerTr,@TR_OUT,@ActApprove,@TR_IN);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerTr AND FromWorkflowStepId=@TR_IN AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerTr,@TR_IN,@ActComplete,@TR_OK);

-- Reject / send back
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerTr, X.FromStep, X.ActionId, X.ToStep
FROM (VALUES
(@TR_APP,@ActReject,@TR_REJ),
(@TR_APP,@ActSendBack,@TR_START)
) X(FromStep,ActionId,ToStep)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerTr AND WT.FromWorkflowStepId=X.FromStep AND WT.WorkflowActionTypeId=X.ActionId
);

-- Cancel
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerTr, S.StepId, @ActCancel, @TR_CAN
FROM (VALUES (@TR_APP),(@TR_OUT),(@TR_IN)) S(StepId)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerTr AND WT.FromWorkflowStepId=S.StepId AND WT.WorkflowActionTypeId=@ActCancel
);

---------------------------------------------------------------
-- WORKFLOW 5: Return (WF_RETURN)
-- Start -> Storekeeper Inspection -> Inventory Manager Approval -> Post Return -> End
-- (Works for Return In/Out; business rules handled by request data + permissions)
---------------------------------------------------------------
DECLARE @DefRt bigint, @VerRt bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_RETURN')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_RETURN','Returns Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefRt = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_RETURN');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefRt AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefRt,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerRt = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefRt AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerRt,'START','Start',@StepTypeStart,1,1),
(@VerRt,'INSPECTION','Storekeeper Inspection',@StepTypeFulfill,2,1),
(@VerRt,'INV_APPROVAL','Inventory Manager Approval',@StepTypeApproval,3,1),
(@VerRt,'POST_RETURN','Post Return (Ledger)',@StepTypeFulfill,4,1),
(@VerRt,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerRt,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerRt,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @RT_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='START'),
  @RT_INSP  bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='INSPECTION'),
  @RT_INV   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='INV_APPROVAL'),
  @RT_POST  bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='POST_RETURN'),
  @RT_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='END_OK'),
  @RT_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='END_REJECT'),
  @RT_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerRt AND StepKey='END_CANCEL');

-- Rules
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@RT_START)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@RT_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@RT_INSP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@RT_INSP,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@RT_INV)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@RT_INV,@ModeAutoRole,@RInvMgr,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@RT_POST)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@RT_POST,@ModeAutoRole,@RStore,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerRt AND FromWorkflowStepId=@RT_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerRt,@RT_START,@ActSubmit,@RT_INSP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerRt AND FromWorkflowStepId=@RT_INSP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerRt,@RT_INSP,@ActApprove,@RT_INV);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerRt AND FromWorkflowStepId=@RT_INV AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerRt,@RT_INV,@ActApprove,@RT_POST);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerRt AND FromWorkflowStepId=@RT_POST AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerRt,@RT_POST,@ActComplete,@RT_OK);

-- Reject / send back
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerRt, X.FromStep, X.ActionId, X.ToStep
FROM (VALUES
(@RT_INSP,@ActReject,@RT_REJ),
(@RT_INV,@ActReject,@RT_REJ),
(@RT_INSP,@ActSendBack,@RT_START),
(@RT_INV,@ActSendBack,@RT_START)
) X(FromStep,ActionId,ToStep)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerRt AND WT.FromWorkflowStepId=X.FromStep AND WT.WorkflowActionTypeId=X.ActionId
);

-- Cancel
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerRt, S.StepId, @ActCancel, @RT_CAN
FROM (VALUES (@RT_INSP),(@RT_INV),(@RT_POST)) S(StepId)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerRt AND WT.FromWorkflowStepId=S.StepId AND WT.WorkflowActionTypeId=@ActCancel
);

---------------------------------------------------------------
-- WORKFLOW 6: Catalog Governance (WF_CATALOG)
-- Start -> Admin Review -> Admin Approval -> Publish -> End
---------------------------------------------------------------
DECLARE @DefCat bigint, @VerCat bigint;

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_CATALOG')
INSERT dbo.WORKFLOW_DEFINITION (Code, Name, IsActive, CreatedByUserId, CreatedAt)
VALUES ('WF_CATALOG','Catalog Governance Workflow',1,@SeedUserId,SYSUTCDATETIME());

SET @DefCat = (SELECT WorkflowDefinitionId FROM dbo.WORKFLOW_DEFINITION WHERE Code='WF_CATALOG');

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefCat AND VersionNo=1)
INSERT dbo.WORKFLOW_DEFINITION_VERSION (WorkflowDefinitionId, VersionNo, IsActive, PublishedAt, PublishedByUserId)
VALUES (@DefCat,1,1,SYSUTCDATETIME(),@SeedUserId);

SET @VerCat = (SELECT WorkflowDefinitionVersionId FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE WorkflowDefinitionId=@DefCat AND VersionNo=1);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='START')
INSERT dbo.WORKFLOW_STEP (WorkflowDefinitionVersionId, StepKey, Name, WorkflowStepTypeId, SequenceNo, IsActive) VALUES
(@VerCat,'START','Start',@StepTypeStart,1,1),
(@VerCat,'CAT_REVIEW','Catalog Review',@StepTypeReview,2,1),
(@VerCat,'CAT_APPROVAL','Catalog Approval',@StepTypeApproval,3,1),
(@VerCat,'PUBLISH','Publish Changes',@StepTypeFulfill,4,1),
(@VerCat,'END_OK','End - Completed',@StepTypeEnd,90,1),
(@VerCat,'END_REJECT','End - Rejected',@StepTypeEnd,91,1),
(@VerCat,'END_CANCEL','End - Cancelled',@StepTypeEnd,92,1);

DECLARE
  @CG_START bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='START'),
  @CG_REV   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='CAT_REVIEW'),
  @CG_APP   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='CAT_APPROVAL'),
  @CG_PUB   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='PUBLISH'),
  @CG_OK    bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='END_OK'),
  @CG_REJ   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='END_REJECT'),
  @CG_CAN   bigint = (SELECT WorkflowStepId FROM dbo.WORKFLOW_STEP WHERE WorkflowDefinitionVersionId=@VerCat AND StepKey='END_CANCEL');

-- Rules (Admin-owned governance)
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@CG_START)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@CG_START,@ModeAutoRole,NULL,NULL,0,0,1,0,0,0,NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@CG_REV)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@CG_REV,@ModeAutoRole,@RAdmin,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@CG_APP)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@CG_APP,@ModeAutoRole,@RAdmin,NULL,0,0,1,0,0,0,1440);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_STEP_RULE WHERE WorkflowStepId=@CG_PUB)
INSERT dbo.WORKFLOW_STEP_RULE VALUES (@CG_PUB,@ModeAutoRole,@RAdmin,NULL,0,0,1,0,0,0,1440);

-- Transitions
IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerCat AND FromWorkflowStepId=@CG_START AND WorkflowActionTypeId=@ActSubmit)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerCat,@CG_START,@ActSubmit,@CG_REV);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerCat AND FromWorkflowStepId=@CG_REV AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerCat,@CG_REV,@ActApprove,@CG_APP);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerCat AND FromWorkflowStepId=@CG_APP AND WorkflowActionTypeId=@ActApprove)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerCat,@CG_APP,@ActApprove,@CG_PUB);

IF NOT EXISTS (SELECT 1 FROM dbo.WORKFLOW_TRANSITION WHERE WorkflowDefinitionVersionId=@VerCat AND FromWorkflowStepId=@CG_PUB AND WorkflowActionTypeId=@ActComplete)
INSERT dbo.WORKFLOW_TRANSITION VALUES (@VerCat,@CG_PUB,@ActComplete,@CG_OK);

-- Reject / send back
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerCat, X.FromStep, X.ActionId, X.ToStep
FROM (VALUES
(@CG_REV,@ActReject,@CG_REJ),
(@CG_APP,@ActReject,@CG_REJ),
(@CG_REV,@ActSendBack,@CG_START),
(@CG_APP,@ActSendBack,@CG_START)
) X(FromStep,ActionId,ToStep)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerCat AND WT.FromWorkflowStepId=X.FromStep AND WT.WorkflowActionTypeId=X.ActionId
);

-- Cancel
INSERT dbo.WORKFLOW_TRANSITION (WorkflowDefinitionVersionId, FromWorkflowStepId, WorkflowActionTypeId, ToWorkflowStepId)
SELECT @VerCat, S.StepId, @ActCancel, @CG_CAN
FROM (VALUES (@CG_REV),(@CG_APP),(@CG_PUB)) S(StepId)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.WORKFLOW_TRANSITION WT
  WHERE WT.WorkflowDefinitionVersionId=@VerCat AND WT.FromWorkflowStepId=S.StepId AND WT.WorkflowActionTypeId=@ActCancel
);

COMMIT TRAN;

/* ============================================================
   Done ✅
   Templates now exist for:
   - WF_INV_ISSUE
   - WF_PROCURE
   - WF_ADJUST
   - WF_TRANSFER
   - WF_RETURN
   - WF_CATALOG
   ============================================================ */
