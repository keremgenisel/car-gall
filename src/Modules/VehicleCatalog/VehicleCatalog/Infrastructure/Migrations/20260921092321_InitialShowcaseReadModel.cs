using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialShowcaseReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vehicle_catalog");

            migrationBuilder.CreateTable(
                name: "showcase_listings",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_reserved = table.Column<bool>(type: "boolean", nullable: false),
                    showcase_sort_weight = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    body_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    vehicle_model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_year = table.Column<int>(type: "integer", nullable: false),
                    mileage_kilometers = table.Column<int>(type: "integer", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    price_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    price_minor_unit_exponent = table.Column<int>(type: "integer", nullable: true),
                    price_on_request = table.Column<bool>(type: "boolean", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_names = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    cover_media_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_listings", x => x.listing_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_showcase_listings_body_type",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                column: "body_type_slug");

            migrationBuilder.CreateIndex(
                name: "ix_showcase_listings_ordering",
                schema: "vehicle_catalog",
                table: "showcase_listings",
                columns: new[] { "showcase_sort_weight", "published_at", "listing_id" },
                descending: new[] { true, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "showcase_listings",
                schema: "vehicle_catalog");
        }
    }
}
