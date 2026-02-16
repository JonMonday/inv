using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionModeAndDeptToWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepartmentId",
                table: "WORKFLOW_TEMPLATE",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RejectionModeId",
                table: "WORKFLOW_TEMPLATE",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WORKFLOW_REJECTION_MODE",
                columns: table => new
                {
                    RejectionModeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_REJECTION_MODE", x => x.RejectionModeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TEMPLATE_DepartmentId",
                table: "WORKFLOW_TEMPLATE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TEMPLATE_RejectionModeId",
                table: "WORKFLOW_TEMPLATE",
                column: "RejectionModeId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_REJECTION_MODE_Code",
                table: "WORKFLOW_REJECTION_MODE",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WORKFLOW_TEMPLATE_DEPARTMENT_DepartmentId",
                table: "WORKFLOW_TEMPLATE",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_WORKFLOW_TEMPLATE_WORKFLOW_REJECTION_MODE_RejectionModeId",
                table: "WORKFLOW_TEMPLATE",
                column: "RejectionModeId",
                principalTable: "WORKFLOW_REJECTION_MODE",
                principalColumn: "RejectionModeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WORKFLOW_TEMPLATE_DEPARTMENT_DepartmentId",
                table: "WORKFLOW_TEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_WORKFLOW_TEMPLATE_WORKFLOW_REJECTION_MODE_RejectionModeId",
                table: "WORKFLOW_TEMPLATE");

            migrationBuilder.DropTable(
                name: "WORKFLOW_REJECTION_MODE");

            migrationBuilder.DropIndex(
                name: "IX_WORKFLOW_TEMPLATE_DepartmentId",
                table: "WORKFLOW_TEMPLATE");

            migrationBuilder.DropIndex(
                name: "IX_WORKFLOW_TEMPLATE_RejectionModeId",
                table: "WORKFLOW_TEMPLATE");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "WORKFLOW_TEMPLATE");

            migrationBuilder.DropColumn(
                name: "RejectionModeId",
                table: "WORKFLOW_TEMPLATE");
        }
    }
}
