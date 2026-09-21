using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Dealership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDealership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dealership");

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "dealership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    names = table.Column<string>(type: "jsonb", nullable: false),
                    addresses = table.Column<string>(type: "jsonb", nullable: false),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    media_asset_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "galleries",
                schema: "dealership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logo_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    trade_registry_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_galleries", x => x.id);
                    table.CheckConstraint("ck_galleries_singleton", "id = '00000000-0000-7000-8000-00000000da01'");
                });

            migrationBuilder.CreateTable(
                name: "gallery_social_links",
                schema: "dealership",
                columns: table => new
                {
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    gallery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gallery_social_links", x => new { x.gallery_id, x.platform });
                    table.ForeignKey(
                        name: "FK_gallery_social_links_galleries_gallery_id",
                        column: x => x.gallery_id,
                        principalSchema: "dealership",
                        principalTable: "galleries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_branches_slug",
                schema: "dealership",
                table: "branches",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches",
                schema: "dealership");

            migrationBuilder.DropTable(
                name: "gallery_social_links",
                schema: "dealership");

            migrationBuilder.DropTable(
                name: "galleries",
                schema: "dealership");
        }
    }
}
