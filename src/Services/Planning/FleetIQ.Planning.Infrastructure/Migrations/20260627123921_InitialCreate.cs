using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Planning.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "planning");

            migrationBuilder.CreateTable(
                name: "loads",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OriginCity = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    DestinationCity = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    TotalWeightKg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "load_orders",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LoadId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_load_orders_loads_LoadId",
                        column: x => x.LoadId,
                        principalSchema: "planning",
                        principalTable: "loads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                schema: "planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_route_stops_routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "planning",
                        principalTable: "routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_load_orders_LoadId",
                schema: "planning",
                table: "load_orders",
                column: "LoadId");

            migrationBuilder.CreateIndex(
                name: "IX_loads_Reference",
                schema: "planning",
                table: "loads",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loads_Status",
                schema: "planning",
                table: "loads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "planning",
                table: "outbox_messages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_RouteId",
                schema: "planning",
                table: "route_stops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_routes_LoadId",
                schema: "planning",
                table: "routes",
                column: "LoadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "load_orders",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "route_stops",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "loads",
                schema: "planning");

            migrationBuilder.DropTable(
                name: "routes",
                schema: "planning");
        }
    }
}
