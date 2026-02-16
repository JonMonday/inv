using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureTransferType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert missing types if they don't exist
            migrationBuilder.Sql(@"
                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'REFILL', 'Refill (Stock In)'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'REFILL');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'RETURN', 'Return (Stock In)'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'RETURN');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'TRANSFER', 'Transfer (Cross-Warehouse)'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'TRANSFER');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'ADJUSTMENT', 'Inventory Adjustment'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'ADJUSTMENT');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
