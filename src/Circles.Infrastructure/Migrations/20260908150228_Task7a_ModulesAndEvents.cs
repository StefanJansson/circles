using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task7a_ModulesAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LagetSeCalendarUrl",
                table: "circles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    circle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    starts_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ends_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    location = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    external_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    external_source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_events_circles_circle_id",
                        column: x => x.circle_id,
                        principalTable: "circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organization_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_modules", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_modules_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_circle_id",
                table: "events",
                column: "circle_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_circle_id_external_id",
                table: "events",
                columns: new[] { "circle_id", "external_id" });

            migrationBuilder.CreateIndex(
                name: "IX_events_starts_at",
                table: "events",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "IX_organization_modules_organization_id_module",
                table: "organization_modules",
                columns: new[] { "organization_id", "module" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "organization_modules");

            migrationBuilder.DropColumn(
                name: "LagetSeCalendarUrl",
                table: "circles");
        }
    }
}
