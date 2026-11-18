using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status_before_sale",
                schema: "vehicle_catalog",
                table: "listings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "listing_sale_reversals",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    restored_status = table.Column<int>(type: "integer", nullable: false),
                    reverted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_sale_reversals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_listing_sale_reversals_vehicle_id",
                schema: "vehicle_catalog",
                table: "listing_sale_reversals",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_sale_reversals",
                schema: "vehicle_catalog");

            migrationBuilder.DropColumn(
                name: "status_before_sale",
                schema: "vehicle_catalog",
                table: "listings");
        }
    }
}
