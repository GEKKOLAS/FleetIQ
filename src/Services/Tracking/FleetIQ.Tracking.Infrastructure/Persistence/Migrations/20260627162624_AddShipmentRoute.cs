using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetIQ.Tracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationCity",
                schema: "tracking",
                table: "shipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DestinationLatitude",
                schema: "tracking",
                table: "shipments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLongitude",
                schema: "tracking",
                table: "shipments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OriginCity",
                schema: "tracking",
                table: "shipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "OriginLatitude",
                schema: "tracking",
                table: "shipments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OriginLongitude",
                schema: "tracking",
                table: "shipments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationCity",
                schema: "tracking",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "DestinationLatitude",
                schema: "tracking",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "DestinationLongitude",
                schema: "tracking",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "OriginCity",
                schema: "tracking",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "OriginLatitude",
                schema: "tracking",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "OriginLongitude",
                schema: "tracking",
                table: "shipments");
        }
    }
}
