using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCalendarSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LagetSeCalendarUrl",
                table: "circles");

            migrationBuilder.CreateTable(
                name: "calendar_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    circle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_calendar_subscriptions_circles_circle_id",
                        column: x => x.circle_id,
                        principalTable: "circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calendar_subscriptions_circle_id",
                table: "calendar_subscriptions",
                column: "circle_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_subscriptions_circle_id_is_active",
                table: "calendar_subscriptions",
                columns: new[] { "circle_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "LagetSeCalendarUrl",
                table: "circles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
