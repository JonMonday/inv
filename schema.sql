/* ============================================================
   MASTER SCHEMA - INVENTORY + WORKFLOW + RESERVATIONS
   Target: SQL Server
   Based on: static-seeder, template-seeder, realworld-seeder
   ============================================================ */

-- Ensure we are using the correct DB (Optional, usually handled by connection string)
-- USE [InventoryDB];
-- GO

SET NOCOUNT ON;
GO

/* ============================================================
   1. SYSTEM & UTILITY TABLES
   ============================================================ */

-- IDEMPOTENCY (Must match strict requirements)
IF OBJECT_ID('dbo.IDEMPOTENCY_KEY', 'U') IS NULL
CREATE TABLE dbo.IDEMPOTENCY_KEY (
    IdempotencyKeyId bigint IDENTITY(1,1) NOT NULL,
    [Key] nvarchar(200) NOT NULL,
    UserId bigint NOT NULL,
    Route nvarchar(200) NOT NULL,
    RequestHash nvarchar(max) NULL,
    CreatedAtUtc datetime2(7) NOT NULL,
    ExpiresAtUtc datetime2(7) NOT NULL,
    ResponseJson nvarchar(max) NULL,
    ResponseStatusCode int NULL,
    CONSTRAINT PK_IDEMPOTENCY_KEY PRIMARY KEY CLUSTERED (IdempotencyKeyId),
    CONSTRAINT UQ_IDEMPOTENCY_KEY UNIQUE ([Key], UserId, Route)
);
GO

-- AUDIT LOGGING (Append-only)
IF OBJECT_ID('dbo.SECURITY_EVENT_TYPE', 'U') IS NULL
CREATE TABLE dbo.SECURITY_EVENT_TYPE (
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_SECURITY_EVENT_TYPE PRIMARY KEY (Code)
);
GO

IF OBJECT_ID('dbo.AUDIT_LOG', 'U') IS NULL
CREATE TABLE dbo.AUDIT_LOG (
    AuditLogId bigint IDENTITY(1,1) NOT NULL,
    CorrelationId nvarchar(50) NULL,
    UserId bigint NULL,
    EventType varchar(50) NOT NULL,
    EntityTable varchar(50) NULL,
    EntityId varchar(50) NULL,
    Action nvarchar(50) NULL,
    PayloadJson nvarchar(max) NULL,
    CreatedAtUtc datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AUDIT_LOG PRIMARY KEY CLUSTERED (AuditLogId)
);
GO

/* ============================================================
   2. LOOKUP TABLES (STATIC)
   ============================================================ */

IF OBJECT_ID('dbo.ACCESS_SCOPE_TYPE', 'U') IS NULL
CREATE TABLE dbo.ACCESS_SCOPE_TYPE (
    AccessScopeTypeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_ACCESS_SCOPE_TYPE PRIMARY KEY (AccessScopeTypeId),
    CONSTRAINT UQ_ACCESS_SCOPE_TYPE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.INVENTORY_REQUEST_TYPE', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_REQUEST_TYPE (
    RequestTypeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_INVENTORY_REQUEST_TYPE PRIMARY KEY (RequestTypeId),
    CONSTRAINT UQ_INVENTORY_REQUEST_TYPE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.INVENTORY_REQUEST_STATUS', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_REQUEST_STATUS (
    RequestStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsTerminal bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_INVENTORY_REQUEST_STATUS PRIMARY KEY (RequestStatusId),
    CONSTRAINT UQ_INVENTORY_REQUEST_STATUS_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.RESERVATION_STATUS', 'U') IS NULL
CREATE TABLE dbo.RESERVATION_STATUS (
    ReservationStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsTerminal bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_RESERVATION_STATUS PRIMARY KEY (ReservationStatusId),
    CONSTRAINT UQ_RESERVATION_STATUS_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.INVENTORY_MOVEMENT_STATUS', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_MOVEMENT_STATUS (
    MovementStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsTerminal bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_INVENTORY_MOVEMENT_STATUS PRIMARY KEY (MovementStatusId),
    CONSTRAINT UQ_INVENTORY_MOVEMENT_STATUS_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.INVENTORY_MOVEMENT_TYPE', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_MOVEMENT_TYPE (
    MovementTypeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_INVENTORY_MOVEMENT_TYPE PRIMARY KEY (MovementTypeId),
    CONSTRAINT UQ_INVENTORY_MOVEMENT_TYPE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.INVENTORY_REASON_CODE', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_REASON_CODE (
    ReasonCodeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    RequiresApproval bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_INVENTORY_REASON_CODE PRIMARY KEY (ReasonCodeId),
    CONSTRAINT UQ_INVENTORY_REASON_CODE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_STEP_TYPE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_STEP_TYPE (
    WorkflowStepTypeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_WORKFLOW_STEP_TYPE PRIMARY KEY (WorkflowStepTypeId),
    CONSTRAINT UQ_WORKFLOW_STEP_TYPE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_ASSIGNMENT_MODE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_ASSIGNMENT_MODE (
    AssignmentModeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_WORKFLOW_ASSIGNMENT_MODE PRIMARY KEY (AssignmentModeId),
    CONSTRAINT UQ_WORKFLOW_ASSIGNMENT_MODE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_ACTION_TYPE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_ACTION_TYPE (
    WorkflowActionTypeId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsDecision bit NOT NULL DEFAULT 0,
    IsSystemAction bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_WORKFLOW_ACTION_TYPE PRIMARY KEY (WorkflowActionTypeId),
    CONSTRAINT UQ_WORKFLOW_ACTION_TYPE_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_CONDITION_OPERATOR', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_CONDITION_OPERATOR (
    WorkflowConditionOperatorId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_WORKFLOW_CONDITION_OPERATOR PRIMARY KEY (WorkflowConditionOperatorId),
    CONSTRAINT UQ_WORKFLOW_CONDITION_OPERATOR_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_INSTANCE_STATUS', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_INSTANCE_STATUS (
    WorkflowInstanceStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsTerminal bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_WORKFLOW_INSTANCE_STATUS PRIMARY KEY (WorkflowInstanceStatusId),
    CONSTRAINT UQ_WORKFLOW_INSTANCE_STATUS_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_TASK_STATUS', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TASK_STATUS (
    WorkflowTaskStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsTerminal bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_WORKFLOW_TASK_STATUS PRIMARY KEY (WorkflowTaskStatusId),
    CONSTRAINT UQ_WORKFLOW_TASK_STATUS_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_TASK_ASSIGNEE_STATUS', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TASK_ASSIGNEE_STATUS (
    AssigneeStatusId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    CONSTRAINT PK_WORKFLOW_TASK_ASSIGNEE_STATUS PRIMARY KEY (AssigneeStatusId),
    CONSTRAINT UQ_WORKFLOW_TASK_ASSIGNEE_STATUS_Code UNIQUE (Code)
);
GO

/* ============================================================
   3. ENTITY & RBAC TABLES
   ============================================================ */

IF OBJECT_ID('dbo.DEPARTMENT', 'U') IS NULL
CREATE TABLE dbo.DEPARTMENT (
    DepartmentId bigint IDENTITY(1,1) NOT NULL,
    Name nvarchar(100) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_DEPARTMENT PRIMARY KEY (DepartmentId),
    CONSTRAINT UQ_DEPARTMENT_Name UNIQUE (Name)
);
GO

IF OBJECT_ID('dbo.WAREHOUSE', 'U') IS NULL
CREATE TABLE dbo.WAREHOUSE (
    WarehouseId bigint IDENTITY(1,1) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Location nvarchar(200) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_WAREHOUSE PRIMARY KEY (WarehouseId),
    CONSTRAINT UQ_WAREHOUSE_Name UNIQUE (Name)
);
GO

IF OBJECT_ID('dbo.ROLE', 'U') IS NULL
CREATE TABLE dbo.ROLE (
    RoleId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name varchar(100) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_ROLE PRIMARY KEY (RoleId),
    CONSTRAINT UQ_ROLE_Code UNIQUE (Code),
    CONSTRAINT UQ_ROLE_Name UNIQUE (Name)
);
GO

IF OBJECT_ID('dbo.[USER]', 'U') IS NULL
CREATE TABLE dbo.[USER] (
    UserId bigint IDENTITY(1,1) NOT NULL,
    Username varchar(100) NOT NULL,
    Email varchar(200) NOT NULL,
    DisplayName nvarchar(200) NOT NULL,
    PasswordHash varchar(max) NULL, -- Auth
    IsActive bit NOT NULL DEFAULT 1,
    PermVersion int NOT NULL DEFAULT 1,
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_USER PRIMARY KEY (UserId),
    CONSTRAINT UQ_USER_Username UNIQUE (Username),
    CONSTRAINT UQ_USER_Email UNIQUE (Email)
);
GO

IF OBJECT_ID('dbo.AUTH_SESSION', 'U') IS NULL
CREATE TABLE dbo.AUTH_SESSION (
    AuthSessionId bigint IDENTITY(1,1) NOT NULL,
    UserId bigint NOT NULL,
    RefreshTokenHash varchar(255) NOT NULL,
    DeviceInfo nvarchar(500) NULL,
    IpAddress varchar(50) NULL,
    ExpiresAtUtc datetime2(7) NOT NULL,
    CreatedAtUtc datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    RevokedAtUtc datetime2(7) NULL,
    CONSTRAINT PK_AUTH_SESSION PRIMARY KEY (AuthSessionId),
    CONSTRAINT FK_AUTH_SESSION_USER FOREIGN KEY (UserId) REFERENCES dbo.[USER](UserId)
);
GO
CREATE INDEX IX_AUTH_SESSION_UserId ON dbo.AUTH_SESSION(UserId);
GO

IF OBJECT_ID('dbo.USER_DEPARTMENT', 'U') IS NULL
CREATE TABLE dbo.USER_DEPARTMENT (
    UserDepartmentId bigint IDENTITY(1,1) NOT NULL,
    UserId bigint NOT NULL,
    DepartmentId bigint NOT NULL,
    IsPrimary bit NOT NULL DEFAULT 0,
    AssignedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_USER_DEPARTMENT PRIMARY KEY (UserDepartmentId),
    CONSTRAINT FK_USER_DEPARTMENT_USER FOREIGN KEY (UserId) REFERENCES dbo.[USER](UserId),
    CONSTRAINT FK_USER_DEPARTMENT_DEPT FOREIGN KEY (DepartmentId) REFERENCES dbo.DEPARTMENT(DepartmentId),
    CONSTRAINT UQ_USER_DEPARTMENT UNIQUE (UserId, DepartmentId)
);
GO

IF OBJECT_ID('dbo.USER_ROLE', 'U') IS NULL
CREATE TABLE dbo.USER_ROLE (
    UserRoleId bigint IDENTITY(1,1) NOT NULL,
    UserId bigint NOT NULL,
    RoleId bigint NOT NULL,
    AssignedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_USER_ROLE PRIMARY KEY (UserRoleId),
    CONSTRAINT FK_USER_ROLE_USER FOREIGN KEY (UserId) REFERENCES dbo.[USER](UserId),
    CONSTRAINT FK_USER_ROLE_ROLE FOREIGN KEY (RoleId) REFERENCES dbo.ROLE(RoleId),
    CONSTRAINT UQ_USER_ROLE UNIQUE (UserId, RoleId)
);
GO

IF OBJECT_ID('dbo.PERMISSION', 'U') IS NULL
CREATE TABLE dbo.PERMISSION (
    PermissionId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(200) NOT NULL,
    Name varchar(200) NOT NULL,
    Description nvarchar(max) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_PERMISSION PRIMARY KEY (PermissionId),
    CONSTRAINT UQ_PERMISSION_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.ROLE_PERMISSION', 'U') IS NULL
CREATE TABLE dbo.ROLE_PERMISSION (
    RolePermissionId bigint IDENTITY(1,1) NOT NULL,
    RoleId bigint NOT NULL,
    PermissionId bigint NOT NULL,
    GrantedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    GrantedByUserId bigint NULL,
    CONSTRAINT PK_ROLE_PERMISSION PRIMARY KEY (RolePermissionId),
    CONSTRAINT FK_ROLE_PERM_ROLE FOREIGN KEY (RoleId) REFERENCES dbo.ROLE(RoleId),
    CONSTRAINT FK_ROLE_PERM_PERM FOREIGN KEY (PermissionId) REFERENCES dbo.PERMISSION(PermissionId),
    CONSTRAINT UQ_ROLE_PERMISSION UNIQUE (RoleId, PermissionId)
);
GO

IF OBJECT_ID('dbo.ROLE_PERMISSION_SCOPE', 'U') IS NULL
CREATE TABLE dbo.ROLE_PERMISSION_SCOPE (
    RolePermissionScopeId bigint IDENTITY(1,1) NOT NULL,
    RolePermissionId bigint NOT NULL,
    AccessScopeTypeId bigint NOT NULL,
    DepartmentId bigint NULL,
    WarehouseId bigint NULL,
    CONSTRAINT PK_ROLE_PERMISSION_SCOPE PRIMARY KEY (RolePermissionScopeId),
    CONSTRAINT FK_RPS_RP FOREIGN KEY (RolePermissionId) REFERENCES dbo.ROLE_PERMISSION(RolePermissionId),
    CONSTRAINT FK_RPS_SCOPE FOREIGN KEY (AccessScopeTypeId) REFERENCES dbo.ACCESS_SCOPE_TYPE(AccessScopeTypeId),
    CONSTRAINT FK_RPS_DEPT FOREIGN KEY (DepartmentId) REFERENCES dbo.DEPARTMENT(DepartmentId),
    CONSTRAINT FK_RPS_WH FOREIGN KEY (WarehouseId) REFERENCES dbo.WAREHOUSE(WarehouseId)
);
GO

/* ============================================================
   4. INVENTORY CATALOG
   ============================================================ */

IF OBJECT_ID('dbo.CATEGORY', 'U') IS NULL
CREATE TABLE dbo.CATEGORY (
    CategoryId bigint IDENTITY(1,1) NOT NULL,
    Name nvarchar(200) NOT NULL,
    ParentCategoryId bigint NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_CATEGORY PRIMARY KEY (CategoryId),
    CONSTRAINT FK_CATEGORY_PARENT FOREIGN KEY (ParentCategoryId) REFERENCES dbo.CATEGORY(CategoryId)
);
GO

IF OBJECT_ID('dbo.PRODUCT', 'U') IS NULL
CREATE TABLE dbo.PRODUCT (
    ProductId bigint IDENTITY(1,1) NOT NULL,
    SKU varchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    CategoryId bigint NULL,
    UnitOfMeasure varchar(50) NOT NULL DEFAULT 'EACH',
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_PRODUCT PRIMARY KEY (ProductId),
    CONSTRAINT FK_PRODUCT_CATEGORY FOREIGN KEY (CategoryId) REFERENCES dbo.CATEGORY(CategoryId),
    CONSTRAINT UQ_PRODUCT_SKU UNIQUE (SKU)
);
GO

/* ============================================================
   5. WORKFLOW DEFINITIONS
   ============================================================ */

IF OBJECT_ID('dbo.WORKFLOW_DEFINITION', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_DEFINITION (
    WorkflowDefinitionId bigint IDENTITY(1,1) NOT NULL,
    Code varchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedByUserId bigint NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_WORKFLOW_DEFINITION PRIMARY KEY (WorkflowDefinitionId),
    CONSTRAINT UQ_WORKFLOW_DEFINITION_Code UNIQUE (Code)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_DEFINITION_VERSION', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_DEFINITION_VERSION (
    WorkflowDefinitionVersionId bigint IDENTITY(1,1) NOT NULL,
    WorkflowDefinitionId bigint NOT NULL,
    VersionNo int NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    PublishedAt datetime2(7) NOT NULL,
    PublishedByUserId bigint NULL,
    CONSTRAINT PK_WORKFLOW_DEFINITION_VERSION PRIMARY KEY (WorkflowDefinitionVersionId),
    CONSTRAINT FK_WDV_DEF FOREIGN KEY (WorkflowDefinitionId) REFERENCES dbo.WORKFLOW_DEFINITION(WorkflowDefinitionId),
    CONSTRAINT UQ_WDV_Version UNIQUE (WorkflowDefinitionId, VersionNo)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_STEP', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_STEP (
    WorkflowStepId bigint IDENTITY(1,1) NOT NULL,
    WorkflowDefinitionVersionId bigint NOT NULL,
    StepKey varchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    WorkflowStepTypeId bigint NOT NULL,
    SequenceNo int NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_WORKFLOW_STEP PRIMARY KEY (WorkflowStepId),
    CONSTRAINT FK_STEP_VER FOREIGN KEY (WorkflowDefinitionVersionId) REFERENCES dbo.WORKFLOW_DEFINITION_VERSION(WorkflowDefinitionVersionId),
    CONSTRAINT FK_STEP_TYPE FOREIGN KEY (WorkflowStepTypeId) REFERENCES dbo.WORKFLOW_STEP_TYPE(WorkflowStepTypeId),
    CONSTRAINT UQ_STEP_Key UNIQUE (WorkflowDefinitionVersionId, StepKey)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_STEP_RULE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_STEP_RULE (
    WorkflowStepRuleId bigint IDENTITY(1,1) NOT NULL,
    WorkflowStepId bigint NOT NULL,
    AssignmentModeId bigint NOT NULL,
    RoleId bigint NULL,
    DepartmentId bigint NULL,
    UseRequesterDepartment bit NOT NULL DEFAULT 0,
    AllowRequesterSelect bit NOT NULL DEFAULT 0,
    MinApprovers int NOT NULL DEFAULT 1,
    RequireAll bit NOT NULL DEFAULT 0,
    AllowReassign bit NOT NULL DEFAULT 1,
    AllowDelegate bit NOT NULL DEFAULT 1,
    SLA_Minutes int NULL,
    CONSTRAINT PK_WORKFLOW_STEP_RULE PRIMARY KEY (WorkflowStepRuleId),
    CONSTRAINT FK_RULE_STEP FOREIGN KEY (WorkflowStepId) REFERENCES dbo.WORKFLOW_STEP(WorkflowStepId),
    CONSTRAINT FK_RULE_MODE FOREIGN KEY (AssignmentModeId) REFERENCES dbo.WORKFLOW_ASSIGNMENT_MODE(AssignmentModeId),
    CONSTRAINT FK_RULE_ROLE FOREIGN KEY (RoleId) REFERENCES dbo.ROLE(RoleId),
    CONSTRAINT FK_RULE_DEPT FOREIGN KEY (DepartmentId) REFERENCES dbo.DEPARTMENT(DepartmentId)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_TRANSITION', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TRANSITION (
    WorkflowTransitionId bigint IDENTITY(1,1) NOT NULL,
    WorkflowDefinitionVersionId bigint NOT NULL,
    FromWorkflowStepId bigint NOT NULL,
    WorkflowActionTypeId bigint NOT NULL,
    ToWorkflowStepId bigint NOT NULL,
    CONSTRAINT PK_WORKFLOW_TRANSITION PRIMARY KEY (WorkflowTransitionId),
    CONSTRAINT FK_TRANS_VER FOREIGN KEY (WorkflowDefinitionVersionId) REFERENCES dbo.WORKFLOW_DEFINITION_VERSION(WorkflowDefinitionVersionId),
    CONSTRAINT FK_TRANS_FROM FOREIGN KEY (FromWorkflowStepId) REFERENCES dbo.WORKFLOW_STEP(WorkflowStepId),
    CONSTRAINT FK_TRANS_TO FOREIGN KEY (ToWorkflowStepId) REFERENCES dbo.WORKFLOW_STEP(WorkflowStepId),
    CONSTRAINT FK_TRANS_ACTION FOREIGN KEY (WorkflowActionTypeId) REFERENCES dbo.WORKFLOW_ACTION_TYPE(WorkflowActionTypeId)
    -- Typically unique constraint on From + Action
);
GO

/* ============================================================
   6. WORKFLOW RUNTIME
   ============================================================ */

IF OBJECT_ID('dbo.WORKFLOW_INSTANCE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_INSTANCE (
    WorkflowInstanceId bigint IDENTITY(1,1) NOT NULL,
    WorkflowDefinitionVersionId bigint NOT NULL,
    WorkflowInstanceStatusId bigint NOT NULL,
    InitiatorUserId bigint NOT NULL,
    BusinessEntityKey varchar(100) NOT NULL, -- e.g., RequestNo
    CurrentWorkflowStepId bigint NULL,
    StartedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt datetime2(7) NULL,
    CONSTRAINT PK_WORKFLOW_INSTANCE PRIMARY KEY (WorkflowInstanceId),
    CONSTRAINT FK_INST_VER FOREIGN KEY (WorkflowDefinitionVersionId) REFERENCES dbo.WORKFLOW_DEFINITION_VERSION(WorkflowDefinitionVersionId),
    CONSTRAINT FK_INST_STATUS FOREIGN KEY (WorkflowInstanceStatusId) REFERENCES dbo.WORKFLOW_INSTANCE_STATUS(WorkflowInstanceStatusId),
    CONSTRAINT FK_INST_USER FOREIGN KEY (InitiatorUserId) REFERENCES dbo.[USER](UserId),
    CONSTRAINT FK_INST_CURRSTEP FOREIGN KEY (CurrentWorkflowStepId) REFERENCES dbo.WORKFLOW_STEP(WorkflowStepId)
);
GO
CREATE INDEX IX_WORKFLOW_INSTANCE_Entity ON dbo.WORKFLOW_INSTANCE(BusinessEntityKey, CurrentWorkflowStepId);
GO

IF OBJECT_ID('dbo.WORKFLOW_TASK', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TASK (
    WorkflowTaskId bigint IDENTITY(1,1) NOT NULL,
    WorkflowInstanceId bigint NOT NULL,
    WorkflowStepId bigint NOT NULL,
    WorkflowTaskStatusId bigint NOT NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    DueAt datetime2(7) NULL,
    ClaimedByUserId bigint NULL,
    CompletedAt datetime2(7) NULL,
    CONSTRAINT PK_WORKFLOW_TASK PRIMARY KEY (WorkflowTaskId),
    CONSTRAINT FK_TASK_INST FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WORKFLOW_INSTANCE(WorkflowInstanceId),
    CONSTRAINT FK_TASK_STEP FOREIGN KEY (WorkflowStepId) REFERENCES dbo.WORKFLOW_STEP(WorkflowStepId),
    CONSTRAINT FK_TASK_STATUS FOREIGN KEY (WorkflowTaskStatusId) REFERENCES dbo.WORKFLOW_TASK_STATUS(WorkflowTaskStatusId),
    CONSTRAINT FK_TASK_CLAIM FOREIGN KEY (ClaimedByUserId) REFERENCES dbo.[USER](UserId)
);
GO
CREATE INDEX IX_WORKFLOW_TASK_Status ON dbo.WORKFLOW_TASK(WorkflowTaskStatusId, ClaimedByUserId);
GO

IF OBJECT_ID('dbo.WORKFLOW_TASK_ASSIGNEE', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TASK_ASSIGNEE (
    WorkflowTaskAssigneeId bigint IDENTITY(1,1) NOT NULL,
    WorkflowTaskId bigint NOT NULL,
    UserId bigint NOT NULL,
    AssigneeStatusId bigint NOT NULL,
    AssignedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    DecidedAt datetime2(7) NULL,
    CONSTRAINT PK_WORKFLOW_TASK_ASSIGNEE PRIMARY KEY (WorkflowTaskAssigneeId),
    CONSTRAINT FK_WTA_TASK FOREIGN KEY (WorkflowTaskId) REFERENCES dbo.WORKFLOW_TASK(WorkflowTaskId),
    CONSTRAINT FK_WTA_USER FOREIGN KEY (UserId) REFERENCES dbo.[USER](UserId),
    CONSTRAINT FK_WTA_STATUS FOREIGN KEY (AssigneeStatusId) REFERENCES dbo.WORKFLOW_TASK_ASSIGNEE_STATUS(AssigneeStatusId)
);
GO

IF OBJECT_ID('dbo.WORKFLOW_TASK_ACTION', 'U') IS NULL
CREATE TABLE dbo.WORKFLOW_TASK_ACTION (
    WorkflowTaskActionId bigint IDENTITY(1,1) NOT NULL,
    WorkflowTaskId bigint NOT NULL,
    WorkflowActionTypeId bigint NOT NULL,
    ActionByUserId bigint NOT NULL,
    ActionAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    Notes nvarchar(max) NULL,
    PayloadJson nvarchar(max) NULL,
    CONSTRAINT PK_WORKFLOW_TASK_ACTION PRIMARY KEY (WorkflowTaskActionId),
    CONSTRAINT FK_WTACT_TASK FOREIGN KEY (WorkflowTaskId) REFERENCES dbo.WORKFLOW_TASK(WorkflowTaskId),
    CONSTRAINT FK_WTACT_TYPE FOREIGN KEY (WorkflowActionTypeId) REFERENCES dbo.WORKFLOW_ACTION_TYPE(WorkflowActionTypeId),
    CONSTRAINT FK_WTACT_USER FOREIGN KEY (ActionByUserId) REFERENCES dbo.[USER](UserId)
);
GO

/* ============================================================
   7. INVENTORY REQUESTS & RESERVATIONS
   ============================================================ */

IF OBJECT_ID('dbo.INVENTORY_REQUEST', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_REQUEST (
    RequestId bigint IDENTITY(1,1) NOT NULL,
    RequestNo varchar(50) NOT NULL,
    RequestTypeId bigint NOT NULL,
    RequestStatusId bigint NOT NULL,
    WarehouseId bigint NOT NULL,
    RequestedByUserId bigint NOT NULL,
    RequestedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    DepartmentId bigint NOT NULL,
    Notes nvarchar(max) NULL,
    WorkflowInstanceId bigint NULL,
    CONSTRAINT PK_INVENTORY_REQUEST PRIMARY KEY (RequestId),
    CONSTRAINT FK_REQ_DEPT FOREIGN KEY (DepartmentId) REFERENCES dbo.DEPARTMENT(DepartmentId),
    CONSTRAINT UQ_INVENTORY_REQUEST_No UNIQUE (RequestNo),
    CONSTRAINT FK_REQ_TYPE FOREIGN KEY (RequestTypeId) REFERENCES dbo.INVENTORY_REQUEST_TYPE(RequestTypeId),
    CONSTRAINT FK_REQ_STATUS FOREIGN KEY (RequestStatusId) REFERENCES dbo.INVENTORY_REQUEST_STATUS(RequestStatusId),
    CONSTRAINT FK_REQ_WH FOREIGN KEY (WarehouseId) REFERENCES dbo.WAREHOUSE(WarehouseId),
    CONSTRAINT FK_REQ_USER FOREIGN KEY (RequestedByUserId) REFERENCES dbo.[USER](UserId),
    CONSTRAINT FK_REQ_WFINST FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WORKFLOW_INSTANCE(WorkflowInstanceId)
);
GO
CREATE INDEX IX_INVENTORY_REQUEST_Status ON dbo.INVENTORY_REQUEST(RequestStatusId, RequestedAt);
GO

IF OBJECT_ID('dbo.INVENTORY_REQUEST_LINE', 'U') IS NULL
CREATE TABLE dbo.INVENTORY_REQUEST_LINE (
    RequestLineId bigint IDENTITY(1,1) NOT NULL,
    RequestId bigint NOT NULL,
    ProductId bigint NOT NULL,
    QtyRequested decimal(18,4) NOT NULL,
    QtyApproved decimal(18,4) NULL,
    QtyFulfilled decimal(18,4) NOT NULL DEFAULT 0,
    LineNotes nvarchar(max) NULL,
    CONSTRAINT PK_INVENTORY_REQUEST_LINE PRIMARY KEY (RequestLineId),
    CONSTRAINT FK_REQL_REQ FOREIGN KEY (RequestId) REFERENCES dbo.INVENTORY_REQUEST(RequestId),
    CONSTRAINT FK_REQL_PROD FOREIGN KEY (ProductId) REFERENCES dbo.PRODUCT(ProductId)
);
GO

IF OBJECT_ID('dbo.RESERVATION', 'U') IS NULL
CREATE TABLE dbo.RESERVATION (
    ReservationId bigint IDENTITY(1,1) NOT NULL,
    ReservationNo varchar(50) NOT NULL,
    ReservationStatusId bigint NOT NULL,
    WarehouseId bigint NOT NULL,
    RequestId bigint NULL,
    ReservedByUserId bigint NOT NULL,
    ReservedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt datetime2(7) NULL,
    Notes nvarchar(max) NULL,
    CONSTRAINT PK_RESERVATION PRIMARY KEY (ReservationId),
    CONSTRAINT UQ_RESERVATION_No UNIQUE (ReservationNo),
    CONSTRAINT FK_RES_STATUS FOREIGN KEY (ReservationStatusId) REFERENCES dbo.RESERVATION_STATUS(ReservationStatusId),
    CONSTRAINT FK_RES_WH FOREIGN KEY (WarehouseId) REFERENCES dbo.WAREHOUSE(WarehouseId),
    CONSTRAINT FK_RES_REQ FOREIGN KEY (RequestId) REFERENCES dbo.INVENTORY_REQUEST(RequestId),
    CONSTRAINT FK_RES_USER FOREIGN KEY (ReservedByUserId) REFERENCES dbo.[USER](UserId)
);
GO

IF OBJECT_ID('dbo.RESERVATION_LINE', 'U') IS NULL
CREATE TABLE dbo.RESERVATION_LINE (
    ReservationLineId bigint IDENTITY(1,1) NOT NULL,
    ReservationId bigint NOT NULL,
    RequestLineId bigint NULL,
    ProductId bigint NOT NULL,
    QtyReserved decimal(18,4) NOT NULL,
    CONSTRAINT PK_RESERVATION_LINE PRIMARY KEY (ReservationLineId),
    CONSTRAINT FK_RESL_RES FOREIGN KEY (ReservationId) REFERENCES dbo.RESERVATION(ReservationId),
    CONSTRAINT FK_RESL_REQL FOREIGN KEY (RequestLineId) REFERENCES dbo.INVENTORY_REQUEST_LINE(RequestLineId),
    CONSTRAINT FK_RESL_PROD FOREIGN KEY (ProductId) REFERENCES dbo.PRODUCT(ProductId)
);
GO

/* ============================================================
   8. STOCK LEDGER & LEVELS
   ============================================================ */

IF OBJECT_ID('dbo.STOCK_LEVEL', 'U') IS NULL
CREATE TABLE dbo.STOCK_LEVEL (
    StockLevelId bigint IDENTITY(1,1) NOT NULL,
    WarehouseId bigint NOT NULL,
    ProductId bigint NOT NULL,
    OnHandQty decimal(18,4) NOT NULL DEFAULT 0,
    ReservedQty decimal(18,4) NOT NULL DEFAULT 0,
    UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_STOCK_LEVEL PRIMARY KEY (StockLevelId),
    CONSTRAINT UQ_STOCK_LEVEL UNIQUE (WarehouseId, ProductId),
    CONSTRAINT FK_LEVEL_WH FOREIGN KEY (WarehouseId) REFERENCES dbo.WAREHOUSE(WarehouseId),
    CONSTRAINT FK_LEVEL_PROD FOREIGN KEY (ProductId) REFERENCES dbo.PRODUCT(ProductId),
    CONSTRAINT CK_STOCK_LEVEL_OnHand CHECK (OnHandQty >= 0),
    CONSTRAINT CK_STOCK_LEVEL_Reserved CHECK (ReservedQty >= 0),
    CONSTRAINT CK_STOCK_LEVEL_Valid CHECK (ReservedQty <= OnHandQty)
);
GO

IF OBJECT_ID('dbo.STOCK_MOVEMENT', 'U') IS NULL
CREATE TABLE dbo.STOCK_MOVEMENT (
    StockMovementId bigint IDENTITY(1,1) NOT NULL,
    MovementTypeId bigint NOT NULL,
    MovementStatusId bigint NOT NULL,
    ReasonCodeId bigint NULL,
    WarehouseId bigint NOT NULL,
    ReferenceRequestId bigint NULL,
    ReferenceReservationId bigint NULL,
    CreatedByUserId bigint NOT NULL,
    CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    PostedByUserId bigint NULL,
    PostedAt datetime2(7) NULL,
    ReversedMovementId bigint NULL,
    Notes nvarchar(max) NULL,
    CONSTRAINT PK_STOCK_MOVEMENT PRIMARY KEY (StockMovementId),
    CONSTRAINT FK_MOV_TYPE FOREIGN KEY (MovementTypeId) REFERENCES dbo.INVENTORY_MOVEMENT_TYPE(MovementTypeId),
    CONSTRAINT FK_MOV_STATUS FOREIGN KEY (MovementStatusId) REFERENCES dbo.INVENTORY_MOVEMENT_STATUS(MovementStatusId),
    CONSTRAINT FK_MOV_REASON FOREIGN KEY (ReasonCodeId) REFERENCES dbo.INVENTORY_REASON_CODE(ReasonCodeId),
    CONSTRAINT FK_MOV_WH FOREIGN KEY (WarehouseId) REFERENCES dbo.WAREHOUSE(WarehouseId),
    CONSTRAINT FK_MOV_REQ FOREIGN KEY (ReferenceRequestId) REFERENCES dbo.INVENTORY_REQUEST(RequestId),
    CONSTRAINT FK_MOV_RES FOREIGN KEY (ReferenceReservationId) REFERENCES dbo.RESERVATION(ReservationId),
    CONSTRAINT FK_MOV_USER FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[USER](UserId)
);
GO

IF OBJECT_ID('dbo.STOCK_MOVEMENT_LINE', 'U') IS NULL
CREATE TABLE dbo.STOCK_MOVEMENT_LINE (
    StockMovementLineId bigint IDENTITY(1,1) NOT NULL,
    StockMovementId bigint NOT NULL,
    ProductId bigint NOT NULL,
    QtyDeltaOnHand decimal(18,4) NOT NULL DEFAULT 0,
    QtyDeltaReserved decimal(18,4) NOT NULL DEFAULT 0,
    UnitCost decimal(18,4) NULL,
    LineNotes nvarchar(max) NULL,
    CONSTRAINT PK_STOCK_MOVEMENT_LINE PRIMARY KEY (StockMovementLineId),
    CONSTRAINT FK_MOVL_MOV FOREIGN KEY (StockMovementId) REFERENCES dbo.STOCK_MOVEMENT(StockMovementId),
    CONSTRAINT FK_MOVL_PROD FOREIGN KEY (ProductId) REFERENCES dbo.PRODUCT(ProductId)
);
GO
CREATE INDEX IX_STOCK_MOVEMENT_LINE_Prod ON dbo.STOCK_MOVEMENT_LINE(ProductId);
GO

/* ============================================================
   9. SECURITY EVENTS TYPE SEEDER (Ensure exists if not in static seeder)
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.SECURITY_EVENT_TYPE WHERE Code='LOGIN_SUCCESS')
INSERT dbo.SECURITY_EVENT_TYPE (Code, Name) VALUES
('LOGIN_SUCCESS', 'Login Success'),
('LOGIN_FAILURE', 'Login Failure'),
('TOKEN_REFRESH', 'Token Refresh'),
('LOGOUT', 'Logout');
GO
