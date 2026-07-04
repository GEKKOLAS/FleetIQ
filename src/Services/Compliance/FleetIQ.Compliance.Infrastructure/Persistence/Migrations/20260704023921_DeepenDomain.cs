using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Compliance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedOnUtc",
                schema: "compliance",
                table: "compliancechecks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                schema: "compliance",
                table: "compliancechecks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WaivedReason",
                schema: "compliance",
                table: "compliancechecks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "compliance_findings",
                schema: "compliance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RecordedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ComplianceCheckId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compliance_findings_compliancechecks_ComplianceCheckId",
                        column: x => x.ComplianceCheckId,
                        principalSchema: "compliance",
                        principalTable: "compliancechecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compliancechecks_Status",
                schema: "compliance",
                table: "compliancechecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_compliancechecks_SubjectId",
                schema: "compliance",
                table: "compliancechecks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_findings_ComplianceCheckId",
                schema: "compliance",
                table: "compliance_findings",
                column: "ComplianceCheckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_findings",
                schema: "compliance");

            migrationBuilder.DropIndex(
                name: "IX_compliancechecks_Status",
                schema: "compliance",
                table: "compliancechecks");

            migrationBuilder.DropIndex(
                name: "IX_compliancechecks_SubjectId",
                schema: "compliance",
                table: "compliancechecks");

            migrationBuilder.DropColumn(
                name: "CheckedOnUtc",
                schema: "compliance",
                table: "compliancechecks");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                schema: "compliance",
                table: "compliancechecks");

            migrationBuilder.DropColumn(
                name: "WaivedReason",
                schema: "compliance",
                table: "compliancechecks");
        }
    }
}
