using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShowcaseListingMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "showcase_listing_media",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    variants = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_showcase_listing_media", x => new { x.listing_id, x.position });
                    table.ForeignKey(
                        name: "FK_showcase_listing_media_showcase_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "vehicle_catalog",
                        principalTable: "showcase_listings",
                        principalColumn: "listing_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_showcase_listing_media_asset",
                schema: "vehicle_catalog",
                table: "showcase_listing_media",
                column: "media_asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "showcase_listing_media",
                schema: "vehicle_catalog");
        }
    }
}
