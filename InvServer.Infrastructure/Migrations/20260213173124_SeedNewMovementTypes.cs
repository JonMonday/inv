using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNewMovementTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed Movement Types
            migrationBuilder.Sql(@"
                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'REFILL', 'Refill'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'REFILL');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'RETURN', 'Return'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'RETURN');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'TRANSFER', 'Transfer'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'TRANSFER');

                INSERT INTO ""INVENTORY_MOVEMENT_TYPE"" (""Code"", ""Name"")
                SELECT 'ADJUSTMENT', 'Adjustment'
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_MOVEMENT_TYPE"" WHERE ""Code"" = 'ADJUSTMENT');
            ");

            // Seed Reason Codes
            migrationBuilder.Sql(@"
                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'THRIFT', 'Thrift', false, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'THRIFT');

                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'MISCALCULATION', 'Miscalculation', true, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'MISCALCULATION');

                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'DAMAGE', 'Damage', true, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'DAMAGE');

                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'EXPIRED', 'Expired', true, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'EXPIRED');

                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'OTHER', 'Other', false, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'OTHER');

                INSERT INTO ""INVENTORY_REASON_CODE"" (""Code"", ""Name"", ""RequiresApproval"", ""IsActive"")
                SELECT 'CORRECTION', 'Correction', true, true
                WHERE NOT EXISTS (SELECT 1 FROM ""INVENTORY_REASON_CODE"" WHERE ""Code"" = 'CORRECTION');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"INVENTORY_MOVEMENT_TYPE\" WHERE \"Code\" IN ('REFILL', 'RETURN', 'TRANSFER', 'ADJUSTMENT');");
            migrationBuilder.Sql("DELETE FROM \"INVENTORY_REASON_CODE\" WHERE \"Code\" IN ('THRIFT', 'MISCALCULATION', 'DAMAGE', 'EXPIRED', 'OTHER', 'CORRECTION');");
        }
    }
}
