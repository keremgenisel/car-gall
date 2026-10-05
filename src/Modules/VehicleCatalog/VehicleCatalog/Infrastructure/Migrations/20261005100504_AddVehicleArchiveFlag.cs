using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleArchiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                schema: "vehicle_catalog",
                table: "vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ux_vehicles_vin",
                schema: "vehicle_catalog",
                table: "vehicles",
                column: "vin",
                unique: true,
                filter: "NOT is_archived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_vehicles_vin",
                schema: "vehicle_catalog",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "is_archived",
                schema: "vehicle_catalog",
                table: "vehicles");
        }
    }
}
