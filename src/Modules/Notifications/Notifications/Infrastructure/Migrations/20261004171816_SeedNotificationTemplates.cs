using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNotificationTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // C13 — şablonlar KURULUM VERİSİDİR ve seed migration ile gelir (0003/0006 deseni).
            // Panelden düzenlenmeleri kapsam dışıdır (X5).
            //
            // İki anahtar × üç dil = altı satır. Üçüncü bir anahtar YOKTUR: pazarlama
            // şablonu bu spec'te hiç yok (X2), dolayısıyla "pazarlama gönderilmez"
            // doğrulanacak bir şey değil.
            //
            // Ziyaretçi metinleri DÖNÜŞ SÜRESİ TAAHHÜDÜ İÇERMEZ (R4, AC-9). 0005 Karar D6
            // bunu yasaklıyor ve orada bir kez gerçekten çiğnenmişti — metin "ulaştı" der,
            // "döneceğiz" demez.
            //
            // Gövdelerdeki satır sonları E'...' kaçış dizgesiyle SQL tarafında üretilir;
            // C# kaynağına gerçek satır sonu olarak girselerdi ham dizgenin girintisini
            // bozarlardı.
            migrationBuilder.Sql("""
                INSERT INTO notifications.templates (template_key, locale, subject, body)
                VALUES
                    ('lead.received.gallery', 'tr', 'Yeni talep: {modelAdi} ({subeAdi})', E'{subeAdi} şubesindeki {modelAdi} için yeni bir talep var.\n\nİlan: {ilanBaglantisi}'),
                    ('lead.received.gallery', 'ar', 'طلب جديد: {modelAdi} ({subeAdi})', E'هناك طلب جديد على {modelAdi} في فرع {subeAdi}.\n\nالإعلان: {ilanBaglantisi}'),
                    ('lead.received.gallery', 'en', 'New lead: {modelAdi} ({subeAdi})', E'There is a new lead for {modelAdi} at the {subeAdi} branch.\n\nListing: {ilanBaglantisi}'),
                    ('lead.received.visitor', 'tr', 'Talebiniz alındı', E'Merhaba,\n\n{modelAdi} için bıraktığınız talep galeriye ulaştı.\n\nİlan: {ilanBaglantisi}'),
                    ('lead.received.visitor', 'ar', 'تم استلام طلبك', E'مرحبًا،\n\nوصل طلبك بخصوص {modelAdi} إلى المعرض.\n\nالإعلان: {ilanBaglantisi}'),
                    ('lead.received.visitor', 'en', 'We received your enquiry', E'Hello,\n\nYour enquiry about {modelAdi} reached the dealership.\n\nListing: {ilanBaglantisi}');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql("DELETE FROM notifications.templates;");
    }
}
