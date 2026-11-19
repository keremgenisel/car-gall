using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNewsletterTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `0018` C6 · D11 — şablonlar KURULUM VERİSİDİR ve seed migration ile gelir
            // (`0009` C13 deseni). Altı anahtar × üç dil = on sekiz satır (AC-96).
            //
            // Bülten başlık, satır ve altlık olarak AYRI şablonlardır; satır şablonu her ilan
            // için bir kez doldurulur. "Fiyat sorunuz" ve "Diğer yeni araçlar…" de buradadır
            // (C8). Parça şablonların konusu kullanılmaz ama boş değildir: `TemplateResolver`
            // boş konuyu eksik sayıyor ve o kural `0009` R9'un kendisidir.
            //
            // Metinler `0009` AC-9'un taramasından geçer: dönüş süresi taahhüdü yok.
            // Teyit iletisi tanıtım içeriği TAŞIMAZ (AC-26) ve galerinin adı ayrı bir satırdadır:
            // ad okunamazsa yalnız o satır düşer (`TemplateRenderer`, `0009` AC-32).
            migrationBuilder.Sql("""
                INSERT INTO notifications.templates (template_key, locale, subject, body)
                VALUES
                    ('newsletter.confirmation', 'tr', 'Bülten aboneliğinizi onaylayın', E'Merhaba,\n\nBu adres yeni araç bültenine abone olmak için kullanıldı. Aboneliği onaylamak için bağlantıyı açıp "Aboneliği onayla" düğmesine basın:\n\n{teyitBaglantisi}\n\nBu isteği siz yapmadıysanız iletiyi yok sayın; onaylamadığınız sürece size bülten gönderilmez.\n\nBu iletiyi {galeriAdi} gönderdi.'),
                    ('newsletter.confirmation', 'ar', 'أكّد اشتراكك في النشرة', E'مرحبًا،\n\nاستُخدم هذا العنوان للاشتراك في نشرة السيارات الجديدة. لتأكيد الاشتراك افتح الرابط واضغط زر «تأكيد الاشتراك»:\n\n{teyitBaglantisi}\n\nإذا لم تطلب ذلك فتجاهل هذه الرسالة؛ لن تصلك النشرة ما لم تؤكد اشتراكك.\n\nأرسل {galeriAdi} هذه الرسالة.'),
                    ('newsletter.confirmation', 'en', 'Confirm your newsletter subscription', E'Hello,\n\nThis address was used to subscribe to the new-car newsletter. To confirm, open the link and press "Confirm subscription":\n\n{teyitBaglantisi}\n\nIf you did not ask for this, ignore this message; you will not receive the newsletter unless you confirm.\n\nThis message was sent by {galeriAdi}.'),
                    ('newsletter.header', 'tr', 'Vitrine yeni gelen araçlar', E'Merhaba,\n\nVitrine yeni gelen araçlar:'),
                    ('newsletter.header', 'ar', 'سيارات وصلت حديثًا إلى المعرض', E'مرحبًا،\n\nسيارات وصلت حديثًا إلى المعرض:'),
                    ('newsletter.header', 'en', 'New cars in the showcase', E'Hello,\n\nNew cars in the showcase:'),
                    ('newsletter.row', 'tr', 'Araç', E'{modelAdi} · {modelYili} · {km} km · {fiyat}\n{subeAdi}\n{ilanBaglantisi}'),
                    ('newsletter.row', 'ar', 'سيارة', E'{modelAdi} · {modelYili} · {km} كم · {fiyat}\n{subeAdi}\n{ilanBaglantisi}'),
                    ('newsletter.row', 'en', 'Car', E'{modelAdi} · {modelYili} · {km} km · {fiyat}\n{subeAdi}\n{ilanBaglantisi}'),
                    ('newsletter.footer', 'tr', 'Bülten', E'Bu bülteni {galeriAdi} gönderdi.\nAbonelikten tek tıkla çıkmak için: {cikisBaglantisi}'),
                    ('newsletter.footer', 'ar', 'النشرة', E'أرسل {galeriAdi} هذه النشرة.\nلإلغاء الاشتراك بنقرة واحدة: {cikisBaglantisi}'),
                    ('newsletter.footer', 'en', 'Newsletter', E'This newsletter was sent by {galeriAdi}.\nTo unsubscribe in one click: {cikisBaglantisi}'),
                    ('newsletter.price_on_request', 'tr', 'Fiyat', 'Fiyat sorunuz'),
                    ('newsletter.price_on_request', 'ar', 'السعر', 'السعر عند الطلب'),
                    ('newsletter.price_on_request', 'en', 'Price', 'Price on request'),
                    ('newsletter.more', 'tr', 'Vitrin', 'Diğer yeni araçlar için vitrine göz atın: {vitrinBaglantisi}'),
                    ('newsletter.more', 'ar', 'المعرض', 'تصفّح المعرض لمزيد من السيارات الجديدة: {vitrinBaglantisi}'),
                    ('newsletter.more', 'en', 'Showcase', 'Browse the showcase for more new cars: {vitrinBaglantisi}');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql("DELETE FROM notifications.templates WHERE template_key LIKE 'newsletter.%';");
    }
}
