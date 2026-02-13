using InvServer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure;

public class InvDbContext : DbContext
{
    public InvDbContext(DbContextOptions<InvDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RolePermissionScope> RolePermissionScopes => Set<RolePermissionScope>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowStepRule> WorkflowStepRules => Set<WorkflowStepRule>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowStepType> WorkflowStepTypes => Set<WorkflowStepType>();
    public DbSet<WorkflowActionType> WorkflowActionTypes => Set<WorkflowActionType>();
    public DbSet<WorkflowInstanceStatus> WorkflowInstanceStatuses => Set<WorkflowInstanceStatus>();
    public DbSet<WorkflowTaskStatus> WorkflowTaskStatuses => Set<WorkflowTaskStatus>();
    public DbSet<WorkflowTaskAssigneeStatus> WorkflowTaskAssigneeStatuses => Set<WorkflowTaskAssigneeStatus>();
    public DbSet<InventoryRequestStatus> InventoryRequestStatuses => Set<InventoryRequestStatus>();
    public DbSet<InventoryMovementType> InventoryMovementTypes => Set<InventoryMovementType>();
    public DbSet<InventoryMovementStatus> InventoryMovementStatuses => Set<InventoryMovementStatus>();
    public DbSet<InventoryReasonCode> InventoryReasonCodes => Set<InventoryReasonCode>();
    public DbSet<ReservationStatus> ReservationStatuses => Set<ReservationStatus>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<WorkflowTaskAssignee> WorkflowTaskAssignees => Set<WorkflowTaskAssignee>();
    public DbSet<WorkflowTaskAction> WorkflowTaskActions => Set<WorkflowTaskAction>();
    public DbSet<WorkflowInstanceManualAssignment> WorkflowInstanceManualAssignments => Set<WorkflowInstanceManualAssignment>();
    public DbSet<InventoryRequest> InventoryRequests => Set<InventoryRequest>();
    public DbSet<InventoryRequestLine> InventoryRequestLines => Set<InventoryRequestLine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationLine> ReservationLines => Set<ReservationLine>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockMovementLine> StockMovementLines => Set<StockMovementLine>();
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;
    public DbSet<AccessScopeType> AccessScopeTypes { get; set; } = null!;
    public DbSet<InventoryRequestType> InventoryRequestTypes => Set<InventoryRequestType>();
    public DbSet<WorkflowAssignmentMode> WorkflowAssignmentModes => Set<WorkflowAssignmentMode>();
    public DbSet<WorkflowConditionOperator> WorkflowConditionOperators => Set<WorkflowConditionOperator>();
    public DbSet<SecurityEventType> SecurityEventTypes => Set<SecurityEventType>();

    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
 
        // RBAC unique constraints
        modelBuilder.Entity<Permission>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        // WorkflowTemplate unique constraints
        modelBuilder.Entity<WorkflowTemplate>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<WorkflowTemplate>().HasIndex(x => x.Name).IsUnique();

        // StepKey unique per template
        modelBuilder.Entity<WorkflowStep>()
            .HasIndex(x => new { x.WorkflowTemplateId, x.StepKey })
            .IsUnique();

        // Only one rule per step
        modelBuilder.Entity<WorkflowStepRule>()
            .HasIndex(x => x.WorkflowStepId)
            .IsUnique();

        // Only one transition per (template + fromStep + action)
        modelBuilder.Entity<WorkflowTransition>()
            .HasIndex(x => new { x.WorkflowTemplateId, x.FromWorkflowStepId, x.WorkflowActionTypeId })
            .IsUnique();

        // Step <-> Rule 1:1 (delete step = delete rule)
        modelBuilder.Entity<WorkflowStep>()
            .HasOne(s => s.Rule)
            .WithOne(r => r.WorkflowStep)
            .HasForeignKey<WorkflowStepRule>(r => r.WorkflowStepId)
            .OnDelete(DeleteBehavior.Cascade);

        // Template deletes steps/transitions (safe for DRAFT cleanup)
        modelBuilder.Entity<WorkflowTemplate>()
            .HasMany(t => t.Steps)
            .WithOne(s => s.WorkflowTemplate)
            .HasForeignKey(s => s.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowTemplate>()
            .HasMany(t => t.Transitions)
            .WithOne(tr => tr.WorkflowTemplate)
            .HasForeignKey(tr => tr.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent "cascade cycle" issues on transitions
        modelBuilder.Entity<WorkflowTransition>()
            .HasOne(t => t.FromStep)
            .WithMany()
            .HasForeignKey(t => t.FromWorkflowStepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkflowTransition>()
            .HasOne(t => t.ToStep)
            .WithMany()
            .HasForeignKey(t => t.ToWorkflowStepId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory unique constraints
        modelBuilder.Entity<UnitOfMeasure>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<InventoryRequest>().HasIndex(x => x.RequestNo).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.SKU).IsUnique();
        modelBuilder.Entity<StockLevel>()
            .HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
        modelBuilder.Entity<Reservation>().HasIndex(x => x.ReservationNo).IsUnique();

        // Idempotency unique constraint
        modelBuilder.Entity<IdempotencyKey>()
            .HasIndex(x => new { x.UserId, x.RouteKey, x.Key }).IsUnique();

        // Precise decimal configuration
        var decimalEntities = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

        foreach (var property in decimalEntities)
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        // Configuration for reserved table/keyword name
        modelBuilder.Entity<User>().ToTable("USER");
    }
}
