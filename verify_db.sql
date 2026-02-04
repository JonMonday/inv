USE InventoryDB;
SET NOCOUNT ON;
PRINT '--- DB VERIFICATION REPORT ---';
SELECT 'Roles' as Entity, Count(*) as Qty FROM dbo.ROLE;
SELECT 'Permissions' as Entity, Count(*) as Qty FROM dbo.PERMISSION;
SELECT 'Workflows' as Entity, Count(*) as Qty FROM dbo.WORKFLOW_DEFINITION_VERSION WHERE IsActive = 1;
SELECT 'Requests' as Entity, Count(*) as Qty FROM dbo.INVENTORY_REQUEST;
SELECT 'Warehouses' as Entity, Count(*) as Qty FROM dbo.WAREHOUSE;
SELECT 'Products' as Entity, Count(*) as Qty FROM dbo.PRODUCT;
GO
