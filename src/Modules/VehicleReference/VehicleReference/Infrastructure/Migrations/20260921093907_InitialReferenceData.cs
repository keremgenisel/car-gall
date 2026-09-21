using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarGallery.Modules.VehicleReference.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vehicle_reference");

            migrationBuilder.CreateTable(
                name: "body_types",
                schema: "vehicle_reference",
                columns: table => new
                {
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_types", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                schema: "vehicle_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "body_type_translations",
                schema: "vehicle_reference",
                columns: table => new
                {
                    body_type_slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    locale = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_type_translations", x => new { x.body_type_slug, x.locale });
                    table.ForeignKey(
                        name: "FK_body_type_translations_body_types_body_type_slug",
                        column: x => x.body_type_slug,
                        principalSchema: "vehicle_reference",
                        principalTable: "body_types",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_model_translations",
                schema: "vehicle_reference",
                columns: table => new
                {
                    vehicle_model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_model_translations", x => new { x.vehicle_model_id, x.locale });
                    table.ForeignKey(
                        name: "FK_vehicle_model_translations_vehicle_models_vehicle_model_id",
                        column: x => x.vehicle_model_id,
                        principalSchema: "vehicle_reference",
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "vehicle_reference",
                table: "body_types",
                columns: new[] { "slug", "canonical_name" },
                values: new object[,]
                {
                    { "coupe", "Coupe" },
                    { "hatchback", "Hatchback" },
                    { "pickup", "Pickup" },
                    { "sedan", "Sedan" },
                    { "station-wagon", "Station Wagon" },
                    { "suv", "SUV" }
                });

            migrationBuilder.InsertData(
                schema: "vehicle_reference",
                table: "body_type_translations",
                columns: new[] { "body_type_slug", "locale", "name" },
                values: new object[,]
                {
                    { "coupe", "ar", "كوبيه" },
                    { "coupe", "en", "Coupe" },
                    { "coupe", "tr", "Coupe" },
                    { "hatchback", "ar", "هاتشباك" },
                    { "hatchback", "en", "Hatchback" },
                    { "hatchback", "tr", "Hatchback" },
                    { "pickup", "ar", "بيك أب" },
                    { "pickup", "en", "Pickup" },
                    { "pickup", "tr", "Pikap" },
                    { "sedan", "ar", "سيدان" },
                    { "sedan", "en", "Sedan" },
                    { "sedan", "tr", "Sedan" },
                    { "station-wagon", "ar", "ستيشن" },
                    { "station-wagon", "en", "Station Wagon" },
                    { "station-wagon", "tr", "Station Wagon" },
                    { "suv", "ar", "دفع رباعي" },
                    { "suv", "en", "SUV" },
                    { "suv", "tr", "SUV" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_type_translations",
                schema: "vehicle_reference");

            migrationBuilder.DropTable(
                name: "vehicle_model_translations",
                schema: "vehicle_reference");

            migrationBuilder.DropTable(
                name: "body_types",
                schema: "vehicle_reference");

            migrationBuilder.DropTable(
                name: "vehicle_models",
                schema: "vehicle_reference");
        }
    }
}
