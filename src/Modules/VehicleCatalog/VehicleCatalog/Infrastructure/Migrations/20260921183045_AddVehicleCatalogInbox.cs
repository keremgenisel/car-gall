using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalogInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_entries",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handler_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_entries", x => new { x.event_id, x.handler_name });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_entries",
                schema: "vehicle_catalog");
        }
    }
}
