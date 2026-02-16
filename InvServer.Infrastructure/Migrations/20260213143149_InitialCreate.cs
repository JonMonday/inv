using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCESS_SCOPE_TYPE",
                columns: table => new
                {
                    AccessScopeTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCESS_SCOPE_TYPE", x => x.AccessScopeTypeId);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORY",
                columns: table => new
                {
                    CategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORY", x => x.CategoryId);
                    table.ForeignKey(
                        name: "FK_CATEGORY_CATEGORY_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "DEPARTMENT",
                columns: table => new
                {
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "IDEMPOTENCY_KEY",
                columns: table => new
                {
                    IdempotencyKeyId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MovementId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDEMPOTENCY_KEY", x => x.IdempotencyKeyId);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_MOVEMENT_STATUS",
                columns: table => new
                {
                    MovementStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_MOVEMENT_STATUS", x => x.MovementStatusId);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_MOVEMENT_TYPE",
                columns: table => new
                {
                    MovementTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_MOVEMENT_TYPE", x => x.MovementTypeId);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REASON_CODE",
                columns: table => new
                {
                    ReasonCodeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REASON_CODE", x => x.ReasonCodeId);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REQUEST_STATUS",
                columns: table => new
                {
                    RequestStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REQUEST_STATUS", x => x.RequestStatusId);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REQUEST_TYPE",
                columns: table => new
                {
                    RequestTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REQUEST_TYPE", x => x.RequestTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PERMISSION",
                columns: table => new
                {
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERMISSION", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "RESERVATION_STATUS",
                columns: table => new
                {
                    ReservationStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESERVATION_STATUS", x => x.ReservationStatusId);
                });

            migrationBuilder.CreateTable(
                name: "ROLE",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "SECURITY_EVENT_TYPE",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SECURITY_EVENT_TYPE", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "UNIT_OF_MEASURE",
                columns: table => new
                {
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UNIT_OF_MEASURE", x => x.UnitOfMeasureId);
                });

            migrationBuilder.CreateTable(
                name: "USER",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PermVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WAREHOUSE",
                columns: table => new
                {
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WAREHOUSE", x => x.WarehouseId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_ACTION_TYPE",
                columns: table => new
                {
                    WorkflowActionTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDecision = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemAction = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_ACTION_TYPE", x => x.WorkflowActionTypeId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_ASSIGNMENT_MODE",
                columns: table => new
                {
                    AssignmentModeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_ASSIGNMENT_MODE", x => x.AssignmentModeId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_CONDITION_OPERATOR",
                columns: table => new
                {
                    WorkflowConditionOperatorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_CONDITION_OPERATOR", x => x.WorkflowConditionOperatorId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_INSTANCE_STATUS",
                columns: table => new
                {
                    WorkflowInstanceStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_INSTANCE_STATUS", x => x.WorkflowInstanceStatusId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_STEP_TYPE",
                columns: table => new
                {
                    WorkflowStepTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_STEP_TYPE", x => x.WorkflowStepTypeId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK_ASSIGNEE_STATUS",
                columns: table => new
                {
                    AssigneeStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK_ASSIGNEE_STATUS", x => x.AssigneeStatusId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK_STATUS",
                columns: table => new
                {
                    WorkflowTaskStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK_STATUS", x => x.WorkflowTaskStatusId);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TEMPLATE",
                columns: table => new
                {
                    WorkflowTemplateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SourceTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TEMPLATE", x => x.WorkflowTemplateId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TEMPLATE_WORKFLOW_TEMPLATE_SourceTemplateId",
                        column: x => x.SourceTemplateId,
                        principalTable: "WORKFLOW_TEMPLATE",
                        principalColumn: "WorkflowTemplateId");
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSION",
                columns: table => new
                {
                    RolePermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PERMISSION", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_PERMISSION_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "PERMISSION",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_ROLE_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ROLE",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_PRODUCT_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "CategoryId");
                    table.ForeignKey(
                        name: "FK_PRODUCT_UNIT_OF_MEASURE_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalTable: "UNIT_OF_MEASURE",
                        principalColumn: "UnitOfMeasureId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOG",
                columns: table => new
                {
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityTable = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOG_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AUTH_SESSION",
                columns: table => new
                {
                    AuthSessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUTH_SESSION", x => x.AuthSessionId);
                    table.ForeignKey(
                        name: "FK_AUTH_SESSION_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_DEPARTMENT",
                columns: table => new
                {
                    UserDepartmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_DEPARTMENT", x => x.UserDepartmentId);
                    table.ForeignKey(
                        name: "FK_USER_DEPARTMENT_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_DEPARTMENT_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_ROLE",
                columns: table => new
                {
                    UserRoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_ROLE", x => x.UserRoleId);
                    table.ForeignKey(
                        name: "FK_USER_ROLE_ROLE_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ROLE",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_ROLE_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_STEP",
                columns: table => new
                {
                    WorkflowStepId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkflowStepTypeId = table.Column<long>(type: "bigint", nullable: false),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_STEP", x => x.WorkflowStepId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_WORKFLOW_STEP_TYPE_WorkflowStepTypeId",
                        column: x => x.WorkflowStepTypeId,
                        principalTable: "WORKFLOW_STEP_TYPE",
                        principalColumn: "WorkflowStepTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_WORKFLOW_TEMPLATE_WorkflowTemplateId",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WORKFLOW_TEMPLATE",
                        principalColumn: "WorkflowTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSION_SCOPE",
                columns: table => new
                {
                    RolePermissionScopeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RolePermissionId = table.Column<long>(type: "bigint", nullable: false),
                    AccessScopeTypeId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE_PERMISSION_SCOPE", x => x.RolePermissionScopeId);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SCOPE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SCOPE_ROLE_PERMISSION_RolePermissionId",
                        column: x => x.RolePermissionId,
                        principalTable: "ROLE_PERMISSION",
                        principalColumn: "RolePermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROLE_PERMISSION_SCOPE_WAREHOUSE_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "WAREHOUSE",
                        principalColumn: "WarehouseId");
                });

            migrationBuilder.CreateTable(
                name: "STOCK_LEVEL",
                columns: table => new
                {
                    StockLevelId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    OnHandQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReservedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK_LEVEL", x => x.StockLevelId);
                    table.ForeignKey(
                        name: "FK_STOCK_LEVEL_PRODUCT_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STOCK_LEVEL_WAREHOUSE_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "WAREHOUSE",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_INSTANCE",
                columns: table => new
                {
                    WorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowInstanceStatusId = table.Column<long>(type: "bigint", nullable: false),
                    InitiatorUserId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessEntityKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentWorkflowStepId = table.Column<long>(type: "bigint", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_INSTANCE", x => x.WorkflowInstanceId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_USER_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_WORKFLOW_INSTANCE_STATUS_WorkflowInstance~",
                        column: x => x.WorkflowInstanceStatusId,
                        principalTable: "WORKFLOW_INSTANCE_STATUS",
                        principalColumn: "WorkflowInstanceStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_WORKFLOW_STEP_CurrentWorkflowStepId",
                        column: x => x.CurrentWorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId");
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_WORKFLOW_TEMPLATE_WorkflowTemplateId",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WORKFLOW_TEMPLATE",
                        principalColumn: "WorkflowTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_STEP_RULE",
                columns: table => new
                {
                    WorkflowStepRuleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowStepId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentModeId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    UseRequesterDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    AllowRequesterSelect = table.Column<bool>(type: "boolean", nullable: false),
                    MinApprovers = table.Column<int>(type: "integer", nullable: false),
                    RequireAll = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReassign = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDelegate = table.Column<bool>(type: "boolean", nullable: false),
                    SLA_Minutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_STEP_RULE", x => x.WorkflowStepRuleId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_RULE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_RULE_ROLE_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ROLE",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_RULE_WORKFLOW_ASSIGNMENT_MODE_AssignmentModeId",
                        column: x => x.AssignmentModeId,
                        principalTable: "WORKFLOW_ASSIGNMENT_MODE",
                        principalColumn: "AssignmentModeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_STEP_RULE_WORKFLOW_STEP_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TRANSITION",
                columns: table => new
                {
                    WorkflowTransitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    FromWorkflowStepId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowActionTypeId = table.Column<long>(type: "bigint", nullable: false),
                    ToWorkflowStepId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TRANSITION", x => x.WorkflowTransitionId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TRANSITION_WORKFLOW_ACTION_TYPE_WorkflowActionType~",
                        column: x => x.WorkflowActionTypeId,
                        principalTable: "WORKFLOW_ACTION_TYPE",
                        principalColumn: "WorkflowActionTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TRANSITION_WORKFLOW_STEP_FromWorkflowStepId",
                        column: x => x.FromWorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TRANSITION_WORKFLOW_STEP_ToWorkflowStepId",
                        column: x => x.ToWorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TRANSITION_WORKFLOW_TEMPLATE_WorkflowTemplateId",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WORKFLOW_TEMPLATE",
                        principalColumn: "WorkflowTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REQUEST",
                columns: table => new
                {
                    RequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestTypeId = table.Column<long>(type: "bigint", nullable: false),
                    RequestStatusId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WorkflowInstanceId = table.Column<long>(type: "bigint", nullable: true),
                    WorkflowTemplateId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REQUEST", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_INVENTORY_REQUEST_STATUS_RequestStatusId",
                        column: x => x.RequestStatusId,
                        principalTable: "INVENTORY_REQUEST_STATUS",
                        principalColumn: "RequestStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_USER_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_WAREHOUSE_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "WAREHOUSE",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_WORKFLOW_INSTANCE_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WORKFLOW_INSTANCE",
                        principalColumn: "WorkflowInstanceId");
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_WORKFLOW_TEMPLATE_WorkflowTemplateId",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WORKFLOW_TEMPLATE",
                        principalColumn: "WorkflowTemplateId");
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT",
                columns: table => new
                {
                    ManualAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowStepId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT", x => x.ManualAssignmentId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_WORKFLOW_INSTANCE_Workf~",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WORKFLOW_INSTANCE",
                        principalColumn: "WorkflowInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_WORKFLOW_STEP_WorkflowS~",
                        column: x => x.WorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK",
                columns: table => new
                {
                    WorkflowTaskId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowStepId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowTaskStatusId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK", x => x.WorkflowTaskId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_USER_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_WORKFLOW_INSTANCE_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WORKFLOW_INSTANCE",
                        principalColumn: "WorkflowInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_WORKFLOW_STEP_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WORKFLOW_STEP",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_WORKFLOW_TASK_STATUS_WorkflowTaskStatusId",
                        column: x => x.WorkflowTaskStatusId,
                        principalTable: "WORKFLOW_TASK_STATUS",
                        principalColumn: "WorkflowTaskStatusId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REQUEST_LINE",
                columns: table => new
                {
                    RequestLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    QtyRequested = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyApproved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    QtyFulfilled = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LineNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REQUEST_LINE", x => x.RequestLineId);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_LINE_INVENTORY_REQUEST_RequestId",
                        column: x => x.RequestId,
                        principalTable: "INVENTORY_REQUEST",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REQUEST_LINE_PRODUCT_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RESERVATION",
                columns: table => new
                {
                    ReservationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReservationStatusId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<long>(type: "bigint", nullable: true),
                    ReservedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESERVATION", x => x.ReservationId);
                    table.ForeignKey(
                        name: "FK_RESERVATION_INVENTORY_REQUEST_RequestId",
                        column: x => x.RequestId,
                        principalTable: "INVENTORY_REQUEST",
                        principalColumn: "RequestId");
                    table.ForeignKey(
                        name: "FK_RESERVATION_RESERVATION_STATUS_ReservationStatusId",
                        column: x => x.ReservationStatusId,
                        principalTable: "RESERVATION_STATUS",
                        principalColumn: "ReservationStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RESERVATION_USER_ReservedByUserId",
                        column: x => x.ReservedByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RESERVATION_WAREHOUSE_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "WAREHOUSE",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK_ACTION",
                columns: table => new
                {
                    WorkflowTaskActionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowTaskId = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowActionTypeId = table.Column<long>(type: "bigint", nullable: false),
                    ActionByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ActionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK_ACTION", x => x.WorkflowTaskActionId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ACTION_USER_ActionByUserId",
                        column: x => x.ActionByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ACTION_WORKFLOW_ACTION_TYPE_WorkflowActionTyp~",
                        column: x => x.WorkflowActionTypeId,
                        principalTable: "WORKFLOW_ACTION_TYPE",
                        principalColumn: "WorkflowActionTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ACTION_WORKFLOW_TASK_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WORKFLOW_TASK",
                        principalColumn: "WorkflowTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK_ASSIGNEE",
                columns: table => new
                {
                    WorkflowTaskAssigneeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowTaskId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AssigneeStatusId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK_ASSIGNEE", x => x.WorkflowTaskAssigneeId);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ASSIGNEE_USER_UserId",
                        column: x => x.UserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ASSIGNEE_WORKFLOW_TASK_ASSIGNEE_STATUS_Assign~",
                        column: x => x.AssigneeStatusId,
                        principalTable: "WORKFLOW_TASK_ASSIGNEE_STATUS",
                        principalColumn: "AssigneeStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_ASSIGNEE_WORKFLOW_TASK_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WORKFLOW_TASK",
                        principalColumn: "WorkflowTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RESERVATION_LINE",
                columns: table => new
                {
                    ReservationLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationId = table.Column<long>(type: "bigint", nullable: false),
                    RequestLineId = table.Column<long>(type: "bigint", nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    QtyReserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESERVATION_LINE", x => x.ReservationLineId);
                    table.ForeignKey(
                        name: "FK_RESERVATION_LINE_INVENTORY_REQUEST_LINE_RequestLineId",
                        column: x => x.RequestLineId,
                        principalTable: "INVENTORY_REQUEST_LINE",
                        principalColumn: "RequestLineId");
                    table.ForeignKey(
                        name: "FK_RESERVATION_LINE_PRODUCT_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RESERVATION_LINE_RESERVATION_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "RESERVATION",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "STOCK_MOVEMENT",
                columns: table => new
                {
                    StockMovementId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MovementTypeId = table.Column<long>(type: "bigint", nullable: false),
                    MovementStatusId = table.Column<long>(type: "bigint", nullable: false),
                    ReasonCodeId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceRequestId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceReservationId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedMovementId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK_MOVEMENT", x => x.StockMovementId);
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_INVENTORY_MOVEMENT_TYPE_MovementTypeId",
                        column: x => x.MovementTypeId,
                        principalTable: "INVENTORY_MOVEMENT_TYPE",
                        principalColumn: "MovementTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_INVENTORY_REQUEST_ReferenceRequestId",
                        column: x => x.ReferenceRequestId,
                        principalTable: "INVENTORY_REQUEST",
                        principalColumn: "RequestId");
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_RESERVATION_ReferenceReservationId",
                        column: x => x.ReferenceReservationId,
                        principalTable: "RESERVATION",
                        principalColumn: "ReservationId");
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_USER_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_USER_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "USER",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_WAREHOUSE_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "WAREHOUSE",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "STOCK_MOVEMENT_LINE",
                columns: table => new
                {
                    StockMovementLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockMovementId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    QtyDeltaOnHand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyDeltaReserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    LineNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK_MOVEMENT_LINE", x => x.StockMovementLineId);
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_LINE_PRODUCT_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_STOCK_MOVEMENT_LINE_STOCK_MOVEMENT_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "STOCK_MOVEMENT",
                        principalColumn: "StockMovementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_UserId",
                table: "AUDIT_LOG",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AUTH_SESSION_UserId",
                table: "AUTH_SESSION",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_ParentCategoryId",
                table: "CATEGORY",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IDEMPOTENCY_KEY_UserId_RouteKey_Key",
                table: "IDEMPOTENCY_KEY",
                columns: new[] { "UserId", "RouteKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_DepartmentId",
                table: "INVENTORY_REQUEST",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_RequestedByUserId",
                table: "INVENTORY_REQUEST",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_RequestNo",
                table: "INVENTORY_REQUEST",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_RequestStatusId",
                table: "INVENTORY_REQUEST",
                column: "RequestStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_WarehouseId",
                table: "INVENTORY_REQUEST",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_WorkflowInstanceId",
                table: "INVENTORY_REQUEST",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_WorkflowTemplateId",
                table: "INVENTORY_REQUEST",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_LINE_ProductId",
                table: "INVENTORY_REQUEST_LINE",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REQUEST_LINE_RequestId",
                table: "INVENTORY_REQUEST_LINE",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PERMISSION_Code",
                table: "PERMISSION",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_CategoryId",
                table: "PRODUCT",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_SKU",
                table: "PRODUCT",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_UnitOfMeasureId",
                table: "PRODUCT",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_RequestId",
                table: "RESERVATION",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_ReservationNo",
                table: "RESERVATION",
                column: "ReservationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_ReservationStatusId",
                table: "RESERVATION",
                column: "ReservationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_ReservedByUserId",
                table: "RESERVATION",
                column: "ReservedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_WarehouseId",
                table: "RESERVATION",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_LINE_ProductId",
                table: "RESERVATION_LINE",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_LINE_RequestLineId",
                table: "RESERVATION_LINE",
                column: "RequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RESERVATION_LINE_ReservationId",
                table: "RESERVATION_LINE",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_Name",
                table: "ROLE",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_PermissionId",
                table: "ROLE_PERMISSION",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_RoleId",
                table: "ROLE_PERMISSION",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_SCOPE_DepartmentId",
                table: "ROLE_PERMISSION_SCOPE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_SCOPE_RolePermissionId",
                table: "ROLE_PERMISSION_SCOPE",
                column: "RolePermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_SCOPE_WarehouseId",
                table: "ROLE_PERMISSION_SCOPE",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_LEVEL_ProductId",
                table: "STOCK_LEVEL",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_LEVEL_WarehouseId_ProductId",
                table: "STOCK_LEVEL",
                columns: new[] { "WarehouseId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_CreatedByUserId",
                table: "STOCK_MOVEMENT",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_MovementTypeId",
                table: "STOCK_MOVEMENT",
                column: "MovementTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_PostedByUserId",
                table: "STOCK_MOVEMENT",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_ReferenceRequestId",
                table: "STOCK_MOVEMENT",
                column: "ReferenceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_ReferenceReservationId",
                table: "STOCK_MOVEMENT",
                column: "ReferenceReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_WarehouseId",
                table: "STOCK_MOVEMENT",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_LINE_ProductId",
                table: "STOCK_MOVEMENT_LINE",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_LINE_StockMovementId",
                table: "STOCK_MOVEMENT_LINE",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_UNIT_OF_MEASURE_Code",
                table: "UNIT_OF_MEASURE",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_Email",
                table: "USER",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_Username",
                table: "USER",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_DEPARTMENT_DepartmentId",
                table: "USER_DEPARTMENT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_DEPARTMENT_UserId",
                table: "USER_DEPARTMENT",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ROLE_RoleId",
                table: "USER_ROLE",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ROLE_UserId",
                table: "USER_ROLE",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_CurrentWorkflowStepId",
                table: "WORKFLOW_INSTANCE",
                column: "CurrentWorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_InitiatorUserId",
                table: "WORKFLOW_INSTANCE",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_WorkflowInstanceStatusId",
                table: "WORKFLOW_INSTANCE",
                column: "WorkflowInstanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_WorkflowTemplateId",
                table: "WORKFLOW_INSTANCE",
                column: "WorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_UserId",
                table: "WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_WorkflowInstanceId",
                table: "WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT_WorkflowStepId",
                table: "WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_WorkflowStepTypeId",
                table: "WORKFLOW_STEP",
                column: "WorkflowStepTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_WorkflowTemplateId_StepKey",
                table: "WORKFLOW_STEP",
                columns: new[] { "WorkflowTemplateId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_RULE_AssignmentModeId",
                table: "WORKFLOW_STEP_RULE",
                column: "AssignmentModeId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_RULE_DepartmentId",
                table: "WORKFLOW_STEP_RULE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_RULE_RoleId",
                table: "WORKFLOW_STEP_RULE",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_STEP_RULE_WorkflowStepId",
                table: "WORKFLOW_STEP_RULE",
                column: "WorkflowStepId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ClaimedByUserId",
                table: "WORKFLOW_TASK",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_WorkflowInstanceId",
                table: "WORKFLOW_TASK",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_WorkflowStepId",
                table: "WORKFLOW_TASK",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_WorkflowTaskStatusId",
                table: "WORKFLOW_TASK",
                column: "WorkflowTaskStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ACTION_ActionByUserId",
                table: "WORKFLOW_TASK_ACTION",
                column: "ActionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ACTION_WorkflowActionTypeId",
                table: "WORKFLOW_TASK_ACTION",
                column: "WorkflowActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ACTION_WorkflowTaskId",
                table: "WORKFLOW_TASK_ACTION",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ASSIGNEE_AssigneeStatusId",
                table: "WORKFLOW_TASK_ASSIGNEE",
                column: "AssigneeStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ASSIGNEE_UserId",
                table: "WORKFLOW_TASK_ASSIGNEE",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_ASSIGNEE_WorkflowTaskId",
                table: "WORKFLOW_TASK_ASSIGNEE",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TEMPLATE_Code",
                table: "WORKFLOW_TEMPLATE",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TEMPLATE_Name",
                table: "WORKFLOW_TEMPLATE",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TEMPLATE_SourceTemplateId",
                table: "WORKFLOW_TEMPLATE",
                column: "SourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TRANSITION_FromWorkflowStepId",
                table: "WORKFLOW_TRANSITION",
                column: "FromWorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TRANSITION_ToWorkflowStepId",
                table: "WORKFLOW_TRANSITION",
                column: "ToWorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TRANSITION_WorkflowActionTypeId",
                table: "WORKFLOW_TRANSITION",
                column: "WorkflowActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TRANSITION_WorkflowTemplateId_FromWorkflowStepId_W~",
                table: "WORKFLOW_TRANSITION",
                columns: new[] { "WorkflowTemplateId", "FromWorkflowStepId", "WorkflowActionTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ACCESS_SCOPE_TYPE");

            migrationBuilder.DropTable(
                name: "AUDIT_LOG");

            migrationBuilder.DropTable(
                name: "AUTH_SESSION");

            migrationBuilder.DropTable(
                name: "IDEMPOTENCY_KEY");

            migrationBuilder.DropTable(
                name: "INVENTORY_MOVEMENT_STATUS");

            migrationBuilder.DropTable(
                name: "INVENTORY_REASON_CODE");

            migrationBuilder.DropTable(
                name: "INVENTORY_REQUEST_TYPE");

            migrationBuilder.DropTable(
                name: "RESERVATION_LINE");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSION_SCOPE");

            migrationBuilder.DropTable(
                name: "SECURITY_EVENT_TYPE");

            migrationBuilder.DropTable(
                name: "STOCK_LEVEL");

            migrationBuilder.DropTable(
                name: "STOCK_MOVEMENT_LINE");

            migrationBuilder.DropTable(
                name: "USER_DEPARTMENT");

            migrationBuilder.DropTable(
                name: "USER_ROLE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_CONDITION_OPERATOR");

            migrationBuilder.DropTable(
                name: "WORKFLOW_INSTANCE_MANUAL_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "WORKFLOW_STEP_RULE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK_ACTION");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK_ASSIGNEE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TRANSITION");

            migrationBuilder.DropTable(
                name: "INVENTORY_REQUEST_LINE");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSION");

            migrationBuilder.DropTable(
                name: "STOCK_MOVEMENT");

            migrationBuilder.DropTable(
                name: "WORKFLOW_ASSIGNMENT_MODE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK_ASSIGNEE_STATUS");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK");

            migrationBuilder.DropTable(
                name: "WORKFLOW_ACTION_TYPE");

            migrationBuilder.DropTable(
                name: "PRODUCT");

            migrationBuilder.DropTable(
                name: "PERMISSION");

            migrationBuilder.DropTable(
                name: "ROLE");

            migrationBuilder.DropTable(
                name: "INVENTORY_MOVEMENT_TYPE");

            migrationBuilder.DropTable(
                name: "RESERVATION");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK_STATUS");

            migrationBuilder.DropTable(
                name: "CATEGORY");

            migrationBuilder.DropTable(
                name: "UNIT_OF_MEASURE");

            migrationBuilder.DropTable(
                name: "INVENTORY_REQUEST");

            migrationBuilder.DropTable(
                name: "RESERVATION_STATUS");

            migrationBuilder.DropTable(
                name: "DEPARTMENT");

            migrationBuilder.DropTable(
                name: "INVENTORY_REQUEST_STATUS");

            migrationBuilder.DropTable(
                name: "WAREHOUSE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_INSTANCE");

            migrationBuilder.DropTable(
                name: "USER");

            migrationBuilder.DropTable(
                name: "WORKFLOW_INSTANCE_STATUS");

            migrationBuilder.DropTable(
                name: "WORKFLOW_STEP");

            migrationBuilder.DropTable(
                name: "WORKFLOW_STEP_TYPE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TEMPLATE");
        }
    }
}
