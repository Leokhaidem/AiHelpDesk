using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCategoryDefaultAgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_DefaultAgentId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_DefaultAgentId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DefaultAgentId",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultAgentId",
                table: "Categories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DefaultAgentId",
                table: "Categories",
                column: "DefaultAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_DefaultAgentId",
                table: "Categories",
                column: "DefaultAgentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
