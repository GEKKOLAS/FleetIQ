using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Carriers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichCarriers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedOnUtc",
                schema: "carriers",
                table: "carriers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedOnUtc",
                schema: "carriers",
                table: "carriers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "drivers",
                schema: "carriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LicenceNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_drivers_carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carriers",
                        principalTable: "carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "carriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CapacityKg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicles_carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carriers",
                        principalTable: "carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carriers_Status",
                schema: "carriers",
                table: "carriers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_drivers_CarrierId",
                schema: "carriers",
                table: "drivers",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_CarrierId",
                schema: "carriers",
                table: "vehicles",
                column: "CarrierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drivers",
                schema: "carriers");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "carriers");

            migrationBuilder.DropIndex(
                name: "IX_carriers_Status",
                schema: "carriers",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "SuspendedOnUtc",
                schema: "carriers",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "VerifiedOnUtc",
                schema: "carriers",
                table: "carriers");
        }
    }
}
