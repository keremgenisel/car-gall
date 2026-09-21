using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleFieldSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_field_settings",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    field_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    form_state = table.Column<int>(type: "integer", nullable: false),
                    public_visibility = table.Column<int>(type: "integer", nullable: false),
                    is_core = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_field_settings", x => x.field_key);
                });

            migrationBuilder.InsertData(
                schema: "vehicle_catalog",
                table: "vehicle_field_settings",
                columns: new[] { "field_key", "form_state", "is_core", "public_visibility" },
                values: new object[] { "body_type", 2, true, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_field_settings",
                schema: "vehicle_catalog");
        }
    }
}
