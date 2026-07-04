using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Documents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeepenDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresOnUtc",
                schema: "documents",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedReason",
                schema: "documents",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedOnUtc",
                schema: "documents",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_versions",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    StorageRef = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    UploadedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_OwnerId",
                schema: "documents",
                table: "documents",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Status",
                schema: "documents",
                table: "documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_DocumentId",
                schema: "documents",
                table: "document_versions",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_versions",
                schema: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_OwnerId",
                schema: "documents",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_Status",
                schema: "documents",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ExpiresOnUtc",
                schema: "documents",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "RejectedReason",
                schema: "documents",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "VerifiedOnUtc",
                schema: "documents",
                table: "documents");
        }
    }
}
