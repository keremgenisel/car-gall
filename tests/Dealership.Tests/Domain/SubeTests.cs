using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Domain.Events;

namespace CarGallery.Modules.Dealership.Tests.Domain;

public sealed class SubeTests
{
    private static readonly Locale VarsayilanLocale = Locale.Create("tr").Value;

    [Fact(DisplayName = "0003/AC-3: Bir şube; görünen ad, adres, koordinat, telefon, WhatsApp numarası, e-posta, saat dilimi, durum, slug ve görsel medya referansları alanlarıyla kaydedilir")]
    public void Kaydedilir_TumAlanlarla()
    {
        Branch sube = Ac().Value;
        sube.SetCoordinate(Coordinate.Create(40.99m, 29.02m).Value);
        sube.SetWhatsApp(PhoneNumber.Create("+905321112233").Value);
        sube.SetEmail(EmailAddress.Create("kadikoy@otogaleri.com.tr").Value);
        sube.AttachMediaAsset(Guid.CreateVersion7());

        Assert.Equal("kadikoy-subesi", sube.Slug.Value);
        Assert.Equal("Europe/Istanbul", sube.TimeZone.Value);
        Assert.Equal(BranchStatus.Active, sube.Status);
        Assert.NotNull(sube.Coordinate);
        Assert.NotNull(sube.WhatsApp);
        Assert.NotNull(sube.Email);
        Assert.Single(sube.MediaAssetIds);
    }

    [Fact(DisplayName = "0003/AC-4: Şubenin görünen adı ve adresi locale başına saklanır; aynı şube için TR ve AR adları birbirinden bağımsız okunur")]
    public void BagimsizOkunur_TrVeArAdlari()
    {
        Branch sube = Ac(Adlar("Kadıköy Şubesi", "فرع كاديكوي")).Value;

        Assert.Equal("Kadıköy Şubesi", sube.Names.Resolve(VarsayilanLocale, VarsayilanLocale));
        Assert.Equal("فرع كاديكوي", sube.Names.Resolve(Locale.Create("ar").Value, VarsayilanLocale));
    }

    [Fact(DisplayName = "0003/AC-5: Ziyaretçinin locale'inde karşılığı olmayan bir şube adı istendiğinde varsayılan locale'deki ad döner; boş değer dönmez")]
    public void VarsayilanaDuser_CevirisiOlmayanLocale()
    {
        Branch sube = Ac(Adlar("Kadıköy Şubesi", null)).Value;

        string? ad = sube.Names.Resolve(Locale.Create("ar").Value, VarsayilanLocale);

        Assert.Equal("Kadıköy Şubesi", ad);
    }

    [Fact(DisplayName = "0003/AC-6: Varsayılan locale'deki görünen adı veya adresi olmayan bir şube kaydedilemez")]
    public void Kaydedilemez_VarsayilanLocaleAdiYoksa()
    {
        LocalizedText yalnizArapca = LocalizedText.From(
            [new KeyValuePair<Locale, string>(Locale.Create("ar").Value, "فرع كاديكوي")]);

        Result<Branch> sonuc = Ac(yalnizArapca);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.DefaultLocaleNameRequired, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0003/AC-7: Koordinat, WhatsApp numarası ve e-posta zorunlu değildir; üçü de boş bırakılarak bir şube kaydedilebilir")]
    public void Kaydedilir_OpsiyonelAlanlarBosken()
    {
        Branch sube = Ac().Value;

        Assert.Null(sube.Coordinate);
        Assert.Null(sube.WhatsApp);
        Assert.Null(sube.Email);
    }

    [Fact(DisplayName = "0003/AC-16: Galerideki son aktif şube kapatılamaz; girişim reddedilir")]
    public void Kapatilamaz_SonAktifSube()
    {
        Branch sube = Ac().Value;

        Result sonuc = sube.Close(otherActiveBranchCount: 0);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.LastActiveBranch, sonuc.Error.Code);
        Assert.Equal(BranchStatus.Active, sube.Status);
    }

    [Fact(DisplayName = "0003/AC-17: İki aktif şubeden biri kapatıldığında işlem başarılı olur ve geriye bir aktif şube kalır")]
    public void Kapatilir_BaskaAktifSubeVarken()
    {
        Branch sube = Ac().Value;

        Result sonuc = sube.Close(otherActiveBranchCount: 1);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(BranchStatus.Closed, sube.Status);
    }

    [Fact(DisplayName = "0003/AC-18: Bir şubenin geçici kapalı yapılması, o şube tek aktif şube olsa bile reddedilmez")]
    public void Reddedilmez_TekAktifSubeGeciciKapali()
    {
        Branch sube = Ac().Value;

        Result sonuc = sube.CloseTemporarily();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(BranchStatus.TemporarilyClosed, sube.Status);
    }

    [Fact(DisplayName = "0003/AC-19: Kapatılmış bir şube yeniden aktif edilemez; girişim reddedilir")]
    public void YenidenAktifEdilemez_KapatilmisSube()
    {
        Branch sube = Ac().Value;
        sube.Close(otherActiveBranchCount: 1);

        Result sonuc = sube.Reopen();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(BranchStatus.Closed, sube.Status);
    }

    [Fact]
    public void GeriDoner_GeciciKapaliSube()
    {
        Branch sube = Ac().Value;
        sube.CloseTemporarily();

        Assert.True(sube.Reopen().IsSuccess);
        Assert.Equal(BranchStatus.Active, sube.Status);
    }

    [Fact(DisplayName = "0003/AC-20: Yeni bir şube açıldığında BranchOpened duyurusu üretilir ve şube kimliğiyle locale başına adları taşır")]
    public void OlayUretilir_SubeAcildiginda()
    {
        Branch sube = Ac().Value;

        Assert.Single(sube.DomainEvents.OfType<BranchWasOpened>());
    }

    [Fact(DisplayName = "0003/AC-21: Şubenin görünen adı değiştiğinde BranchDetailsChanged duyurusu üretilir ve locale başına güncel adları taşır")]
    public void OlayUretilir_GorunenAdDegistiginde()
    {
        Branch sube = Ac().Value;
        sube.ClearDomainEvents();

        sube.ChangeDisplayNames(Adlar("Bostancı Şubesi", "فرع بوستانجي"), VarsayilanLocale);

        Assert.Single(sube.DomainEvents.OfType<BranchDisplayNameWasChanged>());
        Assert.Equal("Bostancı Şubesi", sube.Names.Resolve(VarsayilanLocale, VarsayilanLocale));
    }

    [Fact(DisplayName = "0003/AC-22: Şubenin adresi, telefonu, koordinatı, saat dilimi veya slug'ı değiştiğinde BranchDetailsChanged duyurusu üretilmez")]
    public void OlayUretilmez_AdDisindakiAlanlarDegistiginde()
    {
        Branch sube = Ac().Value;
        sube.ClearDomainEvents();

        sube.ChangeAddresses(Adlar("Yeni adres", "عنوان جديد"), VarsayilanLocale);
        sube.ChangePhone(PhoneNumber.Create("+902169998877").Value);
        sube.SetCoordinate(Coordinate.Create(41.0m, 29.0m).Value);
        sube.ChangeTimeZone(TimeZoneId.Create("Asia/Kuwait").Value);
        sube.ChangeSlug(BranchSlug.Create("bostanci-subesi").Value);

        Assert.Empty(sube.DomainEvents);
    }

    [Fact(DisplayName = "0003/AC-23: Bir şube kapatılmış durumuna geçtiğinde BranchDeactivated duyurusu üretilir")]
    public void OlayUretilir_SubeKapatildiginda()
    {
        Branch sube = Ac().Value;
        sube.ClearDomainEvents();

        sube.Close(otherActiveBranchCount: 1);

        Assert.Single(sube.DomainEvents.OfType<BranchWasClosed>());
    }

    [Fact(DisplayName = "0003/AC-24: Bir şube geçici kapalı yapıldığında BranchDeactivated duyurusu üretilmez")]
    public void OlayUretilmez_GeciciKapaliYapildiginda()
    {
        Branch sube = Ac().Value;
        sube.ClearDomainEvents();

        sube.CloseTemporarily();

        Assert.Empty(sube.DomainEvents);
    }

    private static LocalizedText Adlar(string tr, string? ar)
    {
        List<KeyValuePair<Locale, string>> degerler =
            [new KeyValuePair<Locale, string>(VarsayilanLocale, tr)];

        if (ar is not null)
        {
            degerler.Add(new KeyValuePair<Locale, string>(Locale.Create("ar").Value, ar));
        }

        return LocalizedText.From(degerler);
    }

    private static Result<Branch> Ac(LocalizedText? adlar = null) => Branch.Open(
        adlar ?? Adlar("Kadıköy Şubesi", "فرع كاديكوي"),
        Adlar("Bağdat Caddesi No:1", "شارع بغداد ١"),
        PhoneNumber.Create("+902165554433").Value,
        TimeZoneId.Create("Europe/Istanbul").Value,
        BranchSlug.Create("kadikoy-subesi").Value,
        VarsayilanLocale);
}
