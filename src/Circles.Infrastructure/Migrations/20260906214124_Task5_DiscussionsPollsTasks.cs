using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task5_DiscussionsPollsTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "circles_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CircleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circles_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_circles_tasks_circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_circles_tasks_persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discussions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CircleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalPosterPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discussions_circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discussions_persons_OriginalPosterPersonId",
                        column: x => x.OriginalPosterPersonId,
                        principalTable: "persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CircleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_polls_circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_posts_discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "discussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_posts_persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_options_polls_PollId",
                        column: x => x.PollId,
                        principalTable: "polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PollOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_votes_persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_votes_poll_options_PollOptionId",
                        column: x => x.PollOptionId,
                        principalTable: "poll_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_circles_tasks_CircleId",
                table: "circles_tasks",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_circles_tasks_CircleId_CompletedAt",
                table: "circles_tasks",
                columns: new[] { "CircleId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_circles_tasks_CreatedByPersonId",
                table: "circles_tasks",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_discussions_CircleId",
                table: "discussions",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_discussions_OriginalPosterPersonId",
                table: "discussions",
                column: "OriginalPosterPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_options_PollId_Order",
                table: "poll_options",
                columns: new[] { "PollId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_polls_CircleId",
                table: "polls",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_DiscussionId_CreatedAt",
                table: "posts",
                columns: new[] { "DiscussionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_PersonId",
                table: "posts",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_votes_PersonId",
                table: "votes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_votes_PollOptionId_PersonId",
                table: "votes",
                columns: new[] { "PollOptionId", "PersonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "circles_tasks");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "discussions");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "polls");
        }
    }
}
