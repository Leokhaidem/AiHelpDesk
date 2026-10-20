using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentCategories",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCategories", x => new { x.AgentId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_AgentCategories_AspNetUsers_AgentId",
                        column: x => x.AgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    MaxOpenTickets = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AgentProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentApplicationCategories",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApplicationCategories", x => new { x.ApplicationId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_AgentApplicationCategories_AgentApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AgentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentApplicationCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplicationCategories_CategoryId",
                table: "AgentApplicationCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApplications_UserId",
                table: "AgentApplications",
                column: "UserId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCategories_CategoryId",
                table: "AgentCategories",
                column: "CategoryId");
                    // ── Data migration: carry existing agents/default-agent links into the new tables ──

            // 1) Every user currently in the Agent role gets a profile.
            migrationBuilder.Sql(@"
                INSERT INTO AgentProfiles (UserId, IsAvailable, MaxOpenTickets, CreatedAt)
                SELECT ur.UserId, 1, 10, SYSUTCDATETIME()
                FROM AspNetUserRoles ur
                JOIN AspNetRoles r ON r.Id = ur.RoleId
                WHERE r.Name = 'Agent';");

            // 2) Each category's old DefaultAgentId becomes an AgentCategory row
            //    (only when that user still has an agent profile).
            migrationBuilder.Sql(@"
                INSERT INTO AgentCategories (AgentId, CategoryId, CreatedAt)
                SELECT c.DefaultAgentId, c.Id, SYSUTCDATETIME()
                FROM Categories c
                WHERE c.DefaultAgentId IS NOT NULL
                  AND c.DefaultAgentId IN (SELECT UserId FROM AgentProfiles);");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentApplicationCategories");

            migrationBuilder.DropTable(
                name: "AgentCategories");

            migrationBuilder.DropTable(
                name: "AgentProfiles");

            migrationBuilder.DropTable(
                name: "AgentApplications");
        }
    }
}
