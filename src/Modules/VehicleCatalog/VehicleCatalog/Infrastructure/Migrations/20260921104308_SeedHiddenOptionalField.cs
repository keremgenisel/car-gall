using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedHiddenOptionalField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "vehicle_catalog",
                table: "vehicle_field_settings",
                columns: new[] { "field_key", "form_state", "is_core", "public_visibility" },
                values: new object[] { "damage_record_amount", 1, false, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "vehicle_catalog",
                table: "vehicle_field_settings",
                keyColumn: "field_key",
                keyValue: "damage_record_amount");
        }
    }
}
