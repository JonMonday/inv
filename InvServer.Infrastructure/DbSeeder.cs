using InvServer.Core.Entities;
using InvServer.Core.Constants;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(InvDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // ==============================================================================
        // 1. Departments
        // ==============================================================================
        var depts = new List<Department>();
        if (!await context.Departments.AnyAsync())
        {
            depts.AddRange(new[]
            {
                new Department { Name = "IT" },
                new Department { Name = "Finance" },
                new Department { Name = "Procurement" },
                new Department { Name = "Customs" },
                new Department { Name = "Legal" },
                new Department { Name = "Operations" },
                new Department { Name = "HR" }
            });
            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
        }
        else
        {
            depts = await context.Departments.ToListAsync();
        }

        // ==============================================================================
        // 2. Warehouses
        // ==============================================================================
        var warehouses = new List<Warehouse>();
        if (!await context.Warehouses.AnyAsync())
        {
            warehouses.Add(new Warehouse { Name = "Main Warehouse", Location = "HQ" });
            warehouses.Add(new Warehouse { Name = "Port Warehouse", Location = "Port" });
            context.Warehouses.AddRange(warehouses);
            await context.SaveChangesAsync();
        }
        else
        {
            warehouses = await context.Warehouses.ToListAsync();
        }

        // ==============================================================================
        // 3. Lookup Tables
        // ==============================================================================
        if (!await context.InventoryRequestTypes.AnyAsync())
        {
            context.InventoryRequestTypes.AddRange(
                new InventoryRequestType { Code = "STD", Name = "Standard Request", IsActive = true },
                new InventoryRequestType { Code = "URG", Name = "Urgent Request", IsActive = true },
                new InventoryRequestType { Code = "PROJ", Name = "Project Allocation", IsActive = true }
            );
        }

        if (!await context.SecurityEventTypes.AnyAsync())
        {
            context.SecurityEventTypes.AddRange(
                new SecurityEventType { Code = "LOGIN_SUCCESS", Name = "Login Success" },
                new SecurityEventType { Code = "LOGIN_FAIL", Name = "Login Failure" },
                new SecurityEventType { Code = "LOGOUT", Name = "Logout" },
                new SecurityEventType { Code = "PASSWORD_CHANGE", Name = "Password Change" },
                new SecurityEventType { Code = "ACCESS_DENIED", Name = "Access Denied" }
            );
        }

        // Workflow Lookups
        // Re-seed if new modes (ROLE) are missing. Handles migration from old ANY/ALL modes.
        if (!await context.WorkflowAssignmentModes.AnyAsync(m => m.Code == WorkflowAssignmentModeCodes.Role))
        {
            var oldModes = await context.WorkflowAssignmentModes.ToListAsync();
            context.WorkflowAssignmentModes.RemoveRange(oldModes);
            await context.SaveChangesAsync();

            context.WorkflowAssignmentModes.AddRange(
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.Role, Name = "Role Based" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.Department, Name = "Department Based" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.RequestorDepartment, Name = "Requestor Department" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.Requestor, Name = "Requestor (Initiator)" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.RequestorRole, Name = "Requestor Role" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.RoleAndDepartment, Name = "Role & Department" },
                new WorkflowAssignmentMode { Code = WorkflowAssignmentModeCodes.RequestorRoleAndDepartment, Name = "Requestor Role and Department" }
            );
        }

        if (!await context.WorkflowConditionOperators.AnyAsync())
        {
            context.WorkflowConditionOperators.AddRange(
                new WorkflowConditionOperator { Code = "EQ", Name = "Equals" },
                new WorkflowConditionOperator { Code = "GT", Name = "Greater Than" },
                new WorkflowConditionOperator { Code = "LT", Name = "Less Than" },
                new WorkflowConditionOperator { Code = "CONTAINS", Name = "Contains" }
            );
        }

        if (!await context.WorkflowTaskAssigneeStatuses.AnyAsync())
        {
            context.WorkflowTaskAssigneeStatuses.AddRange(
                new WorkflowTaskAssigneeStatus { Code = "PENDING", Name = "Pending" },
                new WorkflowTaskAssigneeStatus { Code = "APPROVED", Name = "Approved" },
                new WorkflowTaskAssigneeStatus { Code = "REJECTED", Name = "Rejected" }
            );
        }

        if (!await context.WorkflowInstanceStatuses.AnyAsync())
        {
            context.WorkflowInstanceStatuses.AddRange(
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Active, Name = "Active", IsTerminal = false },
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Suspended, Name = "Suspended", IsTerminal = false },
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Completed, Name = "Completed", IsTerminal = true },
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Rejected, Name = "Rejected", IsTerminal = true },
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Cancelled, Name = "Cancelled", IsTerminal = true },
                new WorkflowInstanceStatus { Code = WorkflowInstanceStatusCodes.Terminated, Name = "Terminated", IsTerminal = true }
            );
        }

        if (!await context.WorkflowTaskStatuses.AnyAsync())
        {
            context.WorkflowTaskStatuses.AddRange(
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Pending, Name = "Pending", IsTerminal = false },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Available, Name = "Available", IsTerminal = false },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Claimed, Name = "Claimed", IsTerminal = false },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Approved, Name = "Approved", IsTerminal = true },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Rejected, Name = "Rejected", IsTerminal = true },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Cancelled, Name = "Cancelled", IsTerminal = true },
                new WorkflowTaskStatus { Code = WorkflowTaskStatusCodes.Completed, Name = "Completed", IsTerminal = true }
            );
        }

        if (!await context.WorkflowStepTypes.AnyAsync())
        {
            context.WorkflowStepTypes.AddRange(
                new WorkflowStepType { Code = "START", Name = "Start Point" },
                new WorkflowStepType { Code = "REVIEW", Name = "Review / Input" },
                new WorkflowStepType { Code = "APPROVAL", Name = "Approval Decision" },
                new WorkflowStepType { Code = "FULFILLMENT", Name = "Fulfillment / Action" },
                new WorkflowStepType { Code = "END", Name = "End Point" }
            );
        }

        if (!await context.WorkflowActionTypes.AnyAsync())
        {
            context.WorkflowActionTypes.AddRange(
                new WorkflowActionType { Code = "SUBMIT", Name = "Submit" },
                new WorkflowActionType { Code = "APPROVE", Name = "Approve" },
                new WorkflowActionType { Code = "REJECT", Name = "Reject" },
                new WorkflowActionType { Code = "CANCEL", Name = "Cancel" },
                new WorkflowActionType { Code = "COMPLETE", Name = "Complete" },
                new WorkflowActionType { Code = "RETURN", Name = "Return" }
            );
        }

        if (!await context.InventoryMovementTypes.AnyAsync())
        {
            context.InventoryMovementTypes.AddRange(
                new InventoryMovementType { Code = MovementTypeCodes.Receipt, Name = "Stock Receipt" },
                new InventoryMovementType { Code = MovementTypeCodes.Issue, Name = "Stock Issue" },
                new InventoryMovementType { Code = MovementTypeCodes.TransferOut, Name = "Transfer Out" },
                new InventoryMovementType { Code = MovementTypeCodes.TransferIn, Name = "Transfer In" },
                new InventoryMovementType { Code = MovementTypeCodes.AdjustmentIn, Name = "Adjustment In" },
                new InventoryMovementType { Code = MovementTypeCodes.AdjustmentOut, Name = "Adjustment Out" },
                new InventoryMovementType { Code = MovementTypeCodes.Reserve, Name = "Reservation" },
                new InventoryMovementType { Code = MovementTypeCodes.Release, Name = "Reservation Release" },
                new InventoryMovementType { Code = MovementTypeCodes.ConsumeReserve, Name = "Consume Reservation" }
            );
        }

        if (!await context.InventoryMovementStatuses.AnyAsync())
        {
            context.InventoryMovementStatuses.AddRange(
                new InventoryMovementStatus { Code = MovementStatusCodes.Draft, Name = "Draft", IsTerminal = false },
                new InventoryMovementStatus { Code = MovementStatusCodes.Posted, Name = "Posted", IsTerminal = true },
                new InventoryMovementStatus { Code = MovementStatusCodes.Reversed, Name = "Reversed", IsTerminal = true }
            );
        }

        if (!await context.InventoryRequestStatuses.AnyAsync())
        {
            context.InventoryRequestStatuses.AddRange(
                new InventoryRequestStatus { Code = RequestStatusCodes.Draft, Name = "Draft", IsTerminal = false },
                new InventoryRequestStatus { Code = RequestStatusCodes.InWorkflow, Name = "In Workflow", IsTerminal = false },
                new InventoryRequestStatus { Code = RequestStatusCodes.Approved, Name = "Approved", IsTerminal = false },
                new InventoryRequestStatus { Code = RequestStatusCodes.Fulfillment, Name = "Fulfillment", IsTerminal = false },
                new InventoryRequestStatus { Code = RequestStatusCodes.Ready, Name = "Ready for Pickup", IsTerminal = false },
                new InventoryRequestStatus { Code = RequestStatusCodes.Fulfilled, Name = "Fulfilled", IsTerminal = true },
                new InventoryRequestStatus { Code = RequestStatusCodes.Rejected, Name = "Rejected", IsTerminal = true },
                new InventoryRequestStatus { Code = RequestStatusCodes.Cancelled, Name = "Cancelled", IsTerminal = true }
            );
        }

        if (!await context.AccessScopeTypes.AnyAsync())
        {
            context.AccessScopeTypes.AddRange(
                new AccessScopeType { Code = "GLOBAL", Name = "Global" },
                new AccessScopeType { Code = "DEPT", Name = "Department" },
                new AccessScopeType { Code = "WAREHOUSE", Name = "Warehouse" },
                new AccessScopeType { Code = "OWN", Name = "Own Record" }
            );
            await context.SaveChangesAsync();
        }

        await context.SaveChangesAsync();

        // ==============================================================================
        // 3a. Inventory Catalog
        // ==============================================================================
        var categories = new List<Category>();
        if (!await context.Categories.AnyAsync())
        {
            categories.AddRange(new[]
            {
                new Category { Name = "PPE & Safety" },
                new Category { Name = "Office Equipment" },
                new Category { Name = "Networking Hardware" },
                new Category { Name = "Stationery" },
                new Category { Name = "Cleaning Supplies" }
            });
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }
        else
        {
            categories = await context.Categories.ToListAsync();
        }

        var categoryByName = categories.ToDictionary(c => c.Name, c => c.CategoryId);

        var products = new List<Product>();
        if (!await context.Products.AnyAsync())
        {
            products.AddRange(new[]
            {
                new Product { SKU = "PPE-001", Name = "Safety Helmet (Yellow)", CategoryId = categoryByName["PPE & Safety"], UnitOfMeasure = "PCS" },
                new Product { SKU = "PPE-002", Name = "High-Vis Vest (XL)", CategoryId = categoryByName["PPE & Safety"], UnitOfMeasure = "PCS" },
                new Product { SKU = "IT-001", Name = "Dell Latitude 5420", CategoryId = categoryByName["Office Equipment"], UnitOfMeasure = "PCS" },
                new Product { SKU = "IT-002", Name = "Logitech MX Master 3", CategoryId = categoryByName["Office Equipment"], UnitOfMeasure = "PCS" },
                new Product { SKU = "NET-001", Name = "Cisco Catalyst 9200L", CategoryId = categoryByName["Networking Hardware"], UnitOfMeasure = "PCS" },
                new Product { SKU = "OFF-001", Name = "A4 Paper Ream (80gsm)", CategoryId = categoryByName["Stationery"], UnitOfMeasure = "REAM" },
                new Product { SKU = "OFF-002", Name = "Stapler (Heavy Duty)", CategoryId = categoryByName["Stationery"], UnitOfMeasure = "PCS" },
                new Product { SKU = "CLN-001", Name = "Sanitizer 5L", CategoryId = categoryByName["Cleaning Supplies"], UnitOfMeasure = "BTL" }
            });
            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
        else
        {
            products = await context.Products.ToListAsync();
        }

        // ==============================================================================
        // 4. Permissions (Full Catalog)
        // ==============================================================================
        var permissionCatalog = new[]
        {
            // Users & Access Control
            "user.read", "user.create", "user.update", "user.deactivate", "user.reset_password", "user.assign_department", "user.assign_role",
            "auth_session.read", "auth_session.revoke",
            "role.read", "role.create", "role.update", "role.deactivate",
            "permission.read", "permission.create", "permission.update", "permission.deactivate",
            "role_permission.grant", "role_permission.revoke", "role_permission_scope.manage",

            // Org & Locations
            "department.read", "department.create", "department.update", "department.deactivate",
            "warehouse.read", "warehouse.create", "warehouse.update", "warehouse.deactivate",

            // Inventory Setup (Catalog)
            "category.read", "category.create", "category.update", "category.deactivate",
            "product.read", "product.create", "product.update", "product.deactivate",
            "stock_level.read",

            // Inventory Requests
            "inventory_request.read", "inventory_request.create", "inventory_request.update_draft",
            "inventory_request.delete_draft", "inventory_request.submit", "inventory_request.cancel",
            "inventory_request.comment",
            "inventory_request.review_pass", "inventory_request.review_return", "inventory_request.review_comment",
            "inventory_request.approve", "inventory_request.reject", "inventory_request.send_back", "inventory_request.set_line_qty_approved",

            // Reservations
            "reservation.read", "reservation.create", "reservation.update", "reservation.extend", "reservation.cancel", "reservation.release",

            // Stock Movements
            "stock_movement.read", "stock_movement.create", "stock_movement.update", "stock_movement.post",
            "stock_movement.reverse", "stock_movement.set_unit_cost",
            "stock_movement.adjustment.create", "stock_movement.adjustment.post",

            // Lookups & Config
            "lookup.inventory_request_status.manage", "lookup.inventory_request_type.manage",
            "lookup.reservation_status.manage", "lookup.inventory_movement_type.manage",
            "lookup.inventory_movement_status.manage", "lookup.inventory_reason_code.manage",
            "lookup.workflow_task_assignee_status.manage",

            // Workflow Templates
            "workflow_template.read", "workflow_template.create", "workflow_template.update", "workflow_template.deactivate",
            "workflow_step.manage", "workflow_step_rule.manage", "workflow_transition.manage",
            "workflow_task.read_my","workflow_task.read_all","workflow_task.claim","workflow_task.action","workflow_task.read_eligible_assignees",
            "workflow_lookup.step_type.manage", "workflow_lookup.action_type.manage",
            "workflow_lookup.instance_status.manage", "workflow_lookup.task_status.manage",
            "workflow_lookup.assignment_mode.manage", "workflow_lookup.condition_operator.manage",

            // System / Audit / Idempotency
            "audit_log.read", "audit_log.export",
            "security_event_type.manage",
            "idempotency_key.read", "idempotency_key.invalidate"
        };

        var allPerms = new List<Permission>();
        var existingPerms = await context.Permissions.ToDictionaryAsync(p => p.Code);

        foreach (var code in permissionCatalog)
        {
            if (!existingPerms.ContainsKey(code))
            {
                var p = new Permission
                {
                    Code = code,
                    Name = ToHumanName(code),
                    IsActive = true
                };
                context.Permissions.Add(p);
                allPerms.Add(p);
            }
            else
            {
                allPerms.Add(existingPerms[code]);
            }
        }
        await context.SaveChangesAsync();

        allPerms = await context.Permissions.ToListAsync();

        // ==============================================================================
        // 5. Roles
        // ==============================================================================
        var roleDefs = new[]
        {
            new { Name = "SuperAdmin", Code = "SUPERADMIN", Desc = "God mode" },
            new { Name = "Administrator", Code = "ADMIN", Desc = "System Administrator" },
            new { Name = "Manager", Code = "MANAGER", Desc = "Department Manager" },
            new { Name = "Approver", Code = "APPROVER", Desc = "Workflow Approver" },
            new { Name = "Reviewer", Code = "REVIEWER", Desc = "Workflow Reviewer" },
            new { Name = "User", Code = "USER", Desc = "Standard Requester" },
            new { Name = "Storekeeper", Code = "STOREKEEPER", Desc = "Warehouse Operator" },
            new { Name = "Auditor", Code = "AUDITOR", Desc = "Read-only Auditor" }
        };

        var roles = new List<Role>();
        foreach (var def in roleDefs)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Code == def.Code);
            if (role == null)
            {
                role = new Role { Name = def.Name, Code = def.Code, Description = def.Desc, IsActive = true };
                context.Roles.Add(role);
            }
            roles.Add(role);
        }
        await context.SaveChangesAsync();

        roles = await context.Roles.ToListAsync();
        Role GetRole(string code) => roles.First(r => r.Code == code);

        // ==============================================================================
        // 6. Role Permissions Assignment (UPDATED: workflow_template + workflow_task perms)
        // ==============================================================================
        await AssignPermissionsToRole(context, GetRole("SUPERADMIN"), allPerms, "GLOBAL");
        await AssignPermissionsToRole(context, GetRole("ADMIN"), allPerms, "GLOBAL");

        // Small helper sets (avoid repeating strings everywhere)
        var workflowRuntimePerms = allPerms.Where(p =>
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.claim" ||
            p.Code == "workflow_task.action" ||
            p.Code == "workflow_task.read_eligible_assignees"
        ).ToList();

        var workflowTemplateReadPerms = allPerms.Where(p =>
            p.Code == "workflow_template.read"
        ).ToList();

        var workflowAdminPerms = allPerms.Where(p =>
            p.Code.StartsWith("workflow_template.") ||
            p.Code.StartsWith("workflow_step.") ||
            p.Code.StartsWith("workflow_transition.") ||
            p.Code.StartsWith("workflow_lookup.")
        ).ToList();

        // Auditor -> Read-only + can view tasks (my + optionally all)
        var auditorPerms = allPerms.Where(p =>
            p.Code.EndsWith(".read") ||
            p.Code.StartsWith("audit_log") ||
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.read_all" ||          // keep if you support this perm
            p.Code == "inventory_request.read" ||
            p.Code == "workflow_template.read"
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("AUDITOR"), auditorPerms, "GLOBAL");

        // Manager -> Requests + org + runtime tasks + can read templates
        var managerPerms = allPerms.Where(p =>
            p.Code.StartsWith("user.") ||
            p.Code.StartsWith("inventory_request.") ||
            p.Code.StartsWith("department.") ||
            p.Code == "inventory_request.read" ||
            p.Code == "workflow_template.read" ||
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.claim" ||
            p.Code == "workflow_task.action" ||
            p.Code == "workflow_task.read_eligible_assignees"
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("MANAGER"), managerPerms, "GLOBAL");

        // Approver -> runtime tasks + can read requests + read templates
        var approverPerms = allPerms.Where(p =>
            p.Code == "inventory_request.read" ||
            p.Code == "workflow_template.read" ||
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.claim" ||
            p.Code == "workflow_task.action" ||
            p.Code == "workflow_task.read_eligible_assignees"
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("APPROVER"), approverPerms, "GLOBAL");

        // Reviewer -> runtime tasks + can read requests + read templates
        var reviewerPerms = allPerms.Where(p =>
            p.Code == "inventory_request.read" ||
            p.Code == "workflow_template.read" ||
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.claim" ||
            p.Code == "workflow_task.action" ||
            p.Code == "workflow_task.read_eligible_assignees"
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("REVIEWER"), reviewerPerms, "GLOBAL");

        // Storekeeper -> stock/reservations/products + runtime tasks (fulfillment step) + read templates
        var storePerms = allPerms.Where(p =>
            p.Code.StartsWith("stock_") ||
            p.Code.StartsWith("reservation.") ||
            p.Code.StartsWith("product.") ||
            p.Code.StartsWith("category.") ||
            p.Code == "inventory_request.read" ||
            p.Code == "workflow_template.read" ||
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.claim" ||
            p.Code == "workflow_task.action" ||
            p.Code == "workflow_task.read_eligible_assignees"
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("STOREKEEPER"), storePerms, "GLOBAL");

        // User -> request create/update/submit/cancel + can view & action their tasks (submission/confirmation)
        var userPerms = allPerms.Where(p =>
            p.Code == "inventory_request.read" ||
            p.Code == "inventory_request.create" ||
            p.Code == "inventory_request.update_draft" ||
            p.Code == "inventory_request.submit" ||        // include if you enforce submit perm
            p.Code == "inventory_request.cancel" ||        // include if you enforce cancel perm
            p.Code == "workflow_task.read_my" ||
            p.Code == "workflow_task.action"               // needed for confirmation task action
        ).ToList();
        await AssignPermissionsToRole(context, GetRole("USER"), userPerms, "GLOBAL");

        // Optional: if you want a non-admin to manage templates (usually NOT), you can assign workflowAdminPerms to a role.
        // await AssignPermissionsToRole(context, GetRole("MANAGER"), workflowAdminPerms, "GLOBAL");

        // ==============================================================================
        // 7. Seed Users (role-per-dept + filler to ~300)
        // ==============================================================================
        if (await context.Users.CountAsync() < 10)
        {
            var usersToSeed = new List<User>();
            var userRolesToSeed = new List<UserRole>();
            var userDeptsToSeed = new List<UserDepartment>();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");

            var realNames = new[]
            {
                "Adeyemi Johnson", "Chidi Okafor", "Fatima Abubakar", "Kwame Nkrumah", "Zainab Mensah",
                "Oluwaseun Adewale", "Amadi Dike", "Bature Gambo", "Ebele Azikiwe", "Femi Kuti",
                "Gideon Okeke", "Habiba Sani", "Ibrahim Bio", "Jolomi Ayiri", "Kelechi Iheanacho",
                "Lola Shoneyin", "Musa Yaradua", "Nuhu Ribadu", "Omotola Jalade", "Patience Akpabio",
                "Quasim Alabi", "Rakiya Danjuma", "Sadiq Abubakar", "Tunde Bakare", "Uche Jombo",
                "Victoria Inyama", "Wale Adenuga", "Yemi Alade", "Zlatan Ibile", "Burnaboy Ogulu",
                "Davido Adeleke", "Tiwa Savage", "Simi Kosoko", "Kunle Afolayan", "Linda Ikeji"
            };

            int nameIndex = 0;

            // One user for each Role in each Department
            foreach (var dept in depts)
            {
                foreach (var role in roles)
                {
                    var realName = realNames[nameIndex % realNames.Length];
                    var nameParts = realName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var baseUsername = (nameParts[0].Substring(0, 1) + nameParts[1]).ToLower();

                    var username =
                        (dept.Name == "IT" && role.Code == "SUPERADMIN") ? "superadmin" :
                        (dept.Name == "IT" && role.Code == "ADMIN") ? "admin" :
                        $"{baseUsername}{nameIndex + 1}";

                    var user = new User
                    {
                        Username = username,
                        Email = $"{username}@invserver.local",
                        DisplayName = (username == "superadmin" || username == "admin") ? ToHumanName(role.Code) : realName,
                        PasswordHash = passwordHash,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    usersToSeed.Add(user);
                    nameIndex++;
                }
            }

            // Fill to ~300 with standard users
            var target = 300;
            var rnd = new Random();
            while (usersToSeed.Count < target)
            {
                var realName = realNames[nameIndex % realNames.Length];
                var nameParts = realName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var baseUsername = (nameParts[0].Substring(0, 1) + nameParts[1]).ToLower();
                var username = $"{baseUsername}{nameIndex + 1}";

                usersToSeed.Add(new User
                {
                    Username = username,
                    Email = $"{username}@invserver.local",
                    DisplayName = realName,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                nameIndex++;
            }

            context.Users.AddRange(usersToSeed);
            await context.SaveChangesAsync();

            // Map dept/role links
            // First block is strictly dept x role
            int userIdx = 0;
            foreach (var dept in depts)
            {
                foreach (var role in roles)
                {
                    var user = usersToSeed[userIdx];
                    userRolesToSeed.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
                    userDeptsToSeed.Add(new UserDepartment { UserId = user.UserId, DepartmentId = dept.DepartmentId, IsPrimary = true, AssignedAt = DateTime.UtcNow });
                    userIdx++;
                }
            }

            // Remaining users are USER role, random dept
            var userRole = GetRole("USER");
            for (; userIdx < usersToSeed.Count; userIdx++)
            {
                var user = usersToSeed[userIdx];
                var dept = depts[rnd.Next(depts.Count)];

                userRolesToSeed.Add(new UserRole { UserId = user.UserId, RoleId = userRole.RoleId, AssignedAt = DateTime.UtcNow });
                userDeptsToSeed.Add(new UserDepartment { UserId = user.UserId, DepartmentId = dept.DepartmentId, IsPrimary = true, AssignedAt = DateTime.UtcNow });
            }

            context.UserRoles.AddRange(userRolesToSeed);
            context.UserDepartments.AddRange(userDeptsToSeed);
            await context.SaveChangesAsync();
        }

        // ==============================================================================
        // 8. Workflow Templates (NO VERSIONS)
        // ==============================================================================
        async Task<WorkflowTemplate> EnsureWorkflowTemplate(string code, string name)
        {
            var tpl = await context.WorkflowTemplates.FirstOrDefaultAsync(t => t.Code == code);
            if (tpl == null)
            {
                tpl = new WorkflowTemplate
                {
                    Code = code,
                    Name = name,
                    Status = "PUBLISHED",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.WorkflowTemplates.Add(tpl);
                await context.SaveChangesAsync();
            }
            else if (tpl.Status != "PUBLISHED")
            {
                tpl.Status = "PUBLISHED";
                context.WorkflowTemplates.Update(tpl);
                await context.SaveChangesAsync();
            }
            return tpl;
        }

        var assignmentModes = await context.WorkflowAssignmentModes.ToDictionaryAsync(m => m.Code, m => m.AssignmentModeId);
        var rolesByCode = await context.Roles.ToDictionaryAsync(r => r.Code, r => r.RoleId);
        var stepTypes = await context.WorkflowStepTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowStepTypeId);
        var actionTypes = await context.WorkflowActionTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowActionTypeId);

        // Template definitions
        var templates = new[]
        {
    new { Code = "SIMPLE_APPROVE", Name = "Simple Approval Flow", ApproverRole = "MANAGER" },
    new { Code = "FINANCE_FLOW",   Name = "Finance Approval Flow", ApproverRole = "ADMIN" },
    new { Code = "STOCK_FLOW",     Name = "Stock Fulfillment Flow", ApproverRole = "MANAGER" },
};

        foreach (var t in templates)
        {
            var tpl = await EnsureWorkflowTemplate(t.Code, t.Name);

            // If steps already exist for this template, skip (idempotent)
            if (await context.WorkflowSteps.AnyAsync(s => s.WorkflowTemplateId == tpl.WorkflowTemplateId))
                continue;

            // Steps: SUBMISSION -> APPROVAL -> FULFILLMENT -> CONFIRMATION -> END
            var sSubmission = new WorkflowStep
            {
                WorkflowTemplateId = tpl.WorkflowTemplateId,
                StepKey = "SUBMISSION",
                Name = "Submission",
                WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Start],
                SequenceNo = 0,
                IsSystemRequired = true
            };

            var sApproval = new WorkflowStep
            {
                WorkflowTemplateId = tpl.WorkflowTemplateId,
                StepKey = "APPROVAL",
                Name = "Approval",
                WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Approval],
                SequenceNo = 1,
                IsSystemRequired = true
            };

            var sFulfillment = new WorkflowStep
            {
                WorkflowTemplateId = tpl.WorkflowTemplateId,
                StepKey = "FULFILLMENT",
                Name = "Fulfillment",
                WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Fulfillment],
                SequenceNo = 2,
                IsSystemRequired = true
            };

            var sConfirmation = new WorkflowStep
            {
                WorkflowTemplateId = tpl.WorkflowTemplateId,
                StepKey = "CONFIRMATION",
                Name = "Requester Confirmation",
                WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Review],
                SequenceNo = 3,
                IsSystemRequired = true
            };

            var sEnd = new WorkflowStep
            {
                WorkflowTemplateId = tpl.WorkflowTemplateId,
                StepKey = "END",
                Name = "End",
                WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.End],
                SequenceNo = 4,
                IsSystemRequired = true
            };

            context.WorkflowSteps.AddRange(sSubmission, sApproval, sFulfillment, sConfirmation, sEnd);
            await context.SaveChangesAsync();

            // Step Rules (1 rule per step)
            context.WorkflowStepRules.AddRange(
                // Submission: Requestor
                new WorkflowStepRule
                {
                    WorkflowStepId = sSubmission.WorkflowStepId,
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Requestor]
                },

                // Approval: Role-based (template approver role)
                new WorkflowStepRule
                {
                    WorkflowStepId = sApproval.WorkflowStepId,
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Role],
                    RoleId = rolesByCode[t.ApproverRole],
                    AllowRequesterSelect = true
                },

                // Fulfillment: Storekeeper
                new WorkflowStepRule
                {
                    WorkflowStepId = sFulfillment.WorkflowStepId,
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Role],
                    RoleId = rolesByCode["STOREKEEPER"],
                    AllowRequesterSelect = true
                },

                // Confirmation: Requestor
                new WorkflowStepRule
                {
                    WorkflowStepId = sConfirmation.WorkflowStepId,
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Requestor]
                }
            );

            await context.SaveChangesAsync();

            // Transitions (unique per template + fromStep + action)
            context.WorkflowTransitions.AddRange(
                // Submission -> Approval (SUBMIT)
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sSubmission.WorkflowStepId,
                    ToWorkflowStepId = sApproval.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Submit]
                },

                // Approval -> Fulfillment (APPROVE)
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sApproval.WorkflowStepId,
                    ToWorkflowStepId = sFulfillment.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Approve]
                },

                // Fulfillment -> Confirmation (COMPLETE)
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sFulfillment.WorkflowStepId,
                    ToWorkflowStepId = sConfirmation.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Complete]
                },

                // Confirmation -> End (APPROVE)
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sConfirmation.WorkflowStepId,
                    ToWorkflowStepId = sEnd.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Approve]
                },

                // CANCEL only from SUBMISSION
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sSubmission.WorkflowStepId,
                    ToWorkflowStepId = sEnd.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Cancel]
                },

                // REJECT from ANY step -> SUBMISSION
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sApproval.WorkflowStepId,
                    ToWorkflowStepId = sSubmission.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Reject]
                },
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sFulfillment.WorkflowStepId,
                    ToWorkflowStepId = sSubmission.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Reject]
                },
                new WorkflowTransition
                {
                    WorkflowTemplateId = tpl.WorkflowTemplateId,
                    FromWorkflowStepId = sConfirmation.WorkflowStepId,
                    ToWorkflowStepId = sSubmission.WorkflowStepId,
                    WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Reject]
                }
            );

            await context.SaveChangesAsync();
        }

        // PATCH: Ensure all Approval/Fulfillment rules allow requester select
        var rulesToPatch = await context.WorkflowStepRules
            .Include(r => r.WorkflowStep)
            .Where(r => 
                (r.WorkflowStep.StepKey == "APPROVAL" || r.WorkflowStep.StepKey == "FULFILLMENT") && 
                !r.AllowRequesterSelect)
            .ToListAsync();

        if (rulesToPatch.Any())
        {
            foreach (var r in rulesToPatch)
            {
                r.AllowRequesterSelect = true;
            }
            await context.SaveChangesAsync();
        }


        // ==============================================================================
        // 9. Initial Stock
        // ==============================================================================
        if (!await context.StockMovements.AnyAsync())
        {
            var receiptType = await context.InventoryMovementTypes.FirstAsync(x => x.Code == MovementTypeCodes.Receipt);
            var postedStatus = await context.InventoryMovementStatuses.FirstAsync(x => x.Code == MovementStatusCodes.Posted);
            var adminUser = await context.Users.FirstAsync(x => x.Username == "admin");
            var allWarehouses = await context.Warehouses.ToListAsync();
            var allProducts = await context.Products.ToListAsync();
            var rnd = new Random();

            foreach (var wh in allWarehouses)
            {
                var stockIn = new StockMovement
                {
                    MovementTypeId = receiptType.MovementTypeId,
                    MovementStatusId = postedStatus.MovementStatusId,
                    WarehouseId = wh.WarehouseId,
                    CreatedByUserId = adminUser.UserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    PostedByUserId = adminUser.UserId,
                    PostedAt = DateTime.UtcNow.AddDays(-10),
                    Notes = $"Initial Stock Seeding for {wh.Name}"
                };

                foreach (var p in allProducts)
                {
                    var qty = rnd.Next(20, 150);
                    stockIn.Lines.Add(new StockMovementLine
                    {
                        ProductId = p.ProductId,
                        QtyDeltaOnHand = qty,
                        UnitCost = (decimal)(rnd.NextDouble() * 50 + 5)
                    });

                    context.StockLevels.Add(new StockLevel
                    {
                        WarehouseId = wh.WarehouseId,
                        ProductId = p.ProductId,
                        OnHandQty = qty,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                context.StockMovements.Add(stockIn);
            }
            await context.SaveChangesAsync();
        }
    }

    private static string ToHumanName(string input)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(input.Replace(".", " ").Replace("_", " "));
    }

    private static async Task AssignPermissionsToRole(InvDbContext context, Role role, List<Permission> perms, string scopeCode)
    {
        var existingPermIds = await context.RolePermissions
            .Where(rp => rp.RoleId == role.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var p in perms)
        {
            if (!existingPermIds.Contains(p.PermissionId))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = p.PermissionId,
                    GrantedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
