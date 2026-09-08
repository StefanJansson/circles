using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteAdminFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSiteAdmin",
                table: "user_accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSiteAdmin",
                table: "user_accounts");
        }
    }
}
