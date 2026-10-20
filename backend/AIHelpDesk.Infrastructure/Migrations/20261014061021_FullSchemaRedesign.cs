using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FullSchemaRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent drops — safe to re-run if the migration was partially applied.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[dbo].[Tickets]', 'Category') IS NOT NULL
                BEGIN
                    DECLARE @con1 nvarchar(max);
                    SELECT @con1 = QUOTENAME(d.name)
                    FROM sys.default_constraints d
                    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'[Tickets]') AND c.name = N'Category';
                    IF @con1 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @con1);
                    ALTER TABLE [Tickets] DROP COLUMN [Category];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('[dbo].[Tickets]', 'Priority') IS NOT NULL
                BEGIN
                    DECLARE @con2 nvarchar(max);
                    SELECT @con2 = QUOTENAME(d.name)
                    FROM sys.default_constraints d
                    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'[Tickets]') AND c.name = N'Priority';
                    IF @con2 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @con2);
                    ALTER TABLE [Tickets] DROP COLUMN [Priority];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('[dbo].[Tickets]', 'SuggestedResolution') IS NOT NULL
                BEGIN
                    DECLARE @con3 nvarchar(max);
                    SELECT @con3 = QUOTENAME(d.name)
                    FROM sys.default_constraints d
                    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'[Tickets]') AND c.name = N'SuggestedResolution';
                    IF @con3 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @con3);
                    ALTER TABLE [Tickets] DROP COLUMN [SuggestedResolution];
                END
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('[dbo].[Tickets]', 'Summary') IS NOT NULL
                BEGIN
                    DECLARE @con4 nvarchar(max);
                    SELECT @con4 = QUOTENAME(d.name)
                    FROM sys.default_constraints d
                    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'[Tickets]') AND c.name = N'Summary';
                    IF @con4 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @con4);
                    ALTER TABLE [Tickets] DROP COLUMN [Summary];
                END
            ");

            // Only convert Status if it is still nvarchar (idempotent on re-run).
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Tickets]')
                      AND name = N'Status'
                      AND system_type_id = TYPE_ID(N'nvarchar')
                )
                BEGIN
                    -- Map string values to their int enum equivalents
                    UPDATE [Tickets] SET [Status] = CASE [Status]
                        WHEN 'Created'            THEN '0'
                        WHEN 'AnalysisInProgress' THEN '1'
                        WHEN 'Open'               THEN '2'
                        WHEN 'Assigned'           THEN '2'
                        WHEN 'InProgress'         THEN '3'
                        WHEN 'Resolved'           THEN '4'
                        WHEN 'Closed'             THEN '5'
                        WHEN 'AnalysisFailed'     THEN '6'
                        ELSE '0'
                    END;

                    -- Drop any default constraint on Status before altering
                    DECLARE @con5 nvarchar(max);
                    SELECT @con5 = QUOTENAME(d.name)
                    FROM sys.default_constraints d
                    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'[Tickets]') AND c.name = N'Status';
                    IF @con5 IS NOT NULL EXEC(N'ALTER TABLE [Tickets] DROP CONSTRAINT ' + @con5);

                    ALTER TABLE [Tickets] ALTER COLUMN [Status] int NOT NULL;
                END
            ");

            migrationBuilder.AddColumn<string>(
                name: "AiSuggestedResolution",
                table: "Tickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Tickets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Priorities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Priorities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultAgentId",
                table: "Categories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploaderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_AspNetUsers_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attachments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategoryId",
                table: "Tickets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PriorityId",
                table: "Tickets",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Priorities_Level",
                table: "Priorities",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Priorities_Name",
                table: "Priorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_DefaultAgentId",
                table: "Categories",
                column: "DefaultAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TicketId",
                table: "Attachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UploaderId",
                table: "Attachments",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketId",
                table: "Comments",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_DefaultAgentId",
                table: "Categories",
                column: "DefaultAgentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Categories_CategoryId",
                table: "Tickets",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_DefaultAgentId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Categories_CategoryId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategoryId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PriorityId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Priorities_Level",
                table: "Priorities");

            migrationBuilder.DropIndex(
                name: "IX_Priorities_Name",
                table: "Priorities");

            migrationBuilder.DropIndex(
                name: "IX_Categories_DefaultAgentId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AiSuggestedResolution",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Priorities");

            migrationBuilder.DropColumn(
                name: "DefaultAgentId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tickets",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuggestedResolution",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Priorities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
