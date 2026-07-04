using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                schema: "notifications",
                table: "notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "notifications",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailedReason",
                schema: "notifications",
                table: "notifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptOnUtc",
                schema: "notifications",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentOnUtc",
                schema: "notifications",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Status",
                schema: "notifications",
                table: "notifications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_Status",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "FailedReason",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "LastAttemptOnUtc",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "SentOnUtc",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                schema: "notifications",
                table: "notifications",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
