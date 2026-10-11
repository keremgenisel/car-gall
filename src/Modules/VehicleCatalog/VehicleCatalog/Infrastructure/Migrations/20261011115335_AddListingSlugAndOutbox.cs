using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// `0012b` **D13**/**D14** — ilan adresi yazma modeline taşınıyor ve modül outbox'ını
    /// kazanıyor. Migration YALNIZ `vehicle_catalog` şemasına dokunuyor (C9).
    /// </summary>
    public partial class AddListingSlugAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "vehicle_catalog",
                table: "listings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "vehicle_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            // D31 — MEVCUT satırlar GERİ DOLDURULUYOR. Adres bu güne kadar yalnız vitrin
            // okuma modelinde yaşıyordu; yeniden üretmek, bugün yayında olan her ilanın
            // adresini değiştirir ve PAYLAŞILMIŞ HER BAĞLANTIYI kırardı — D13'ün
            // yasakladığı şeyin ta kendisi.
            //
            // İkinci tablo AYNI ŞEMADA (Yasak Listesi 3 ihlal değil) ve okuma modeli
            // yayında olan ilanların hepsini taşıyor; yayında olmayanın zaten adresi
            // yoktu. Kolon nullable kalıyor: taslağın adresi yoktur ve uydurulmaz.
            migrationBuilder.Sql(
                """
                UPDATE vehicle_catalog.listings AS l
                SET slug = s.slug
                FROM vehicle_catalog.showcase_listings AS s
                WHERE s.listing_id = l.id AND l.slug IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_listings_slug",
                schema: "vehicle_catalog",
                table: "listings",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "vehicle_catalog",
                table: "outbox_messages",
                columns: new[] { "processed_at", "failed_at", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "vehicle_catalog");

            migrationBuilder.DropIndex(
                name: "ux_listings_slug",
                schema: "vehicle_catalog",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "vehicle_catalog",
                table: "listings");
        }
    }
}
