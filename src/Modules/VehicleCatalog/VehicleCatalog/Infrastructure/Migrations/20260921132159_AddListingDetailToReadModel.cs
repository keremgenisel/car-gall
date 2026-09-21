using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingDetailToReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_names",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "drivetrain_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "fuel_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "transmission_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "trim_level_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "showcase_listing_optional_values",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    values = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_listing_optional_values", x => new { x.listing_id, x.field_key });
                    table.ForeignKey(
                        name: "FK_showcase_listing_optional_values_showcase_listings_listing_~",
                        column: x => x.listing_id,
                        principalSchema: "vehicle_catalog",
                        principalTable: "showcase_listings",
                        principalColumn: "listing_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_showcase_listings_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "showcase_listing_optional_values",
                schema: "vehicle_catalog");

            migrationBuilder.DropIndex(
                name: "ux_showcase_listings_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "color_names",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "drivetrain_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "fuel_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "transmission_type_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");

            migrationBuilder.DropColumn(
                name: "trim_level_slug",
                schema: "vehicle_catalog",
                table: "showcase_listings");
        }
    }
}
