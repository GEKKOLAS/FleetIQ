using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Integration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                schema: "integration",
                table: "integrationconnectors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DisabledReason",
                schema: "integration",
                table: "integrationconnectors",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncOnUtc",
                schema: "integration",
                table: "integrationconnectors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_integrationconnectors_Status",
                schema: "integration",
                table: "integrationconnectors",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_integrationconnectors_Status",
                schema: "integration",
                table: "integrationconnectors");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                schema: "integration",
                table: "integrationconnectors");

            migrationBuilder.DropColumn(
                name: "DisabledReason",
                schema: "integration",
                table: "integrationconnectors");

            migrationBuilder.DropColumn(
                name: "LastSyncOnUtc",
                schema: "integration",
                table: "integrationconnectors");
        }
    }
}
