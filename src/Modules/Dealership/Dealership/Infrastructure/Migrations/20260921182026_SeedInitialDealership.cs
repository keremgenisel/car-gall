using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Dealership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialDealership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D1 — başlangıç verisi migration ile gelir; bu spec'te yazma ucu yoktur.
            // Identity ve kimlik doğrulama olmadan korumasız bir yazma ucu, galeri profilini
            // ve şube verisini internete açık biçimde düzenlenebilir bırakırdı.
            //
            // İki şube seed ediliyor: B5 "en az bir aktif şube" kuralı tek şubeyle de
            // sağlanırdı, ama ikinci şube AC-17'yi (ikiden birini kapatma) üretim benzeri
            // veriyle sınanabilir kılıyor.
            migrationBuilder.Sql("""
                INSERT INTO dealership.galleries (id, commercial_name, tax_number, trade_registry_number, logo_media_asset_id)
                VALUES ('00000000-0000-7000-8000-00000000da01', 'Oto Galeri', '1234567890', 'TR-123456', NULL);

                INSERT INTO dealership.branches
                    (id, names, addresses, phone, whatsapp, email, time_zone, status, slug, latitude, longitude, media_asset_ids)
                VALUES
                    ('00000000-0000-7000-8000-00000000b001',
                     '{"tr":"Kadıköy Şubesi","ar":"فرع كاديكوي","en":"Kadikoy Branch"}',
                     '{"tr":"Bağdat Caddesi No:1, Kadıköy/İstanbul","ar":"شارع بغداد ١، كاديكوي","en":"Bagdat Avenue No:1, Kadikoy"}',
                     '+902165554433', '+905321112233', 'kadikoy@otogaleri.com.tr',
                     'Europe/Istanbul', 1, 'kadikoy-subesi', 40.98330000, 29.06670000, '{}'),
                    ('00000000-0000-7000-8000-00000000b002',
                     '{"tr":"Ataşehir Şubesi","ar":"فرع أتاشهير","en":"Atasehir Branch"}',
                     '{"tr":"Barbaros Mahallesi No:7, Ataşehir/İstanbul","ar":"حي بارباروس ٧، أتاشهير","en":"Barbaros District No:7, Atasehir"}',
                     '+902164448822', NULL, NULL,
                     'Europe/Istanbul', 1, 'atasehir-subesi', NULL, NULL, '{}');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM dealership.branches
                WHERE id IN ('00000000-0000-7000-8000-00000000b001', '00000000-0000-7000-8000-00000000b002');

                DELETE FROM dealership.galleries WHERE id = '00000000-0000-7000-8000-00000000da01';
                """);
        }
    }
}
