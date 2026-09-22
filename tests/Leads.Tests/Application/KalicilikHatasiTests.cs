using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Application;

/// <summary>
/// Tekil indeks çakışması METİNDEN değil, YAPISAL olarak tanınır.
///
/// Eski uygulama istisna metninde indeks adını arıyordu. İki yönden kırılgandı: indeks
/// yeniden adlandırılırsa sessizce eşleşmeyi bırakır — o anda eşzamanlı ikinci istek
/// 500 alır ve AC-35'in koruması tam da işe yarayacağı yerde kaybolur — ve metninde
/// aynı dizeyi taşıyan BAŞKA bir hata yanlışlıkla "çakışma" sayılırdı.
/// </summary>
public sealed class KalicilikHatasiTests
{
    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public void Tanimaz_MetninIcindeIndeksAdiGecenBaskaHata()
    {
        // Metne bakan eski uygulama bunu ÇAKIŞMA sanırdı ve var olmayan bir kaydı
        // okumaya çalışırdı.
        DbUpdateException baskaHata = new(
            "kaydetme başarısız",
            new InvalidOperationException(
                $"ilgisiz bir hata, metninde {LeadPersistence.IdempotencyKeyIndexName} geçiyor"));

        Assert.False(LeadPersistence.IsIdempotencyKeyConflict(baskaHata));
    }

    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public void Tanimaz_IcHatasiOlmayanKaydetmeHatasi() =>
        Assert.False(LeadPersistence.IsIdempotencyKeyConflict(new DbUpdateException("kaydetme başarısız")));
}
