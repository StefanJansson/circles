using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Task7bc_EventLinks_TaskHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "discussions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToPersonId",
                table: "circles_tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTaskId",
                table: "circles_tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                table: "announcements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discussions_EventId",
                table: "discussions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_circles_tasks_AssignedToPersonId",
                table: "circles_tasks",
                column: "AssignedToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_circles_tasks_ParentTaskId",
                table: "circles_tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_event_id",
                table: "announcements",
                column: "event_id");

            migrationBuilder.AddForeignKey(
                name: "FK_announcements_events_event_id",
                table: "announcements",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_circles_tasks_circles_tasks_ParentTaskId",
                table: "circles_tasks",
                column: "ParentTaskId",
                principalTable: "circles_tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_circles_tasks_persons_AssignedToPersonId",
                table: "circles_tasks",
                column: "AssignedToPersonId",
                principalTable: "persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_discussions_events_EventId",
                table: "discussions",
                column: "EventId",
                principalTable: "events",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_announcements_events_event_id",
                table: "announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_circles_tasks_circles_tasks_ParentTaskId",
                table: "circles_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_circles_tasks_persons_AssignedToPersonId",
                table: "circles_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_discussions_events_EventId",
                table: "discussions");

            migrationBuilder.DropIndex(
                name: "IX_discussions_EventId",
                table: "discussions");

            migrationBuilder.DropIndex(
                name: "IX_circles_tasks_AssignedToPersonId",
                table: "circles_tasks");

            migrationBuilder.DropIndex(
                name: "IX_circles_tasks_ParentTaskId",
                table: "circles_tasks");

            migrationBuilder.DropIndex(
                name: "IX_announcements_event_id",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "discussions");

            migrationBuilder.DropColumn(
                name: "AssignedToPersonId",
                table: "circles_tasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "circles_tasks");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "announcements");
        }
    }
}
