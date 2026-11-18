using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>`0020` R1–R3a — galeri profilinin alan kuralları; veritabanı yok.</summary>
public sealed class GaleriProfiliTests
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;
    private static readonly Locale En = Locale.Create("en").Value;

    [Theory(DisplayName = "0020/AC-3: Boş ya da yalnız boşluktan oluşan bir ticari unvan reddedilir ve hata alanın yanında gösterilir; kayıtlı unvan değişmez.")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Reddedilir_UnvanBosYaDaYalnizBosluksa(string? unvan)
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile(unvan, "1", "2", Adlar((Tr, "Oto")), Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(FieldErrors.ValidationFailed, sonuc.Error.Code);
        // Hata ALANIN kendisine bağlı: panel onu alanın yanında gösterir.
        Assert.Equal(Gallery.CommercialNameRequired, sonuc.Error.Parameters!["commercialName"]);
        Assert.Equal("Oto Galeri", galeri.CommercialName);
        Assert.Equal(1, galeri.Version);
    }

    [Fact(DisplayName = "0020/AC-4: Vergi numarası ve ticaret sicil numarası boşaltılıp kaydedildiğinde ikisi de boş kaydedilir.")]
    public void BosKaydedilir_VergiVeSicilBosaltildiginda()
    {
        Gallery galeri = Galeri();
        galeri.SetRegistration("1234567890", "TR-123456");

        Result sonuc = galeri.UpdateProfile("Oto Galeri", "  ", string.Empty, Adlar((Tr, "Oto")), Tr);

        Assert.True(sonuc.IsSuccess);
        Assert.Null(galeri.TaxNumber);
        Assert.Null(galeri.TradeRegistryNumber);
    }

    [Theory(DisplayName = "0020/AC-5: Varsayılan dildeki görünen ad boş bırakılarak yapılan kaydetme reddedilir; kayıtlı ad değişmez.")]
    [InlineData("")]
    [InlineData("  ")]
    public void Reddedilir_VarsayilanDildekiGorunenAdBossa(string ad)
    {
        Gallery galeri = Galeri();
        Assert.True(galeri.UpdateProfile("Oto Galeri", null, null, Adlar((Tr, "Eski Ad")), Tr).IsSuccess);

        Result sonuc = galeri.UpdateProfile("Oto Galeri", null, null, Adlar((Tr, ad), (Ar, "أوتو")), Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Gallery.DefaultLocaleDisplayNameRequired, sonuc.Error.Parameters!["displayName.tr"]);
        Assert.Equal("Eski Ad", galeri.DisplayNames.Resolve(Tr, Tr));
    }

    [Fact(DisplayName = "0020/AC-5: Varsayılan dildeki görünen ad boş bırakılarak yapılan kaydetme reddedilir; kayıtlı ad değişmez.")]
    public void Reddedilir_VarsayilanDilAnahtariHicYoksa()
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile("Oto Galeri", null, null, Adlar((Ar, "أوتو")), Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Gallery.DefaultLocaleDisplayNameRequired, sonuc.Error.Parameters!["displayName.tr"]);
    }

    [Fact(DisplayName = "0020/AC-5: Varsayılan dildeki görünen ad boş bırakılarak yapılan kaydetme reddedilir; kayıtlı ad değişmez.")]
    public void Reddedilir_VarsayilanDilTrDegilken()
    {
        // Bilinen tuzak 4 — zorunlu karşılık yapılandırmadaki varsayılan dilinkidir.
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile("Oto Galeri", null, null, Adlar((Tr, "Oto")), Ar);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Gallery.DefaultLocaleDisplayNameRequired, sonuc.Error.Parameters!["displayName.ar"]);
    }

    [Fact(DisplayName = "0020/AC-6: Varsayılan olmayan bir dildeki görünen ad boş bırakılarak kaydedilebilir.")]
    public void Kaydedilir_VarsayilanOlmayanDilBosken()
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile("Oto Galeri", null, null, Adlar((Tr, "Oto"), (Ar, " "), (En, string.Empty)), Tr);

        Assert.True(sonuc.IsSuccess);
        Assert.False(galeri.DisplayNames.HasValueFor(Ar));
        Assert.False(galeri.DisplayNames.HasValueFor(En));
        Assert.Equal(2, galeri.Version);
    }

    [Fact(DisplayName = "0020/AC-8: 200 karakteri aşan ticari unvan, 40 karakteri aşan vergi ya da sicil numarası ve 120 karakteri aşan görünen ad alan hatasıyla reddedilir; `500` dönmez (R3a, D22).")]
    public void Reddedilir_SiniriAsanAlanlar()
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile(
            new string('u', 201),
            new string('1', 41),
            new string('2', 41),
            Adlar((Tr, new string('a', 121)), (Ar, new string('ب', 121))),
            Tr);

        Assert.True(sonuc.IsFailure);
        IReadOnlyDictionary<string, string> alanlar = sonuc.Error.Parameters!;
        Assert.Equal(Gallery.CommercialNameTooLong, alanlar["commercialName"]);
        Assert.Equal(Gallery.TaxNumberTooLong, alanlar["taxNumber"]);
        Assert.Equal(Gallery.TradeRegistryNumberTooLong, alanlar["tradeRegistryNumber"]);
        Assert.Equal(Gallery.DisplayNameTooLong, alanlar["displayName.tr"]);
        Assert.Equal(Gallery.DisplayNameTooLong, alanlar["displayName.ar"]);
    }

    [Fact(DisplayName = "0020/AC-8: 200 karakteri aşan ticari unvan, 40 karakteri aşan vergi ya da sicil numarası ve 120 karakteri aşan görünen ad alan hatasıyla reddedilir; `500` dönmez (R3a, D22).")]
    public void Kaydedilir_SinirdakiDegerler()
    {
        Gallery galeri = Galeri();

        Result sonuc = galeri.UpdateProfile(
            new string('u', 200),
            new string('1', 40),
            new string('2', 40),
            Adlar((Tr, new string('a', 120))),
            Tr);

        Assert.True(sonuc.IsSuccess);
    }

    private static Gallery Galeri() => Gallery.Create("Oto Galeri").Value;

    private static Dictionary<Locale, string?> Adlar(params (Locale Locale, string? Ad)[] adlar) =>
        adlar.ToDictionary(item => item.Locale, item => item.Ad);
}
