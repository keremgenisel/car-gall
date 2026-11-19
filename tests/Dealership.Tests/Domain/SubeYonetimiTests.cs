using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Domain.Events;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0020` R9–R15 — şubenin alan kuralları, duyuru yolu ve durum geçişleri; veritabanı yok.
/// Komutun ve ucun aynı kuralları uyguladığı `Integration/SubeYonetimiKomutTests` ve
/// `Host.Tests/GaleriYonetimiZinciriTests`'te ayrıca ölçülür.
/// </summary>
public sealed class SubeYonetimiTests
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;

    [Fact(DisplayName = "0020/AC-24: Varsayılan dildeki adı boş olan şube açılamaz ve hata ad alanının yanında gösterilir.")]
    public void Acilamaz_VarsayilanDildekiAdBossa()
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(Girdi() with { Names = Metin((Tr, " "), (Ar, "فرع")) }, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.DefaultLocaleNameRequired, sonuc.Error.Parameters!["name.tr"]);
    }

    [Fact(DisplayName = "0020/AC-25: Varsayılan dildeki adresi boş olan şube açılamaz.")]
    public void Acilamaz_VarsayilanDildekiAdresBossa()
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(Girdi() with { Addresses = Metin((Ar, "عنوان")) }, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.DefaultLocaleAddressRequired, sonuc.Error.Parameters!["address.tr"]);
    }

    [Theory(DisplayName = "0020/AC-26: Uluslararası biçimde olmayan bir telefon (`02165554433`) reddedilir.")]
    [InlineData("phone")]
    [InlineData("whatsApp")]
    public void Reddedilir_UluslararasiOlmayanTelefon(string alan)
    {
        BranchInput girdi = alan == "phone" ? Girdi() with { Phone = "02165554433" } : Girdi() with { WhatsApp = "02165554433" };

        Result<BranchDraft> sonuc = BranchDraft.Parse(girdi, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(PhoneNumber.InvalidFormat, sonuc.Error.Parameters![alan]);
    }

    [Theory(DisplayName = "0020/AC-27: IANA bölge kimliği olmayan bir saat dilimi (`+03:00`) sunucuda reddedilir.")]
    [InlineData("+03:00")]
    [InlineData("UTC")]
    [InlineData("Europe/Atlantis")]
    public void Reddedilir_IanaOlmayanSaatDilimi(string saatDilimi)
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(Girdi() with { TimeZone = saatDilimi }, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(TimeZoneId.InvalidZone, sonuc.Error.Parameters!["timeZone"]);
    }

    [Theory(DisplayName = "0020/AC-29: Küçük harfli ASCII olmayan bir adres kısaltması (`kadıköy-şubesi`) reddedilir.")]
    [InlineData("kadıköy-şubesi")]
    [InlineData("Kadikoy")]
    [InlineData("kadikoy subesi")]
    public void Reddedilir_KucukHarfliAsciiOlmayanKisaltma(string kisaltma)
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(Girdi() with { Slug = kisaltma }, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(BranchSlug.InvalidFormat, sonuc.Error.Parameters!["slug"]);
    }

    [Fact(DisplayName = "0020/AC-31: Geçersiz bir e-posta (`kadikoy@`) reddedilir.")]
    public void Reddedilir_GecersizEposta()
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(Girdi() with { Email = "kadikoy@" }, Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(EmailAddress.InvalidFormat, sonuc.Error.Parameters!["email"]);
    }

    [Fact(DisplayName = "0020/AC-33: 120 karakteri aşan şube adı ve 300 karakteri aşan adres alan hatasıyla reddedilir (R3a, D22).")]
    public void Reddedilir_SiniriAsanAdVeAdres()
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(
            Girdi() with
            {
                Names = Metin((Tr, "Kadıköy"), (Ar, new string('ف', 121))),
                Addresses = Metin((Tr, new string('a', 301))),
            },
            Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(BranchDraft.NameTooLong, sonuc.Error.Parameters!["name.ar"]);
        Assert.Equal(BranchDraft.AddressTooLong, sonuc.Error.Parameters!["address.tr"]);
    }

    [Fact(DisplayName = "0020/AC-33: 120 karakteri aşan şube adı ve 300 karakteri aşan adres alan hatasıyla reddedilir (R3a, D22).")]
    public void Kabul_SinirdakiAdVeAdres()
    {
        Result<BranchDraft> sonuc = BranchDraft.Parse(
            Girdi() with { Names = Metin((Tr, new string('a', 120))), Addresses = Metin((Tr, new string('a', 300))) },
            Tr);

        Assert.True(sonuc.IsSuccess);
    }

    [Fact(DisplayName = "0020/AC-35: Yalnız Arapça adı değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.")]
    public void TekDuyuru_YalnizArapcaAdDegistiginde()
    {
        Branch sube = Ac();

        Assert.True(sube.Update(Taslak(Girdi() with { Names = Metin((Tr, "Kadıköy Şubesi"), (Ar, "فرع جديد")) }), Tr).IsSuccess);

        Assert.Single(sube.DomainEvents.OfType<BranchDisplayNameWasChanged>());
    }

    [Fact(DisplayName = "0020/AC-36: Ad ve adres birlikte değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.")]
    public void TekDuyuru_AdVeAdresBirlikteDegistiginde()
    {
        Branch sube = Ac();

        Assert.True(sube.Update(
            Taslak(Girdi() with { Names = Metin((Tr, "Yeni Ad")), Addresses = Metin((Tr, "Yeni Adres")) }),
            Tr).IsSuccess);

        Assert.Single(sube.DomainEvents.OfType<BranchDisplayNameWasChanged>());
    }

    [Theory(DisplayName = "0020/AC-37: Adı değişmeden yalnız adresi, telefonu, saat dilimi ya da kısaltması değiştirilen kaydetme **hiçbir** duyuru yazmaz.")]
    [InlineData("adres")]
    [InlineData("telefon")]
    [InlineData("saat")]
    [InlineData("kisaltma")]
    public void DuyuruYok_AdDegismedenKaydedildiginde(string alan)
    {
        Branch sube = Ac();
        BranchInput girdi = alan switch
        {
            "adres" => Girdi() with { Addresses = Metin((Tr, "Başka Adres")) },
            "telefon" => Girdi() with { Phone = "+902169998877" },
            "saat" => Girdi() with { TimeZone = "Asia/Kuwait" },
            _ => Girdi() with { Slug = "baska-kisaltma" },
        };

        Assert.True(sube.Update(Taslak(girdi), Tr).IsSuccess);

        Assert.Empty(sube.DomainEvents.OfType<BranchDisplayNameWasChanged>());
    }

    [Fact(DisplayName = "0020/AC-39: WhatsApp numarası, e-posta ve koordinat boşaltılıp kaydedildiğinde üçü de boş kaydedilir.")]
    public void BosKaydedilir_WhatsAppEpostaVeKoordinat()
    {
        Branch sube = Ac(Girdi() with { WhatsApp = "+905321112233", Email = "kadikoy@ornek.com", Latitude = 40.9m, Longitude = 29.0m });
        Assert.NotNull(sube.WhatsApp);

        Assert.True(sube.Update(Taslak(Girdi() with { WhatsApp = " ", Email = null, Latitude = null, Longitude = null }), Tr).IsSuccess);

        Assert.Null(sube.WhatsApp);
        Assert.Null(sube.Email);
        Assert.Null(sube.Coordinate);
    }

    [Fact(DisplayName = "0020/AC-42: Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder (R10, D21).")]
    public void Duzenlenemez_KapatilmisSube()
    {
        Branch sube = Ac();
        Assert.True(sube.Close(otherActiveBranchCount: 1).IsSuccess);

        Result sonuc = sube.Update(Taslak(Girdi() with { Names = Metin((Tr, "Değişmeli Miydi")) }), Tr);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.ClosedReadOnly, sonuc.Error.Code);
        Assert.Equal("Kadıköy Şubesi", sube.Names.Resolve(Tr, Tr));
    }

    [Fact(DisplayName = "0020/AC-44: Galerinin tek aktif şubesi de geçici kapatılabilir.")]
    public void GeciciKapatilir_TekAktifSube()
    {
        Branch sube = Ac();

        Assert.True(sube.CloseTemporarily().IsSuccess);
        Assert.Equal(BranchStatus.TemporarilyClosed, sube.Status);
        Assert.Empty(sube.DomainEvents.OfType<BranchWasClosed>());
    }

    [Fact(DisplayName = "0020/AC-50: Galerinin son aktif şubesini kalıcı kapatma reddedilir; panel bunun nedenini söyler ve şube aktif kalır.")]
    public void Reddedilir_SonAktifSubeKalici()
    {
        Branch sube = Ac();

        Result sonuc = sube.Close(otherActiveBranchCount: 0);

        Assert.Equal(Branch.LastActiveBranch, sonuc.Error.Code);
        Assert.Equal(BranchStatus.Active, sube.Status);
    }

    [Fact(DisplayName = "0020/AC-51: Galerinin tek aktif şubesi önce geçici kapatılıp ardından kalıcı kapatılmak istendiğinde kalıcı kapatma reddedilir ve şube geçici kapalı kalır (D13).")]
    public void Reddedilir_GeciciKapatilanTekAktifSubeKalici()
    {
        Branch sube = Ac();
        Assert.True(sube.CloseTemporarily().IsSuccess);

        Result sonuc = sube.Close(otherActiveBranchCount: 0);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.LastActiveBranch, sonuc.Error.Code);
        Assert.Equal(BranchStatus.TemporarilyClosed, sube.Status);
        Assert.Empty(sube.DomainEvents.OfType<BranchWasClosed>());
    }

    [Fact(DisplayName = "0020/AC-52: Başka bir aktif şube varken geçici kapalı bir şube kalıcı kapatılabilir (D21).")]
    public void Kapatilir_GeciciKapali_BaskaAktifVarken()
    {
        Branch sube = Ac();
        Assert.True(sube.CloseTemporarily().IsSuccess);

        Assert.True(sube.Close(otherActiveBranchCount: 1).IsSuccess);
        Assert.Equal(BranchStatus.Closed, sube.Status);
        Assert.Single(sube.DomainEvents.OfType<BranchWasClosed>());
    }

    [Fact(DisplayName = "0020/AC-54: Kapatılmış bir şube için yeniden açma ve geçici kapatma eylemleri sunulmaz ve uç onları reddeder.")]
    public void Reddedilir_KapatilmisSubedeYenidenAcmaVeGeciciKapatma()
    {
        Branch sube = Ac();
        Assert.True(sube.Close(otherActiveBranchCount: 1).IsSuccess);

        Assert.Equal(Branch.InvalidTransition, sube.Reopen().Error.Code);
        Assert.Equal(Branch.InvalidTransition, sube.CloseTemporarily().Error.Code);
        Assert.Equal(BranchStatus.Closed, sube.Status);
    }

    [Fact]
    public void SurumIlerler_HerDurumEylemindeVeDuzenlemede()
    {
        Branch sube = Ac();
        int once = sube.Version;

        sube.CloseTemporarily();
        sube.Reopen();
        sube.Update(Taslak(Girdi()), Tr);
        sube.Close(otherActiveBranchCount: 1);

        Assert.Equal(once + 4, sube.Version);
    }

    private static Branch Ac(BranchInput? girdi = null) => Branch.Open(Taslak(girdi ?? Girdi()), Tr).Value;

    private static BranchDraft Taslak(BranchInput girdi)
    {
        Result<BranchDraft> taslak = BranchDraft.Parse(girdi, Tr);
        Assert.True(taslak.IsSuccess, taslak.IsFailure ? string.Join(",", taslak.Error.Parameters ?? new Dictionary<string, string>()) : null);
        return taslak.Value;
    }

    private static BranchInput Girdi() => new(
        Metin((Tr, "Kadıköy Şubesi"), (Ar, "فرع كاديكوي")),
        Metin((Tr, "Bağdat Caddesi 1")),
        "+902165554433",
        null,
        null,
        "Europe/Istanbul",
        "kadikoy-subesi",
        null,
        null);

    private static Dictionary<Locale, string?> Metin(params (Locale Locale, string? Deger)[] degerler) =>
        degerler.ToDictionary(item => item.Locale, item => item.Deger);
}
