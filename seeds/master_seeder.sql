/* ============================================================
   MASTER SEEDER ORCHESTRATOR
   Order:
   1. Static Seeder (Lookups, Roles, Permissions)
   2. Template Seeder (Workflow Definitions)
   3. Realworld Seeder (Sample Users, Data, Inventory)
   ============================================================ */

:r ./mnt/data/static-seeder.sql
GO

:r ./mnt/data/template-seeder.sql
GO

:r ./mnt/data/realworld-seeder.sql
GO
