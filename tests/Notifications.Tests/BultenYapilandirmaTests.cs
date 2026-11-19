using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Tests.Integration;

namespace CarGallery.Modules.Notifications.Tests;

/// <summary>
/// `0018` C14 — bülten yapılandırması KURULUŞTA doğrulanır ve hiçbir alanın koda gömülü
/// varsayılanı yoktur. "Uygulama ayağa kalkmaz"ın Host yarısı `Host.Tests`'te
/// (`BultenYapilandirmasiTests`); burada hangi değerin neden reddedildiği.
/// </summary>
public sealed class BultenYapilandirmaTests
{
    [Theory(DisplayName = "0018/AC-50: Çıkış bağlantısının imza sırrı yapılandırmada yokken uygulama ayağa kalkmaz")]
    [InlineData(" ")]
    [InlineData("bu-base64-degil")]
    [InlineData("QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFB")]
    public void Reddeder_EksikYaDaKisaImzaSirri(string sir)
    {
        Result<NewsletterOptions> sonuc = NewsletterOptions.Create(Bulten.BultenKurulumu() with { UnsubscribeSigningKey = sir });

        Assert.Equal("notification.newsletter_signing_key_missing", sonuc.Error.Code);
    }

    public static TheoryData<string, NewsletterSetup> EksikAlanlar() => new()
    {
        { "notification.newsletter_schedule_missing", Bulten.BultenKurulumu() with { SendDayOfWeek = "" } },
        { "notification.newsletter_schedule_missing", Bulten.BultenKurulumu() with { SendDayOfWeek = "8" } },
        { "notification.newsletter_schedule_missing", Bulten.BultenKurulumu() with { SendTimeUtc = "" } },
        { "notification.newsletter_schedule_missing", Bulten.BultenKurulumu() with { SendTimeUtc = "25:00" } },
        { "notification.newsletter_confirmation_lifetime_missing", Bulten.BultenKurulumu() with { ConfirmationLinkHours = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { RequestLimit = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { RequestWindowMinutes = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { ValidRequestLimit = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { ValidRequestWindowMinutes = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { AddressConfirmationLimit = 0 } },
        { "notification.newsletter_rate_limit_missing", Bulten.BultenKurulumu() with { AddressWindowHours = 0 } },
        { "notification.newsletter_signing_key_missing", Bulten.BultenKurulumu() with { UnsubscribeSigningKey = "" } },
    };

    [Theory(DisplayName = "0018/AC-97: Dönem günü/saati, teyit bağlantısı süresi, üç hız sınırının (kaba istek, geçerli istek, adres başına teyit iletisi) eşik ve pencerelerinden ya da çıkış bağlantısı sırrından biri yapılandırmada yokken uygulama ayağa kalkmaz")]
    [MemberData(nameof(EksikAlanlar))]
    public void Reddeder_EksikAlan(string beklenen, NewsletterSetup kurulum) =>
        Assert.Equal(beklenen, NewsletterOptions.Create(kurulum).Error.Code);

    /// <summary>Sıfır vakayla koşan bir `Theory` yeşildir; kapı sessizce açılmasın (docs/testing.md § 3).</summary>
    [Fact]
    public void BosDegil_EksikAlanListesi() => Assert.Equal(12, EksikAlanlar().Count);

    [Fact(DisplayName = "0018/AC-98: Dönem anı, teyit süresi ve üç hız sınırını doğrulayan testler yapılandırılan değeri kullanır, koda gömülü bir sabiti değil")]
    public void Kullanir_YapilandirilanDegerleri()
    {
        NewsletterOptions secenek = NewsletterOptions.Create(Bulten.BultenKurulumu(
            teyitSaati: 48, gecerliIstek: 4, adresSiniri: 2, gun: "Wednesday", saat: "08:30")).Value;

        // Spec'in önerileri (72 saat, 5, 3, Pazartesi) BİLEREK kullanılmıyor: bir sabite bağlı
        // kod burada farklı bir değer döndürür.
        Assert.Equal(TimeSpan.FromHours(48), secenek.ConfirmationLinkLifetime);
        Assert.Equal(4, secenek.ValidRequestLimit);
        Assert.Equal(2, secenek.AddressConfirmationLimit);
        Assert.Equal(DayOfWeek.Wednesday, secenek.SendDayOfWeek);
        Assert.Equal(new TimeOnly(8, 30), secenek.SendTimeUtc);
        Assert.Equal(30, secenek.RequestLimit);
    }
}
