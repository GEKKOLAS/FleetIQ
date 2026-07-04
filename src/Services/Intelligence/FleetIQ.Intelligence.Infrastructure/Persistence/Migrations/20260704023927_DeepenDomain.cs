using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Intelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PredictionType",
                schema: "intelligence",
                table: "predictionrequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                schema: "intelligence",
                table: "predictionrequests",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                schema: "intelligence",
                table: "predictionrequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedOnUtc",
                schema: "intelligence",
                table: "predictionrequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultJson",
                schema: "intelligence",
                table: "predictionrequests",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedOnUtc",
                schema: "intelligence",
                table: "predictionrequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_predictionrequests_Status",
                schema: "intelligence",
                table: "predictionrequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_predictionrequests_Status",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.DropColumn(
                name: "Confidence",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.DropColumn(
                name: "Error",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.DropColumn(
                name: "FinishedOnUtc",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.DropColumn(
                name: "StartedOnUtc",
                schema: "intelligence",
                table: "predictionrequests");

            migrationBuilder.AlterColumn<string>(
                name: "PredictionType",
                schema: "intelligence",
                table: "predictionrequests",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
