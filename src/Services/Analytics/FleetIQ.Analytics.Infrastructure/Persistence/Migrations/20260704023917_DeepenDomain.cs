using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Analytics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Query",
                schema: "analytics",
                table: "reportdefinitions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRunOnUtc",
                schema: "analytics",
                table: "reportdefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                schema: "analytics",
                table: "reportdefinitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "report_runs",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReportDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_runs_reportdefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalSchema: "analytics",
                        principalTable: "reportdefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reportdefinitions_Status",
                schema: "analytics",
                table: "reportdefinitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_report_runs_ReportDefinitionId",
                schema: "analytics",
                table: "report_runs",
                column: "ReportDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_runs",
                schema: "analytics");

            migrationBuilder.DropIndex(
                name: "IX_reportdefinitions_Status",
                schema: "analytics",
                table: "reportdefinitions");

            migrationBuilder.DropColumn(
                name: "LastRunOnUtc",
                schema: "analytics",
                table: "reportdefinitions");

            migrationBuilder.DropColumn(
                name: "Schedule",
                schema: "analytics",
                table: "reportdefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "Query",
                schema: "analytics",
                table: "reportdefinitions",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }
    }
}
