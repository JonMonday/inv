using InvServer.Core.Entities;
using InvServer.Core.Constants;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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
        // Inventory Request Types
        if (!await context.InventoryRequestTypes.AnyAsync())
        {
            context.InventoryRequestTypes.AddRange(
                new InventoryRequestType { Code = "STD", Name = "Standard Request", IsActive = true },
                new InventoryRequestType { Code = "URG", Name = "Urgent Request", IsActive = true },
                new InventoryRequestType { Code = "PROJ", Name = "Project Allocation", IsActive = true }
            );
        }

        // Security Event Types
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
        // Re-seed if new modes (ROLE) are missing. This handles migration from old ANY/ALL modes.
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

        // Workflow Step Types
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

        // Workflow Action Types
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

        // Inventory Movement Types
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

        // Inventory Movement Statuses
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

        // Access Scope Types
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

        var products = new List<Product>();
        if (!await context.Products.AnyAsync())
        {
            products.AddRange(new[]
            {
                new Product { SKU = "PPE-001", Name = "Safety Helmet (Yellow)", CategoryId = categories[0].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "PPE-002", Name = "High-Vis Vest (XL)", CategoryId = categories[0].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "IT-001", Name = "Dell Latitude 5420", CategoryId = categories[1].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "IT-002", Name = "Logitech MX Master 3", CategoryId = categories[1].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "NET-001", Name = "Cisco Catalyst 9200L", CategoryId = categories[2].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "OFF-001", Name = "A4 Paper Ream (80gsm)", CategoryId = categories[3].CategoryId, UnitOfMeasure = "REAM" },
                new Product { SKU = "OFF-002", Name = "Stapler (Heavy Duty)", CategoryId = categories[3].CategoryId, UnitOfMeasure = "PCS" },
                new Product { SKU = "CLN-001", Name = "Sanitizer 5L", CategoryId = categories[4].CategoryId, UnitOfMeasure = "BTL" }
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
            
            // Workflow Definitions
            "workflow_definition.read", "workflow_definition.create", "workflow_definition.update", "workflow_definition.deactivate",
            "workflow_definition_version.publish",
            "workflow_step.manage", "workflow_step_rule.manage", "workflow_transition.manage",
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

        // Reload perms to get IDs
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
        
        // Reload roles
        roles = await context.Roles.ToListAsync();

        // ==============================================================================
        // 6. Role Permissions Assignment
        // ==============================================================================
        // Helper to get Role by code
        Role GetRole(string code) => roles.First(r => r.Code == code);
        
        // SuperAdmin & Admin -> All Permissions
        await AssignPermissionsToRole(context, GetRole("SUPERADMIN"), allPerms, "GLOBAL");
        await AssignPermissionsToRole(context, GetRole("ADMIN"), allPerms, "GLOBAL");

        // Auditor -> Read Only
        var readPerms = allPerms.Where(p => p.Code.EndsWith(".read") || p.Code.StartsWith("audit_log")).ToList();
        await AssignPermissionsToRole(context, GetRole("AUDITOR"), readPerms, "GLOBAL");

        // Manager -> Requests, Teams, Reports
        var managerPerms = allPerms.Where(p => 
            p.Code.StartsWith("user.") || 
            p.Code.StartsWith("inventory_request.") || 
            p.Code.StartsWith("department.")).ToList();
        await AssignPermissionsToRole(context, GetRole("MANAGER"), managerPerms, "GLOBAL"); // Should be DEPT in real app

        // Storekeeper -> Stock, Reservations, Products, and view Requests
        var storePerms = allPerms.Where(p => 
            p.Code.StartsWith("stock_") || 
            p.Code.StartsWith("reservation.") ||
            p.Code.StartsWith("product.") ||
            p.Code.StartsWith("category.") ||
            p.Code == "inventory_request.read").ToList();
        await AssignPermissionsToRole(context, GetRole("STOREKEEPER"), storePerms, "GLOBAL"); 

        // User -> Request CRUD
        var userPerms = allPerms.Where(p => 
            p.Code == "inventory_request.read" || 
            p.Code == "inventory_request.create" || 
            p.Code == "inventory_request.update_draft").ToList();
        await AssignPermissionsToRole(context, GetRole("USER"), userPerms, "GLOBAL");


        // ==============================================================================
        // 7. Seed 300 Users
        // ==============================================================================
        if (await context.Users.CountAsync() < 10) 
        {
            var usersToSeed = new List<User>();
            var userRolesToSeed = new List<UserRole>();
            var userDeptsToSeed = new List<UserDepartment>();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            
            var realNames = new[] { 
                "Adeyemi Johnson", "Chidi Okafor", "Fatima Abubakar", "Kwame Nkrumah", "Zainab Mensah",
                "Oluwaseun Adewale", "Amadi Dike", "Bature Gambo", "Ebele Azikiwe", "Femi Kuti",
                "Gideon Okeke", "Habiba Sani", "Ibrahim Bio", "Jolomi Ayiri", "Kelechi Iheanacho",
                "Lola Shoneyin", "Musa Yaradua", "Nuhu Ribadu", "Omotola Jalade", "Patience Akpabio",
                "Quasim Alabi", "Rakiya Danjuma", "Sadiq Abubakar", "Tunde Bakare", "Uche Jombo",
                "Victoria Inyama", "Wale Adenuga", "Xaxa Bello", "Yemi Alade", "Zulum Kashim",
                "Adebayo Balogun", "Chinelo Okonkwo", "Damilola Adegbite", "Emeka Ike", "Folake Coker",
                "Gbenga Akinnagbe", "Hafsat Abiola", "Ify Oji", "Jim Iyke", "Kunle Afolayan",
                "Linda Ikeji", "Majid Michel", "Nadia Buari", "Omoni Oboli", "Phyno Nelson",
                "Rabbie Namaliu", "Simi Kosoko", "Tiwa Savage", "Ubi Franklin", "Vector Tha Viper",
                "Wizkid Balogun", "Ycee Oludemilade", "Zlatan Ibile", "Burnaboy Ogulu", "Davido Adeleke"
            };

            int nameIndex = 0;
            // Create a user for each Role in each Department
            foreach (var dept in depts)
            {
                foreach (var role in roles)
                {
                    var realName = realNames[nameIndex % realNames.Length];
                    var nameParts = realName.Split(' ');
                    var baseUsername = (nameParts[0].Substring(0, 1) + nameParts[1]).ToLower();
                    
                    // Specific usernames for first two users of first department (usually SU and Admin)
                    var username = (dept.Name == "IT" && role.Code == "SUPERADMIN") ? "superadmin" : 
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
                    
                    // We'll map these after saving users to get IDs
                    nameIndex++;
                }
            }
            
            context.Users.AddRange(usersToSeed);
            await context.SaveChangesAsync();

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
            
            context.UserRoles.AddRange(userRolesToSeed);
            context.UserDepartments.AddRange(userDeptsToSeed);
            await context.SaveChangesAsync();
        }

        // ==============================================================================
        // 8. Workflow Templates
        // ==============================================================================
        // ==============================================================================
        // 8. Workflow Templates
        // ==============================================================================
        // Helper to ensure workflow existence
        async Task<WorkflowDefinition> EnsureWorkflowDef(string code, string name)
        {
            var def = await context.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Code == code);
            if (def == null)
            {
                def = new WorkflowDefinition { Code = code, Name = name, IsActive = true, CreatedAt = DateTime.UtcNow };
                context.WorkflowDefinitions.Add(def);
                await context.SaveChangesAsync();
            }
            return def;
        }

        var assignmentModes = await context.WorkflowAssignmentModes.ToDictionaryAsync(m => m.Code, m => m.AssignmentModeId);
        var rolesByCode = await context.Roles.ToDictionaryAsync(r => r.Code, r => r.RoleId);
        var stepTypes = await context.WorkflowStepTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowStepTypeId);
        var actionTypes = await context.WorkflowActionTypes.ToDictionaryAsync(t => t.Code, t => t.WorkflowActionTypeId);

        // Define the templates we want
        var templates = new[]
        {
            new { Code = "SIMPLE_APPROVE", Name = "Simple Approval Flow", Role = "MANAGER" },
            new { Code = "FINANCE_FLOW", Name = "Finance Approval Flow", Role = "ADMIN" },
            new { Code = "STOCK_FLOW", Name = "Stock fulfillment Flow", Role = "STOREKEEPER" }
        };

        foreach (var tpl in templates)
        {
            var def = await EnsureWorkflowDef(tpl.Code, tpl.Name);

            // Ensure v1 exists
            if (!await context.WorkflowDefinitionVersions.AnyAsync(v => v.WorkflowDefinitionId == def.WorkflowDefinitionId))
            {
                var v1 = new WorkflowDefinitionVersion
                {
                    WorkflowDefinitionId = def.WorkflowDefinitionId,
                    VersionNo = 1,
                    IsActive = true,
                    PublishedAt = DateTime.UtcNow,
                    DefinitionJson = "{}"
                };
                context.WorkflowDefinitionVersions.Add(v1);
                await context.SaveChangesAsync();

                // Create Default Steps: Submission -> Fulfillment -> Confirmation -> End
                var sStart = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "START", Name = "Submission", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Start], SequenceNo = 0, IsSystemRequired = true };
                var sFulfill = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "FULFILL", Name = "Fulfillment", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Fulfillment], SequenceNo = 1, IsSystemRequired = true };
                var sConfirm = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "CONFIRM", Name = "Confirmation", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Review], SequenceNo = 2, IsSystemRequired = true };
                var sEnd = new WorkflowStep { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, StepKey = "END", Name = "End", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.End], SequenceNo = 3, IsSystemRequired = true };
                
                context.WorkflowSteps.AddRange(sStart, sFulfill, sConfirm, sEnd);
                await context.SaveChangesAsync();

                // Rules
                // Start: Requester
                context.WorkflowStepRules.Add(new WorkflowStepRule 
                { 
                    WorkflowStepId = sStart.WorkflowStepId, 
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Requestor] 
                });
                
                // Fulfillment: Specific Role (Manager/Finance/Storekeeper)
                context.WorkflowStepRules.Add(new WorkflowStepRule 
                { 
                    WorkflowStepId = sFulfill.WorkflowStepId, 
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Role], 
                    RoleId = rolesByCode.ContainsKey(tpl.Role) ? rolesByCode[tpl.Role] : rolesByCode["MANAGER"] 
                });
                
                // Confirmation: Requester
                context.WorkflowStepRules.Add(new WorkflowStepRule 
                { 
                    WorkflowStepId = sConfirm.WorkflowStepId, 
                    AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Requestor] 
                });
                
                await context.SaveChangesAsync();

                // Transitions
                // Start -> Fulfill (Submit)
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sStart.WorkflowStepId, ToWorkflowStepId = sFulfill.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Submit] });
                
                // Fulfill -> Confirm (Complete)
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sFulfill.WorkflowStepId, ToWorkflowStepId = sConfirm.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Complete] });
                
                // Confirm -> End (Approve)
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sConfirm.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Approve] });

                // Cancellations
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sStart.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Cancel] });
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sFulfill.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Cancel] });
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v1.WorkflowDefinitionVersionId, FromWorkflowStepId = sConfirm.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Cancel] });
                
                await context.SaveChangesAsync();
            }

            // Seed v2 for SIMPLE_APPROVE only, to show multi-version support
            if (tpl.Code == "SIMPLE_APPROVE" && !await context.WorkflowDefinitionVersions.AnyAsync(v => v.WorkflowDefinitionId == def.WorkflowDefinitionId && v.VersionNo == 2))
            {
                // Deactivate v1
                var v1 = await context.WorkflowDefinitionVersions.FirstOrDefaultAsync(v => v.WorkflowDefinitionId == def.WorkflowDefinitionId && v.VersionNo == 1);
                if (v1 != null) { v1.IsActive = false; }

                var v2 = new WorkflowDefinitionVersion
                {
                    WorkflowDefinitionId = def.WorkflowDefinitionId,
                    VersionNo = 2,
                    IsActive = true,
                    PublishedAt = DateTime.UtcNow,
                    DefinitionJson = "{}"
                };
                context.WorkflowDefinitionVersions.Add(v2);
                await context.SaveChangesAsync();

                // Clone logic for v2 (simplified here, just recreate similar steps)
                var sStart = new WorkflowStep { WorkflowDefinitionVersionId = v2.WorkflowDefinitionVersionId, StepKey = "START", Name = "Submission v2", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.Start], SequenceNo = 0, IsSystemRequired = true };
                var sEnd = new WorkflowStep { WorkflowDefinitionVersionId = v2.WorkflowDefinitionVersionId, StepKey = "END", Name = "End", WorkflowStepTypeId = stepTypes[WorkflowStepTypeCodes.End], SequenceNo = 1, IsSystemRequired = true };
                context.WorkflowSteps.AddRange(sStart, sEnd);
                await context.SaveChangesAsync();
                
                context.WorkflowStepRules.Add(new WorkflowStepRule { WorkflowStepId = sStart.WorkflowStepId, AssignmentModeId = assignmentModes[WorkflowAssignmentModeCodes.Requestor] });
                context.WorkflowTransitions.Add(new WorkflowTransition { WorkflowDefinitionVersionId = v2.WorkflowDefinitionVersionId, FromWorkflowStepId = sStart.WorkflowStepId, ToWorkflowStepId = sEnd.WorkflowStepId, WorkflowActionTypeId = actionTypes[WorkflowActionCodes.Submit] });
                
                await context.SaveChangesAsync();
            }
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
                    // Randomly decide to add stock (ensure at least 2 warehouses per product if possible)
                    // Or just add stock for all products in all warehouses for simplicity in demo
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
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.Replace(".", " ").Replace("_", " "));
    }

    private static async Task AssignPermissionsToRole(InvDbContext context, Role role, List<Permission> perms, string scopeCode)
    {
        // Simple distinct assignment
        var existingParams = await context.RolePermissions.Where(rp => rp.RoleId == role.RoleId).Select(rp => rp.PermissionId).ToListAsync();
        var scopeType = await context.AccessScopeTypes.FirstOrDefaultAsync(s => s.Code == scopeCode);

        foreach (var p in perms)
        {
            if (!existingParams.Contains(p.PermissionId))
            {
                var rp = new RolePermission { RoleId = role.RoleId, PermissionId = p.PermissionId, GrantedAt = DateTime.UtcNow };
                context.RolePermissions.Add(rp);
                
                // If we had scope logic
                if (scopeType != null)
                {
                     // In a real app we'd add the scope relationship here
                     // But for now we just seed the link
                }
            }
        }
        await context.SaveChangesAsync();
    }
}
