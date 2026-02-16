using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRefinedMovementTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_STOCK_MOVEMENT_ReasonCodeId",
                table: "STOCK_MOVEMENT",
                column: "ReasonCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_STOCK_MOVEMENT_INVENTORY_REASON_CODE_ReasonCodeId",
                table: "STOCK_MOVEMENT",
                column: "ReasonCodeId",
                principalTable: "INVENTORY_REASON_CODE",
                principalColumn: "ReasonCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_STOCK_MOVEMENT_INVENTORY_REASON_CODE_ReasonCodeId",
                table: "STOCK_MOVEMENT");

            migrationBuilder.DropIndex(
                name: "IX_STOCK_MOVEMENT_ReasonCodeId",
                table: "STOCK_MOVEMENT");
        }
    }
}
