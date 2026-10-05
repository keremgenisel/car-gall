using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleWriteModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "listings",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    showcase_sort_weight = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color_names = table.Column<string>(type: "jsonb", nullable: false),
                    model_year = table.Column<int>(type: "integer", nullable: false),
                    vin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    mileage_kilometers = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    awaiting_branch_assignment = table.Column<bool>(type: "boolean", nullable: false),
                    media_asset_ids = table.Column<string>(type: "jsonb", nullable: false),
                    body_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    drivetrain_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fuel_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    transmission_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    trim_level_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_changes",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_changes", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_changes_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicle_catalog",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_optional_field_values",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    field_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_optional_field_values", x => new { x.vehicle_id, x.field_key });
                    table.ForeignKey(
                        name: "FK_vehicle_optional_field_values_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicle_catalog",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_listings_vehicle_id",
                schema: "vehicle_catalog",
                table: "listings",
                column: "vehicle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_changes_vehicle_id",
                schema: "vehicle_catalog",
                table: "price_changes",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listings",
                schema: "vehicle_catalog");

            migrationBuilder.DropTable(
                name: "price_changes",
                schema: "vehicle_catalog");

            migrationBuilder.DropTable(
                name: "vehicle_optional_field_values",
                schema: "vehicle_catalog");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "vehicle_catalog");
        }
    }
}
