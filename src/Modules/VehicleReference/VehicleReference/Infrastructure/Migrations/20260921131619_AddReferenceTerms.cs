using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarGallery.Modules.VehicleReference.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reference_terms",
                schema: "vehicle_reference",
                columns: table => new
                {
                    kind = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_terms", x => new { x.kind, x.slug });
                });

            migrationBuilder.CreateTable(
                name: "reference_term_translations",
                schema: "vehicle_reference",
                columns: table => new
                {
                    kind = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    locale = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_term_translations", x => new { x.kind, x.slug, x.locale });
                    table.ForeignKey(
                        name: "FK_reference_term_translations_reference_terms_kind_slug",
                        columns: x => new { x.kind, x.slug },
                        principalSchema: "vehicle_reference",
                        principalTable: "reference_terms",
                        principalColumns: new[] { "kind", "slug" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "vehicle_reference",
                table: "reference_terms",
                columns: new[] { "kind", "slug", "canonical_name" },
                values: new object[,]
                {
                    { 1, "diesel", "Diesel" },
                    { 1, "electric", "Electric" },
                    { 1, "hybrid", "Hybrid" },
                    { 1, "petrol", "Petrol" },
                    { 2, "automatic", "Automatic" },
                    { 2, "manual", "Manual" },
                    { 3, "all-wheel-drive", "All-wheel drive" },
                    { 3, "front-wheel-drive", "Front-wheel drive" },
                    { 3, "rear-wheel-drive", "Rear-wheel drive" },
                    { 4, "base", "Base" },
                    { 4, "comfort", "Comfort" },
                    { 4, "premium", "Premium" }
                });

            migrationBuilder.InsertData(
                schema: "vehicle_reference",
                table: "reference_term_translations",
                columns: new[] { "kind", "locale", "slug", "name" },
                values: new object[,]
                {
                    { 1, "ar", "diesel", "ديزل" },
                    { 1, "en", "diesel", "Diesel" },
                    { 1, "tr", "diesel", "Dizel" },
                    { 1, "ar", "electric", "كهربائي" },
                    { 1, "en", "electric", "Electric" },
                    { 1, "tr", "electric", "Elektrik" },
                    { 1, "ar", "hybrid", "هجين" },
                    { 1, "en", "hybrid", "Hybrid" },
                    { 1, "tr", "hybrid", "Hibrit" },
                    { 1, "ar", "petrol", "بنزين" },
                    { 1, "en", "petrol", "Petrol" },
                    { 1, "tr", "petrol", "Benzin" },
                    { 2, "ar", "automatic", "أوتوماتيك" },
                    { 2, "en", "automatic", "Automatic" },
                    { 2, "tr", "automatic", "Otomatik" },
                    { 2, "ar", "manual", "يدوي" },
                    { 2, "en", "manual", "Manual" },
                    { 2, "tr", "manual", "Manuel" },
                    { 3, "ar", "all-wheel-drive", "دفع رباعي" },
                    { 3, "en", "all-wheel-drive", "All-wheel drive" },
                    { 3, "tr", "all-wheel-drive", "Dört çeker" },
                    { 3, "ar", "front-wheel-drive", "دفع أمامي" },
                    { 3, "en", "front-wheel-drive", "Front-wheel drive" },
                    { 3, "tr", "front-wheel-drive", "Önden çekiş" },
                    { 3, "ar", "rear-wheel-drive", "دفع خلفي" },
                    { 3, "en", "rear-wheel-drive", "Rear-wheel drive" },
                    { 3, "tr", "rear-wheel-drive", "Arkadan itiş" },
                    { 4, "ar", "base", "أساسي" },
                    { 4, "en", "base", "Base" },
                    { 4, "tr", "base", "Standart" },
                    { 4, "ar", "comfort", "كومفورت" },
                    { 4, "en", "comfort", "Comfort" },
                    { 4, "tr", "comfort", "Comfort" },
                    { 4, "ar", "premium", "بريميوم" },
                    { 4, "en", "premium", "Premium" },
                    { 4, "tr", "premium", "Premium" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reference_term_translations",
                schema: "vehicle_reference");

            migrationBuilder.DropTable(
                name: "reference_terms",
                schema: "vehicle_reference");
        }
    }
}
