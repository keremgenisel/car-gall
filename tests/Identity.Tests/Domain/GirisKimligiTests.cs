using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

public sealed class GirisKimligiTests
{
    [Fact(DisplayName = "AC-11: `Ali@x.com` kimliğiyle eklenen personel `ali@x.com` yazarak giriş yapar")]
    public void EslesirNormalizeDeger_FarkliBuyukKucukHarfle()
    {
        LoginId eklenen = LoginId.Create("Ali@x.com").Value;
        LoginId girilen = LoginId.Create("ali@x.com").Value;

        Assert.Equal("Ali@x.com", eklenen.Value);
        Assert.Equal(eklenen.Normalized, girilen.Normalized);
    }

    [Fact(DisplayName = "AC-12: `Ali@x.com` kayıtlıyken `ali@x.com` ile ikinci bir personel eklenemez")]
    public void AyniNormalizeDeger_IkiYazimIcin()
    {
        Assert.Equal(
            LoginId.Create("Ali@x.com").Value.Normalized,
            LoginId.Create("ALI@X.COM").Value.Normalized);
    }

    /// <summary>
    /// Türkçe collation altında `lower()` `I → ı` üretir. Kültüre duyarlı bir
    /// normalizasyonda `İSMAIL@x.com` ile `ismail@x.com` çakışır ya da `ALI@x.com` ile
    /// `ali@x.com` ayrı hesaplara düşer. Bedeli bir liste hatası değil, İKİ KİŞİNİN AYNI
    /// HESABA DÜŞMESİDİR (Yasak Listesi 27, Tuzak 9).
    /// </summary>
    [Fact(DisplayName = "AC-13: `İ` içeren bir giriş kimliği kendi yazımıyla eşleşir ve `i` içeren başka bir hesaba düşmez")]
    public void Karismaz_TurkceNoktaliBuyukI()
    {
        LoginId noktali = LoginId.Create("İsmail@x.com").Value;
        LoginId noktasiz = LoginId.Create("ismail@x.com").Value;

        // `İ`nin kültüre duyarsız küçüğü `i̇` (i + birleşik nokta), `i` DEĞİLDİR.
        Assert.NotEqual(noktali.Normalized, noktasiz.Normalized);

        // Kendi yazımıyla eşleşir.
        Assert.Equal(noktali.Normalized, LoginId.Create("İSMAIL@x.com").Value.Normalized);
    }

    [Fact(DisplayName = "AC-13: `İ` içeren bir giriş kimliği kendi yazımıyla eşleşir ve `i` içeren başka bir hesaba düşmez")]
    public void Karismaz_TurkceNoktasizKucukI()
    {
        // `I`nın kültüre duyarsız küçüğü `i`dir; Türkçe kültürde `ı` olurdu ve
        // `ALI@x.com` ile `ali@x.com` AYRI hesaplara düşerdi.
        Assert.Equal(
            LoginId.Create("ALI@x.com").Value.Normalized,
            LoginId.Create("ali@x.com").Value.Normalized);

        Assert.NotEqual(
            LoginId.Create("alı@x.com").Value.Normalized,
            LoginId.Create("ali@x.com").Value.Normalized);
    }

    [Theory(DisplayName = "AC-14: Geçerli biçimde olmayan bir e-posta ile personel eklenemez")]
    [InlineData("ayse")]
    [InlineData("ayse@")]
    [InlineData("@ornek.com")]
    [InlineData("ayse@ornek")]
    [InlineData("ayse ornek@ornek.com")]
    [InlineData("ayse@@ornek.com")]
    [InlineData("")]
    [InlineData(null)]
    public void Reddedilir_GecersizBicim(string? deger)
    {
        Result<LoginId> sonuc = LoginId.Create(deger);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidLoginId, sonuc.Error.Code);
    }

    [Fact(DisplayName = "AC-14: Geçerli biçimde olmayan bir e-posta ile personel eklenemez")]
    public void Reddedilir_UstSiniriAsanKimlik()
    {
        Result<LoginId> sonuc = LoginId.Create($"{new string('a', 250)}@ornek.com");

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidLoginId, sonuc.Error.Code);
    }
}
