using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleReference.Infrastructure.Migrations
{
    /// <summary>
    /// `0021` — marka varlığı ve model → marka bağı. YALNIZ <c>vehicle_reference</c> şemasına
    /// dokunur (C4, D15): vitrin okuma modeli model kimliğini taşır ve kimlik değişmez (AC-3),
    /// bu yüzden <c>catalog</c>/<c>vehicle_catalog</c> şemasında hiçbir şey değişmez.
    ///
    /// <para><b>Ayrılma sabit bir eşlemeyle</b> (C4, D3): kurulumun on iki modeli kimlikle
    /// eşlenir ve her dilin görünen adı marka ve model parçasına ELLE yazılır — "ilk boşluk"
    /// kuralıyla değil. "Mercedes-Benz C-Serisi" boşlukla bölünmez; Arapça "مرسيدس بنز الفئة C"
    /// hiçbir kuralla bölünemez. Eşlemenin sözü: her dilde <c>marka + " " + model</c> eski tam
    /// adı birebir verir (AC-5) — vitrin, panel ve talep kopyası aynı adı göstermeye devam eder
    /// ve ilan adresleri yazma modelinde durduğu için hiç değişmez (AC-4).</para>
    ///
    /// <para><b>Eşlemede olmayan bir model varsa güncelleme DURUR</b> (hata ile). Bugüne kadar
    /// modülün yazma yolu yoktu ve geliştirme tohumu da aynı kimliği kullanıyordu; listede
    /// olmayan bir satır beklenmez. Varsa onu tahminle bölmek C4'ün yasakladığı şeydir.</para>
    /// </summary>
    public partial class AddVehicleBrands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_brands",
                schema: "vehicle_reference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    canonical_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_home = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_brand_translations",
                schema: "vehicle_reference",
                columns: table => new
                {
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_brand_translations", x => new { x.brand_id, x.locale });
                    table.ForeignKey(
                        name: "FK_vehicle_brand_translations_vehicle_brands_brand_id",
                        column: x => x.brand_id,
                        principalSchema: "vehicle_reference",
                        principalTable: "vehicle_brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Dokuz marka. Kısaltma D10 kuralıyla kanonik addan üretildi ve burada sabit; anahtar
            // değişmez kültürde büyük harf (C3). Hiçbiri ana sayfada işaretli başlamaz: hangi
            // markanın öne çıkacağı galerinin kararıdır (D7).
            migrationBuilder.Sql("""
                INSERT INTO vehicle_reference.vehicle_brands (id, canonical_name, canonical_key, slug, is_active, show_on_home, version) VALUES
                    ('0192c000-0000-7000-8000-0000000000b1', 'BMW', 'BMW', 'bmw', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b2', 'Ford', 'FORD', 'ford', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b3', 'Honda', 'HONDA', 'honda', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b4', 'Hyundai', 'HYUNDAI', 'hyundai', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b5', 'Mercedes-Benz', 'MERCEDES-BENZ', 'mercedes-benz', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b6', 'Nissan', 'NISSAN', 'nissan', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b7', 'Renault', 'RENAULT', 'renault', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b8', 'Toyota', 'TOYOTA', 'toyota', TRUE, FALSE, 1),
                    ('0192c000-0000-7000-8000-0000000000b9', 'Volkswagen', 'VOLKSWAGEN', 'volkswagen', TRUE, FALSE, 1);
                """);

            migrationBuilder.Sql("""
                INSERT INTO vehicle_reference.vehicle_brand_translations (brand_id, locale, name) VALUES
                    ('0192c000-0000-7000-8000-0000000000b1', 'tr', 'BMW'),
                    ('0192c000-0000-7000-8000-0000000000b1', 'en', 'BMW'),
                    ('0192c000-0000-7000-8000-0000000000b1', 'ar', 'بي إم دبليو'),
                    ('0192c000-0000-7000-8000-0000000000b2', 'tr', 'Ford'),
                    ('0192c000-0000-7000-8000-0000000000b2', 'en', 'Ford'),
                    ('0192c000-0000-7000-8000-0000000000b2', 'ar', 'فورد'),
                    ('0192c000-0000-7000-8000-0000000000b3', 'tr', 'Honda'),
                    ('0192c000-0000-7000-8000-0000000000b3', 'en', 'Honda'),
                    ('0192c000-0000-7000-8000-0000000000b3', 'ar', 'هوندا'),
                    ('0192c000-0000-7000-8000-0000000000b4', 'tr', 'Hyundai'),
                    ('0192c000-0000-7000-8000-0000000000b4', 'en', 'Hyundai'),
                    ('0192c000-0000-7000-8000-0000000000b4', 'ar', 'هيونداي'),
                    ('0192c000-0000-7000-8000-0000000000b5', 'tr', 'Mercedes-Benz'),
                    ('0192c000-0000-7000-8000-0000000000b5', 'en', 'Mercedes-Benz'),
                    ('0192c000-0000-7000-8000-0000000000b5', 'ar', 'مرسيدس بنز'),
                    ('0192c000-0000-7000-8000-0000000000b6', 'tr', 'Nissan'),
                    ('0192c000-0000-7000-8000-0000000000b6', 'en', 'Nissan'),
                    ('0192c000-0000-7000-8000-0000000000b6', 'ar', 'نيسان'),
                    ('0192c000-0000-7000-8000-0000000000b7', 'tr', 'Renault'),
                    ('0192c000-0000-7000-8000-0000000000b7', 'en', 'Renault'),
                    ('0192c000-0000-7000-8000-0000000000b7', 'ar', 'رينو'),
                    ('0192c000-0000-7000-8000-0000000000b8', 'tr', 'Toyota'),
                    ('0192c000-0000-7000-8000-0000000000b8', 'en', 'Toyota'),
                    ('0192c000-0000-7000-8000-0000000000b8', 'ar', 'تويوتا'),
                    ('0192c000-0000-7000-8000-0000000000b9', 'tr', 'Volkswagen'),
                    ('0192c000-0000-7000-8000-0000000000b9', 'en', 'Volkswagen'),
                    ('0192c000-0000-7000-8000-0000000000b9', 'ar', 'فولكس فاجن');
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Model kimliği DEĞİŞMEZ (AC-3); yalnız marka bağı, markasız kanonik ad ve anahtar.
            migrationBuilder.Sql("""
                UPDATE vehicle_reference.vehicle_models AS model
                SET brand_id = mapping.brand_id, canonical_name = mapping.canonical_name, canonical_key = mapping.canonical_key
                FROM (VALUES
                    ('0192c000-0000-7000-8000-00000000000a'::uuid, '0192c000-0000-7000-8000-0000000000b8'::uuid, 'Corolla', 'COROLLA'),
                    ('0192c000-0000-7000-8000-0000000000a1'::uuid, '0192c000-0000-7000-8000-0000000000b8'::uuid, 'Yaris', 'YARIS'),
                    ('0192c000-0000-7000-8000-0000000000a2'::uuid, '0192c000-0000-7000-8000-0000000000b9'::uuid, 'Passat', 'PASSAT'),
                    ('0192c000-0000-7000-8000-0000000000a3'::uuid, '0192c000-0000-7000-8000-0000000000b9'::uuid, 'Golf', 'GOLF'),
                    ('0192c000-0000-7000-8000-0000000000a4'::uuid, '0192c000-0000-7000-8000-0000000000b7'::uuid, 'Clio', 'CLIO'),
                    ('0192c000-0000-7000-8000-0000000000a5'::uuid, '0192c000-0000-7000-8000-0000000000b7'::uuid, 'Megane', 'MEGANE'),
                    ('0192c000-0000-7000-8000-0000000000a6'::uuid, '0192c000-0000-7000-8000-0000000000b2'::uuid, 'Focus', 'FOCUS'),
                    ('0192c000-0000-7000-8000-0000000000a7'::uuid, '0192c000-0000-7000-8000-0000000000b4'::uuid, 'i20', 'I20'),
                    ('0192c000-0000-7000-8000-0000000000a8'::uuid, '0192c000-0000-7000-8000-0000000000b3'::uuid, 'Civic', 'CIVIC'),
                    ('0192c000-0000-7000-8000-0000000000a9'::uuid, '0192c000-0000-7000-8000-0000000000b6'::uuid, 'Qashqai', 'QASHQAI'),
                    ('0192c000-0000-7000-8000-0000000000aa'::uuid, '0192c000-0000-7000-8000-0000000000b1'::uuid, '3 Serisi', '3 SERISI'),
                    ('0192c000-0000-7000-8000-0000000000ab'::uuid, '0192c000-0000-7000-8000-0000000000b5'::uuid, 'C-Serisi', 'C-SERISI')
                ) AS mapping (id, brand_id, canonical_name, canonical_key)
                WHERE model.id = mapping.id;
                """);

            // Görünen adlar dil başına elle bölündü: marka parçası yukarıdaki marka çevirisi,
            // model parçası burada. Satırlar UPDATE değil SİL + EKLE: geliştirme tohumu bir
            // dili hiç yazmamış olabilir.
            migrationBuilder.Sql("""
                DELETE FROM vehicle_reference.vehicle_model_translations
                WHERE vehicle_model_id IN (
                    '0192c000-0000-7000-8000-00000000000a', '0192c000-0000-7000-8000-0000000000a1', '0192c000-0000-7000-8000-0000000000a2', '0192c000-0000-7000-8000-0000000000a3', '0192c000-0000-7000-8000-0000000000a4', '0192c000-0000-7000-8000-0000000000a5', '0192c000-0000-7000-8000-0000000000a6', '0192c000-0000-7000-8000-0000000000a7', '0192c000-0000-7000-8000-0000000000a8', '0192c000-0000-7000-8000-0000000000a9', '0192c000-0000-7000-8000-0000000000aa', '0192c000-0000-7000-8000-0000000000ab'
                );

                INSERT INTO vehicle_reference.vehicle_model_translations (vehicle_model_id, locale, name)
                SELECT id, locale, name FROM (VALUES
                    ('0192c000-0000-7000-8000-00000000000a'::uuid, 'tr', 'Corolla'),
                    ('0192c000-0000-7000-8000-00000000000a'::uuid, 'en', 'Corolla'),
                    ('0192c000-0000-7000-8000-00000000000a'::uuid, 'ar', 'كورولا'),
                    ('0192c000-0000-7000-8000-0000000000a1'::uuid, 'tr', 'Yaris'),
                    ('0192c000-0000-7000-8000-0000000000a1'::uuid, 'en', 'Yaris'),
                    ('0192c000-0000-7000-8000-0000000000a1'::uuid, 'ar', 'يارِس'),
                    ('0192c000-0000-7000-8000-0000000000a2'::uuid, 'tr', 'Passat'),
                    ('0192c000-0000-7000-8000-0000000000a2'::uuid, 'en', 'Passat'),
                    ('0192c000-0000-7000-8000-0000000000a2'::uuid, 'ar', 'باسات'),
                    ('0192c000-0000-7000-8000-0000000000a3'::uuid, 'tr', 'Golf'),
                    ('0192c000-0000-7000-8000-0000000000a3'::uuid, 'en', 'Golf'),
                    ('0192c000-0000-7000-8000-0000000000a3'::uuid, 'ar', 'جولف'),
                    ('0192c000-0000-7000-8000-0000000000a4'::uuid, 'tr', 'Clio'),
                    ('0192c000-0000-7000-8000-0000000000a4'::uuid, 'en', 'Clio'),
                    ('0192c000-0000-7000-8000-0000000000a4'::uuid, 'ar', 'كليو'),
                    ('0192c000-0000-7000-8000-0000000000a5'::uuid, 'tr', 'Megane'),
                    ('0192c000-0000-7000-8000-0000000000a5'::uuid, 'en', 'Megane'),
                    ('0192c000-0000-7000-8000-0000000000a5'::uuid, 'ar', 'ميجان'),
                    ('0192c000-0000-7000-8000-0000000000a6'::uuid, 'tr', 'Focus'),
                    ('0192c000-0000-7000-8000-0000000000a6'::uuid, 'en', 'Focus'),
                    ('0192c000-0000-7000-8000-0000000000a6'::uuid, 'ar', 'فوكس'),
                    ('0192c000-0000-7000-8000-0000000000a7'::uuid, 'tr', 'i20'),
                    ('0192c000-0000-7000-8000-0000000000a7'::uuid, 'en', 'i20'),
                    ('0192c000-0000-7000-8000-0000000000a7'::uuid, 'ar', 'i20'),
                    ('0192c000-0000-7000-8000-0000000000a8'::uuid, 'tr', 'Civic'),
                    ('0192c000-0000-7000-8000-0000000000a8'::uuid, 'en', 'Civic'),
                    ('0192c000-0000-7000-8000-0000000000a8'::uuid, 'ar', 'سيفيك'),
                    ('0192c000-0000-7000-8000-0000000000a9'::uuid, 'tr', 'Qashqai'),
                    ('0192c000-0000-7000-8000-0000000000a9'::uuid, 'en', 'Qashqai'),
                    ('0192c000-0000-7000-8000-0000000000a9'::uuid, 'ar', 'قاشقاي'),
                    ('0192c000-0000-7000-8000-0000000000aa'::uuid, 'tr', '3 Serisi'),
                    ('0192c000-0000-7000-8000-0000000000aa'::uuid, 'en', '3 Series'),
                    ('0192c000-0000-7000-8000-0000000000aa'::uuid, 'ar', 'الفئة الثالثة'),
                    ('0192c000-0000-7000-8000-0000000000ab'::uuid, 'tr', 'C-Serisi'),
                    ('0192c000-0000-7000-8000-0000000000ab'::uuid, 'en', 'C-Class'),
                    ('0192c000-0000-7000-8000-0000000000ab'::uuid, 'ar', 'الفئة C')
                ) AS mapping (id, locale, name)
                WHERE EXISTS (SELECT 1 FROM vehicle_reference.vehicle_models AS model WHERE model.id = mapping.id);
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM vehicle_reference.vehicle_models WHERE brand_id IS NULL) THEN
                        RAISE EXCEPTION 'vehicle_reference.vehicle_models: eşlemede olmayan model var; marka ayrılması yalnız sabit eşlemeyle yapılır (0021 C4).';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "brand_id",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_models",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_models_brand_canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_models",
                columns: new[] { "brand_id", "canonical_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_brands_canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_brands",
                column: "canonical_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_brands_slug",
                schema: "vehicle_reference",
                table: "vehicle_brands",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicle_models_vehicle_brands_brand_id",
                schema: "vehicle_reference",
                table: "vehicle_models",
                column: "brand_id",
                principalSchema: "vehicle_reference",
                principalTable: "vehicle_brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma tam adları geri yazar: kanonik ad ve her dilin görünen adı yeniden
            // "marka + model" olur (dilde ad yoksa parça kanonik ada düşer).
            migrationBuilder.Sql("""
                UPDATE vehicle_reference.vehicle_model_translations AS translation
                SET name = COALESCE(
                        (SELECT brand_translation.name
                         FROM vehicle_reference.vehicle_brand_translations AS brand_translation
                         WHERE brand_translation.brand_id = brand.id AND brand_translation.locale = translation.locale),
                        brand.canonical_name) || ' ' || translation.name
                FROM vehicle_reference.vehicle_models AS model
                JOIN vehicle_reference.vehicle_brands AS brand ON brand.id = model.brand_id
                WHERE translation.vehicle_model_id = model.id;

                UPDATE vehicle_reference.vehicle_models AS model
                SET canonical_name = brand.canonical_name || ' ' || model.canonical_name
                FROM vehicle_reference.vehicle_brands AS brand
                WHERE brand.id = model.brand_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_vehicle_models_vehicle_brands_brand_id",
                schema: "vehicle_reference",
                table: "vehicle_models");

            migrationBuilder.DropTable(
                name: "vehicle_brand_translations",
                schema: "vehicle_reference");

            migrationBuilder.DropTable(
                name: "vehicle_brands",
                schema: "vehicle_reference");

            migrationBuilder.DropIndex(
                name: "ux_vehicle_models_brand_canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_models");

            migrationBuilder.DropColumn(
                name: "brand_id",
                schema: "vehicle_reference",
                table: "vehicle_models");

            migrationBuilder.DropColumn(
                name: "canonical_key",
                schema: "vehicle_reference",
                table: "vehicle_models");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "vehicle_reference",
                table: "vehicle_models");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "vehicle_reference",
                table: "vehicle_models");
        }
    }
}
