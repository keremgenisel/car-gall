using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

/// <summary>
/// Sır **geri döndürülebilir** olmak zorundadır (C2a/D1) ama metni hiçbir yere
/// sızmamalıdır (AC-47, AC-51). İkisi aynı tipin iki ayrı sorumluluğudur ve bu dosya
/// ikisini birden ölçüyor.
/// </summary>
public sealed class DogrulamaSirriTests
{
    [Fact(DisplayName = "0013/AC-1: Kurulum başlatıldığında sunucu bir doğrulama sırrı üretir ve yanıt `otpauth://` adresini **ve** Base32 sırrı **metin olarak** döner; yanıtta görsel bulunmaz")]
    public void Uretir_Base32Sir()
    {
        TwoFactorSecret sir = TwoFactorSecret.New();

        Assert.Equal(32, sir.Base32.Length);
        Assert.Equal(20, sir.ToBytes().Length);
        Assert.All(sir.Base32, simge => Assert.Contains(simge, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Fact(DisplayName = "0013/AC-1: Kurulum başlatıldığında sunucu bir doğrulama sırrı üretir ve yanıt `otpauth://` adresini **ve** Base32 sırrı **metin olarak** döner; yanıtta görsel bulunmaz")]
    public void Dondurur_AyniBaytlari_Base32Uzerinden()
    {
        TwoFactorSecret sir = TwoFactorSecret.New();

        Assert.Equal(sir.ToBytes(), TwoFactorSecret.FromBase32(sir.Base32).Value.ToBytes());
    }

    /// <summary>
    /// Elle giriş metni dörtlü gruplar hâlinde gösteriliyor (D17); personelin onu
    /// birebir kopyalaması beklenemez.
    /// </summary>
    [Fact(DisplayName = "0013/AC-10: Kurulum ekranı kare kodu **uçta** çizer ve elle giriş metnini **her zaman görünür** tutar; sır sunucuda görsele gömülmez")]
    public void Okur_BosluklaVeKucukHarfleYazilmisSirri()
    {
        TwoFactorSecret sir = TwoFactorSecret.New();
        string elleYazilmis = string.Join(" ", Dortluler(sir.Base32)).ToLowerInvariant();

        Result<TwoFactorSecret> okunan = TwoFactorSecret.FromBase32(elleYazilmis);

        Assert.True(okunan.IsSuccess);
        Assert.Equal(sir.Base32, okunan.Value.Base32);
    }

    [Theory(DisplayName = "0013/AC-1: Kurulum başlatıldığında sunucu bir doğrulama sırrı üretir ve yanıt `otpauth://` adresini **ve** Base32 sırrı **metin olarak** döner; yanıtta görsel bulunmaz")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0189")]
    public void Reddeder_Base32OlmayanDegeri(string? deger) =>
        Assert.Equal(
            IdentityErrors.CorruptTwoFactorSecret,
            TwoFactorSecret.FromBase32(deger).Error.Code);

    [Fact(DisplayName = "0013/AC-2: `otpauth://` adresi dondurulmuş parametre kümesini taşır: `algorithm=SHA1`, `digits=6`, `period=30` ve yapılandırmadan gelen `issuer`")]
    public void Tasir_DondurulmusParametreleri()
    {
        TwoFactorSecret sir = TwoFactorSecret.New();

        string adres = sir.ToUri("Örnek Galeri", "yonetici@ornek.com");

        Assert.StartsWith("otpauth://totp/", adres, StringComparison.Ordinal);
        Assert.Contains($"secret={sir.Base32}", adres, StringComparison.Ordinal);
        Assert.Contains("algorithm=SHA1", adres, StringComparison.Ordinal);
        Assert.Contains("digits=6", adres, StringComparison.Ordinal);
        Assert.Contains("period=30", adres, StringComparison.Ordinal);
        Assert.Contains("issuer=%C3%96rnek%20Galeri", adres, StringComparison.Ordinal);
        Assert.Contains("%C3%96rnek%20Galeri%3Ayonetici%40ornek.com", adres, StringComparison.Ordinal);
    }

    /// <summary>
    /// `PasswordHash` ile aynı desen: log, hata mesajı ve `ToString()` sırra dair hiçbir
    /// şey söylemez. Bu, AC-47'nin tipteki yarısıdır — ötekini log testi ölçüyor.
    /// </summary>
    [Fact(DisplayName = "0013/AC-47: Doğrulama sırrı ve kurtarma kodları **hiçbir log kaydında** görünmez")]
    public void Maskelenir_MetneCevrildiginde()
    {
        TwoFactorSecret sir = TwoFactorSecret.New();

        Assert.Equal("***", sir.ToString());
        Assert.DoesNotContain(sir.Base32, $"{sir}", StringComparison.Ordinal);
    }

    private static IEnumerable<string> Dortluler(string deger)
    {
        for (int basla = 0; basla < deger.Length; basla += 4)
        {
            yield return deger.Substring(basla, Math.Min(4, deger.Length - basla));
        }
    }
}
