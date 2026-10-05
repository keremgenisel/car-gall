using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleReference.Infrastructure.Migrations
{
    /// <summary>
    /// 0010a/D7, AC-14 — başlangıç model kümesi KURULUM VERİSİYLE gelir.
    ///
    /// <para><b>Neden migration:</b> `vehicle_models` bugüne kadar hiç seed edilmiyordu ve
    /// modülün yazma yüzeyi de yok. Bu hâliyle taze bir kurulumda seçilecek HİÇBİR model
    /// yoktu ve `Vehicle.Create` bir `vehicleModelId` istediği için araç kaydı
    /// oluşturulamıyordu — Intent'in "geliştiriciye bağlı değildir" sözü tutmazdı.</para>
    ///
    /// <para><b>Bu küme GEÇİCİDİR (D16).</b> Hangi modellerin geleceği bir ürün kararı ve
    /// o karar verilmedi; buradaki on iki satır bugünün geliştirme verisini genişleten bir
    /// başlangıç. Marka kavramı YOK (D6), bu yüzden kanonik ad markayı da içeriyor ve liste
    /// düz duruyor — model sayısı büyüdüğünde kullanılabilirlik düşer. Besleme yolu
    /// `0010a` X15'te, marka taksonomisi D1'in 4. diliminde.</para>
    ///
    /// <para>İlk satırın kimliği `ShowcaseSeeder`'ın kullandığı kimliktir; geliştirme
    /// seed'i onu bulunca kendi eklemesini atlar ve iki kaynak ayrışmaz.</para>
    /// </summary>
    public partial class SeedInitialVehicleModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `ON CONFLICT DO NOTHING`: geliştirme seed'i bu migration'dan önce koşmuş
            // olabilir ve aynı kimliği eklemiş olabilir.
            migrationBuilder.Sql("""
                INSERT INTO vehicle_reference.vehicle_models (id, canonical_name) VALUES
                    ('0192c000-0000-7000-8000-00000000000a', 'Toyota Corolla'),
                    ('0192c000-0000-7000-8000-0000000000a1', 'Toyota Yaris'),
                    ('0192c000-0000-7000-8000-0000000000a2', 'Volkswagen Passat'),
                    ('0192c000-0000-7000-8000-0000000000a3', 'Volkswagen Golf'),
                    ('0192c000-0000-7000-8000-0000000000a4', 'Renault Clio'),
                    ('0192c000-0000-7000-8000-0000000000a5', 'Renault Megane'),
                    ('0192c000-0000-7000-8000-0000000000a6', 'Ford Focus'),
                    ('0192c000-0000-7000-8000-0000000000a7', 'Hyundai i20'),
                    ('0192c000-0000-7000-8000-0000000000a8', 'Honda Civic'),
                    ('0192c000-0000-7000-8000-0000000000a9', 'Nissan Qashqai'),
                    ('0192c000-0000-7000-8000-0000000000aa', 'BMW 3 Serisi'),
                    ('0192c000-0000-7000-8000-0000000000ab', 'Mercedes-Benz C-Serisi')
                ON CONFLICT (id) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO vehicle_reference.vehicle_model_translations (vehicle_model_id, locale, name) VALUES
                    ('0192c000-0000-7000-8000-00000000000a', 'tr', 'Toyota Corolla'),
                    ('0192c000-0000-7000-8000-00000000000a', 'en', 'Toyota Corolla'),
                    ('0192c000-0000-7000-8000-00000000000a', 'ar', 'تويوتا كورولا'),
                    ('0192c000-0000-7000-8000-0000000000a1', 'tr', 'Toyota Yaris'),
                    ('0192c000-0000-7000-8000-0000000000a1', 'en', 'Toyota Yaris'),
                    ('0192c000-0000-7000-8000-0000000000a1', 'ar', 'تويوتا يارِس'),
                    ('0192c000-0000-7000-8000-0000000000a2', 'tr', 'Volkswagen Passat'),
                    ('0192c000-0000-7000-8000-0000000000a2', 'en', 'Volkswagen Passat'),
                    ('0192c000-0000-7000-8000-0000000000a2', 'ar', 'فولكس فاجن باسات'),
                    ('0192c000-0000-7000-8000-0000000000a3', 'tr', 'Volkswagen Golf'),
                    ('0192c000-0000-7000-8000-0000000000a3', 'en', 'Volkswagen Golf'),
                    ('0192c000-0000-7000-8000-0000000000a3', 'ar', 'فولكس فاجن جولف'),
                    ('0192c000-0000-7000-8000-0000000000a4', 'tr', 'Renault Clio'),
                    ('0192c000-0000-7000-8000-0000000000a4', 'en', 'Renault Clio'),
                    ('0192c000-0000-7000-8000-0000000000a4', 'ar', 'رينو كليو'),
                    ('0192c000-0000-7000-8000-0000000000a5', 'tr', 'Renault Megane'),
                    ('0192c000-0000-7000-8000-0000000000a5', 'en', 'Renault Megane'),
                    ('0192c000-0000-7000-8000-0000000000a5', 'ar', 'رينو ميجان'),
                    ('0192c000-0000-7000-8000-0000000000a6', 'tr', 'Ford Focus'),
                    ('0192c000-0000-7000-8000-0000000000a6', 'en', 'Ford Focus'),
                    ('0192c000-0000-7000-8000-0000000000a6', 'ar', 'فورد فوكس'),
                    ('0192c000-0000-7000-8000-0000000000a7', 'tr', 'Hyundai i20'),
                    ('0192c000-0000-7000-8000-0000000000a7', 'en', 'Hyundai i20'),
                    ('0192c000-0000-7000-8000-0000000000a7', 'ar', 'هيونداي i20'),
                    ('0192c000-0000-7000-8000-0000000000a8', 'tr', 'Honda Civic'),
                    ('0192c000-0000-7000-8000-0000000000a8', 'en', 'Honda Civic'),
                    ('0192c000-0000-7000-8000-0000000000a8', 'ar', 'هوندا سيفيك'),
                    ('0192c000-0000-7000-8000-0000000000a9', 'tr', 'Nissan Qashqai'),
                    ('0192c000-0000-7000-8000-0000000000a9', 'en', 'Nissan Qashqai'),
                    ('0192c000-0000-7000-8000-0000000000a9', 'ar', 'نيسان قاشقاي'),
                    ('0192c000-0000-7000-8000-0000000000aa', 'tr', 'BMW 3 Serisi'),
                    ('0192c000-0000-7000-8000-0000000000aa', 'en', 'BMW 3 Series'),
                    ('0192c000-0000-7000-8000-0000000000aa', 'ar', 'بي إم دبليو الفئة الثالثة'),
                    ('0192c000-0000-7000-8000-0000000000ab', 'tr', 'Mercedes-Benz C-Serisi'),
                    ('0192c000-0000-7000-8000-0000000000ab', 'en', 'Mercedes-Benz C-Class'),
                    ('0192c000-0000-7000-8000-0000000000ab', 'ar', 'مرسيدس بنز الفئة C')
                ON CONFLICT (vehicle_model_id, locale) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql("""
                DELETE FROM vehicle_reference.vehicle_models
                WHERE id IN (
                    '0192c000-0000-7000-8000-00000000000a', '0192c000-0000-7000-8000-0000000000a1', '0192c000-0000-7000-8000-0000000000a2', '0192c000-0000-7000-8000-0000000000a3', '0192c000-0000-7000-8000-0000000000a4', '0192c000-0000-7000-8000-0000000000a5', '0192c000-0000-7000-8000-0000000000a6', '0192c000-0000-7000-8000-0000000000a7', '0192c000-0000-7000-8000-0000000000a8', '0192c000-0000-7000-8000-0000000000a9', '0192c000-0000-7000-8000-0000000000aa', '0192c000-0000-7000-8000-0000000000ab'
                );
                """);
    }
}
