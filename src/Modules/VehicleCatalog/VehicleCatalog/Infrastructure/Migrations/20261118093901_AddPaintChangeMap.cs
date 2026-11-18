using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaintChangeMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "value_part_map",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "paint_change_map",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "vehicle_catalog",
                table: "vehicle_field_settings",
                columns: new[] { "field_key", "form_state", "is_core", "public_visibility" },
                values: new object[] { "paint_change_map", 1, false, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "vehicle_catalog",
                table: "vehicle_field_settings",
                keyColumn: "field_key",
                keyValue: "paint_change_map");

            migrationBuilder.DropColumn(
                name: "value_part_map",
                schema: "vehicle_catalog",
                table: "vehicle_optional_field_values");

            migrationBuilder.DropColumn(
                name: "paint_change_map",
                schema: "vehicle_catalog",
                table: "showcase_listings");
        }
    }
}
