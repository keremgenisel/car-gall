using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0024` D1–D4, D11 — sosyal hesapların alan kuralları, veritabanı yok. Aynı kuralların telden
/// çıkan hâli (`422`, alan anahtarı, kalıcılık) `Host.Tests/SosyalHesapZinciriTests`'te.
/// </summary>
public sealed class SosyalHesapTests
{
    private static readonly Locale Tr = Locale.Create("tr").Value;

    [Theory(DisplayName = "0024/AC-8, AC-9, AC-10, AC-11, AC-12, AC-72, AC-73, AC-74, AC-75, AC-76: Biçimsel olarak o platforma ait olmayan adres `gallery.social_link_invalid` ile reddedilir.")]
    [InlineData("http://www.instagram.com/otogaleri")]
    [InlineData("https://www.facebook.com/otogaleri")]
    [InlineData("https://www.instagram.com/")]
    [InlineData("https://www.instagram.com")]
    [InlineData("https://kullanici@www.instagram.com/otogaleri")]
    [InlineData("https://www.instagram.com:8443/otogaleri")]
    [InlineData("https://instagram.com.kotu.site/x")]
    [InlineData("https://www.instagram.com%2F@kotu.site/x")]
    [InlineData("https://www.instagrаm.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://www.instagram.com:443/x")]
    [InlineData("https://www.instagram.com:/x")]
    [InlineData("https://sub.instagram.com/x")]
    [InlineData("instagram.com/otogaleri")]
    [InlineData("//www.instagram.com/otogaleri")]
    public void Reddedilir_BicimselOlarakHataliAdres(string adres)
    {
        Gallery galeri = Galeri();

        Result sonuc = Kaydet(galeri, new() { ["instagram"] = adres });

        Assert.True(sonuc.IsFailure);
        Assert.Equal(SocialLink.Invalid, sonuc.Error.Parameters!["socialLinks.instagram"]);
        Assert.Empty(galeri.SocialLinks);
        Assert.Equal(2, galeri.Version);
    }

    [Theory(DisplayName = "0024/AC-13, AC-15: Harf büyüklüğü farklı ana makine ve sorgu dizeli Facebook adresi kabul edilir ve girildiği gibi saklanır.")]
    [InlineData("instagram", "https://WWW.Instagram.COM/otogaleri")]
    [InlineData("facebook", "https://www.facebook.com/profile.php?id=100012345")]
    [InlineData("facebook", "https://m.facebook.com/otogaleri")]
    [InlineData("x", "https://twitter.com/otogaleri")]
    [InlineData("youtube", "https://www.youtube.com/@otogaleri")]
    [InlineData("tiktok", "https://www.tiktok.com/@otogaleri")]
    [InlineData("linkedin", "https://www.linkedin.com/company/otogaleri")]
    public void KabulEdilir_GecerliAdresGirildigiGibi(string platform, string adres)
    {
        Gallery galeri = Galeri();

        Assert.True(Kaydet(galeri, new() { [platform] = adres }).IsSuccess);

        SocialLink hesap = Assert.Single(galeri.SocialLinks);
        Assert.Equal(platform, hesap.Platform);
        Assert.Equal(adres, hesap.Url);
    }

    [Fact(DisplayName = "0024/AC-14: Kırpıldıktan sonra UTF-8 olarak 200 bayt olan ASCII adres kabul edilir; 201 bayt `gallery.social_link_too_long` döner.")]
    public void Sinirlar_AsciiAdresi200Baytta()
    {
        string sinirda = AsciiAdres(200);
        Gallery galeri = Galeri();

        Assert.True(Kaydet(galeri, new() { ["instagram"] = $"  {sinirda}  " }).IsSuccess);
        Assert.Equal(sinirda, Assert.Single(galeri.SocialLinks).Url);

        Result sonuc = Kaydet(galeri, new() { ["instagram"] = AsciiAdres(201) });
        Assert.Equal(SocialLink.TooLong, sonuc.Error.Parameters!["socialLinks.instagram"]);
        Assert.Equal(sinirda, Assert.Single(galeri.SocialLinks).Url);
    }

    [Fact(DisplayName = "0024/AC-77: `https://www.instagram.com/` ve 87 Arapça harf (200 bayt) kabul edilir; 88 harf (202 bayt) `gallery.social_link_too_long` döner.")]
    public void Sinirlar_ArapcaYolu200Baytta()
    {
        Gallery galeri = Galeri();

        Assert.True(Kaydet(galeri, new() { ["instagram"] = "https://www.instagram.com/" + new string('ب', 87) }).IsSuccess);

        Result sonuc = Kaydet(galeri, new() { ["instagram"] = "https://www.instagram.com/" + new string('ب', 88) });
        Assert.Equal(SocialLink.TooLong, sonuc.Error.Parameters!["socialLinks.instagram"]);
    }

    [Fact(DisplayName = "0024/AC-16: D1'de olmayan kod `gallery.social_platform_unknown` döner ve alan anahtarı gövdedeki kodu taşır.")]
    public void Reddedilir_BilinmeyenPlatform()
    {
        Result sonuc = Kaydet(Galeri(), new() { ["myspace"] = "https://myspace.com/otogaleri" });

        Assert.Equal(SocialLink.PlatformUnknown, sonuc.Error.Parameters!["socialLinks.myspace"]);
    }

    [Fact(DisplayName = "0024/AC-17, AC-18: Hatalı adresli kaydetmede hiçbir alan değişmez; iki hatalı platformun ikisi de yanıtta.")]
    public void Degistirmez_HicbirAlani_HataVarsa()
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile(
            "Oto Galeri",
            null,
            null,
            new Dictionary<Locale, string?> { [Tr] = "Yeni Ad" },
            Tr,
            new Dictionary<string, string?>
            {
                ["instagram"] = "http://www.instagram.com/x",
                ["youtube"] = "https://www.youtube.com/@gecerli",
                ["tiktok"] = "https://www.tiktok.com/",
            });

        Assert.True(sonuc.IsFailure);
        Assert.Equal("socialLinks.instagram,socialLinks.tiktok", sonuc.Error.Parameters!["fields"]);
        Assert.Equal("Oto", galeri.DisplayNames.Resolve(Tr, Tr));
        Assert.Empty(galeri.SocialLinks);
        Assert.Equal(2, galeri.Version);
    }

    [Fact(DisplayName = "0024/AC-4, AC-5, AC-6, AC-7, AC-23: Kaydetme kümeyi bütünüyle yerine koyar — değiştirir, boşaltılanı ve boşluktan ibaret olanı siler, değeri kırpar.")]
    public void YerineKoyar_KumeyiButunuyle()
    {
        Gallery galeri = Galeri();
        Assert.True(Kaydet(galeri, new()
        {
            ["instagram"] = "https://www.instagram.com/eski",
            ["youtube"] = "https://www.youtube.com/@kanal",
            ["x"] = "https://x.com/hesap",
        }).IsSuccess);

        Assert.True(Kaydet(galeri, new()
        {
            ["instagram"] = "  https://www.instagram.com/yeni ",
            ["youtube"] = string.Empty,
            ["x"] = "   ",
        }).IsSuccess);

        SocialLink hesap = Assert.Single(galeri.SocialLinks);
        Assert.Equal(("instagram", "https://www.instagram.com/yeni"), (hesap.Platform, hesap.Url));

        Assert.True(Kaydet(galeri, []).IsSuccess);
        Assert.Empty(galeri.SocialLinks);
    }

    [Fact(DisplayName = "0024/AC-22, AC-71, AC-21: `null` küme kayıtlı hesapları değiştirmez; her başarılı kaydetme sürümü bir artırır.")]
    public void Degistirmez_NullKume()
    {
        Gallery galeri = Galeri();
        Assert.True(Kaydet(galeri, new() { ["instagram"] = "https://www.instagram.com/kalir" }).IsSuccess);
        Assert.Equal(3, galeri.Version);

        Assert.True(galeri.UpdateProfile("Oto Galeri", null, null, new Dictionary<Locale, string?> { [Tr] = "Oto" }, Tr, socialLinkInput: null).IsSuccess);

        Assert.Equal("https://www.instagram.com/kalir", Assert.Single(galeri.SocialLinks).Url);
        Assert.Equal(4, galeri.Version);
    }

    [Fact(DisplayName = "0024/AC-34: D1'in sırası girilme sırasından bağımsızdır.")]
    public void Siralar_D1eGore()
    {
        Gallery galeri = Galeri();
        Assert.True(Kaydet(galeri, new() { ["youtube"] = "https://www.youtube.com/@k" }).IsSuccess);
        Assert.True(Kaydet(galeri, new() { ["youtube"] = "https://www.youtube.com/@k", ["instagram"] = "https://www.instagram.com/i" }).IsSuccess);

        Assert.Equal(["instagram", "youtube"], SocialPlatform.InOrder(galeri.SocialLinks).Select(link => link.Platform));
    }

    private static string AsciiAdres(int bayt)
    {
        const string Kok = "https://www.instagram.com/";
        return Kok + new string('a', bayt - Kok.Length);
    }

    private static Result Kaydet(Gallery galeri, Dictionary<string, string?> hesaplar) =>
        galeri.UpdateProfile("Oto Galeri", null, null, new Dictionary<Locale, string?> { [Tr] = "Oto" }, Tr, hesaplar);

    private static Gallery Galeri()
    {
        // Görünen ad zorunlu (0020 AC-5): kurulum hâli bir kaydetmeyle kurulur, sürüm 2.
        Gallery galeri = Gallery.Create("Oto Galeri").Value;
        Assert.True(galeri.UpdateProfile("Oto Galeri", null, null, new Dictionary<Locale, string?> { [Tr] = "Oto" }, Tr).IsSuccess);
        return galeri;
    }
}
