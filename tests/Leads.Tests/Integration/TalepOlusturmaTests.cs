using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

[Collection(LeadsDatabaseCollection.Name)]
public sealed class TalepOlusturmaTests(LeadsDatabase database)
{
    // Tip PARAMETRE olarak kod ile geçiyor, enum ile değil: `LeadType` internal'dır ve
    // xUnit'in `[Theory]` imzası public erişim ister. Kodun kendisi zaten sözleşmedir.
    [Theory(DisplayName = "AC-9: Talep tipi bilgi talebi, geri arama, test sürüşü, fiyat teklifi ve kredi görüşmesi değerlerinden birini alır; bu beşi dışında bir tiple talep kaydedilmez")]
    [InlineData("information_request")]
    [InlineData("call_back")]
    [InlineData("test_drive")]
    [InlineData("price_offer")]
    [InlineData("financing_consultation")]
    public async Task Kaydedilir_BesGecerliTipinHerbiriyle(string kod)
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(tip: kod));

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(kod, LeadTypeCode.ToCode((await OkuAsync(sonuc.Value))!.Type));
    }

    [Theory(DisplayName = "AC-9: Talep tipi bilgi talebi, geri arama, test sürüşü, fiyat teklifi ve kredi görüşmesi değerlerinden birini alır; bu beşi dışında bir tiple talep kaydedilmez")]
    [InlineData("randevu")]
    [InlineData("InformationRequest")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Kaydedilmez_TanimsizTiple(string? kod)
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(tip: kod));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.InvalidType, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    /// <summary>
    /// Takas TANINAN bir tiptir: ziyaretçi "böyle bir tip yok" karşılığı almaz, "bu tip
    /// şu an kabul edilmiyor" karşılığı alır. İki kriter (AC-9, AC-10) bu yüzden iki ayrı
    /// koda düşer.
    /// </summary>
    [Fact(DisplayName = "AC-10: Talep tipi takas olarak gönderilen istek reddedilir; takas sistemde tanınan bir tiptir ama bu spec'te kabul edilmez")]
    public async Task Reddedilir_TakasTipiyle()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(tip: "trade_in"));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.TypeNotAccepted, sonuc.Error.Code);
        Assert.NotEqual(LeadErrors.InvalidType, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    [Fact(DisplayName = "AC-12: `Rezerve` durumundaki bir ilan için gönderilen talep sunucuda reddedilir ve kayıt oluşmaz")]
    public async Task Reddedilir_IlanRezerveyse()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.Ilanlar.RezerveYap();

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.ListingReserved, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    [Fact(DisplayName = "AC-13: Vitrinde görünür olmayan bir ilan için gönderilen talep reddedilir ve kayıt oluşmaz")]
    public async Task Reddedilir_IlanVitrindeGorunmuyorsa()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.Ilanlar.VitrindenDusur();

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.ListingUnavailable, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    [Fact(DisplayName = "AC-14: Hiç var olmayan bir ilan için gönderilen talep ile görünür olmayan bir ilan için gönderilen talep aynı karşılığı alır: aynı hata kodu, aynı gövde")]
    public async Task AyniHata_VarOlmayanVeGorunmeyenIlan()
    {
        await database.ResetAsync();

        TalepOlusturmaHarness gorunmeyen = new(database);
        gorunmeyen.Ilanlar.VitrindenDusur();
        Result<Guid> gorunmeyenSonuc = await gorunmeyen.CalistirAsync(TalepOlusturmaHarness.Komut());

        TalepOlusturmaHarness hicYok = new(database);
        Result<Guid> hicYokSonuc = await hicYok.CalistirAsync(
            TalepOlusturmaHarness.Komut(slug: "hic-var-olmayan-ilan-999999"));

        Assert.True(gorunmeyenSonuc.IsFailure);
        Assert.True(hicYokSonuc.IsFailure);
        Assert.Equal(gorunmeyenSonuc.Error, hicYokSonuc.Error);
        await KayitOlusmadiginiDogrulaAsync();
    }

    [Fact(DisplayName = "AC-16: Talep, bırakıldığı locale ile kaydedilir; Arapça sayfadan bırakılan talebin locale'i `ar` olur")]
    public async Task Kaydedilir_ArapcaSayfadanBirakildiginda()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(locale: "ar"));

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("ar", (await OkuAsync(sonuc.Value))!.SubmittedLocale.Code);
    }

    [Theory(DisplayName = "AC-16: Talep, bırakıldığı locale ile kaydedilir; Arapça sayfadan bırakılan talebin locale'i `ar` olur")]
    [InlineData("de")]
    [InlineData("TR")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Kaydedilmez_EtkinOlmayanLocaleIle(string? locale)
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(locale: locale));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.InvalidLocale, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    [Fact(DisplayName = "AC-18: Anlık görüntüdeki model adı kanonik Latin adıdır; Arapça sayfadan bırakılan talepte de kanonik ad saklanır")]
    public async Task Reddedilir_ModelAdiCozulemezse()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.ModelAdlari.Kaybet();

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.ListingUnavailable, sonuc.Error.Code);
        await KayitOlusmadiginiDogrulaAsync();
    }

    /// <summary>
    /// **L2**'nin asıl sorusu: kaynak değişince talep onu İZLİYOR MU? Kaynak burada
    /// gerçekten değişiyor (sahte, use-case'in gördüğü tek kaynaktır) ve talep
    /// veritabanından YENİDEN OKUNUYOR. Talebe yalnız bir kimlik koyup fiyatı okuma
    /// anında çekseydik, bu test ikinci tutarı görürdü (Bilinen tuzak 1).
    /// </summary>
    [Fact(DisplayName = "AC-19: Talep oluşturulduktan sonra aracın fiyatı değiştirildiğinde talepteki fiyat değişmez")]
    public async Task Degismez_AracinFiyatiDegistiginde()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        harness.Ilanlar.FiyatiDegistir(1_180_000m);

        Lead okunan = (await OkuAsync(sonuc.Value))!;

        Assert.Equal(1_250_000m, okunan.Vehicle.PriceAmount);

        // Aynı slug şimdi 1.180.000 diyor: kaynak gerçekten değişti, talep izlemedi.
        Result<Guid> ikinci = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "0192c000-0000-7000-8000-00000000bbbb"));

        Assert.Equal(1_180_000m, (await OkuAsync(ikinci.Value))!.Vehicle.PriceAmount);
        Assert.Equal(1_250_000m, (await OkuAsync(sonuc.Value))!.Vehicle.PriceAmount);
    }

    [Fact(DisplayName = "AC-20: Talep oluşturulduktan sonra araç arşivlendiğinde talep okunabilir kalır ve model adı ile fiyatını göstermeye devam eder")]
    public async Task OkunabilirKalir_AracArsivlendiginde()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        // Araç arşivlendi: vitrin okuma modelinde satırı artık yok, kaynak null döner.
        harness.Ilanlar.VitrindenDusur();
        harness.ModelAdlari.Kaybet();

        Lead? okunan = await OkuAsync(sonuc.Value);

        Assert.NotNull(okunan);
        Assert.Equal("Toyota Corolla", okunan.Vehicle.CanonicalModelName);
        Assert.Equal(1_250_000m, okunan.Vehicle.PriceAmount);
    }

    [Fact(DisplayName = "AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    public async Task Kopyalar_FiyatSorunuzDurumunu()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.Ilanlar.FiyatSorunuzYap();

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Lead okunan = (await OkuAsync(sonuc.Value))!;

        Assert.True(okunan.Vehicle.PriceOnRequest);
        Assert.Null(okunan.Vehicle.PriceAmount);
    }

    [Fact(DisplayName = "AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public async Task Kopyalar_KwdTutarini_Kesmeden()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.Ilanlar.KwdYap(1250.505m);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Lead okunan = (await OkuAsync(sonuc.Value))!;

        Assert.Equal(1250.505m, okunan.Vehicle.PriceAmount);
        Assert.Equal("KWD", okunan.Vehicle.PriceCurrencyCode);
        Assert.Equal(3, okunan.Vehicle.PriceMinorUnitExponent);
    }

    [Fact(DisplayName = "AC-23: Araca bağlı talep, o aracın şubesine bağlanır")]
    public async Task Baglanir_IlaninSubesine()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        Assert.Equal(FakeListingSnapshotSource.SubeId, (await OkuAsync(sonuc.Value))!.BranchId);
    }

    [Fact(DisplayName = "AC-31: Talep kaydedilmediğinde kaydedilmiş bir `LeadCaptured` duyurusu kalmaz")]
    public async Task Duyurulmaz_TalepReddedildiginde()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);
        harness.Ilanlar.VitrindenDusur();

        await harness.CalistirAsync(TalepOlusturmaHarness.Komut());

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Leads.CountAsync());
        Assert.Equal(0, await context.OutboxMessages.CountAsync());
    }

    private async Task KayitOlusmadiginiDogrulaAsync()
    {
        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Leads.CountAsync());
    }

    private async Task<Lead?> OkuAsync(Guid id)
    {
        await using LeadsDbContext context = database.CreateContext();

        return await context.Leads.AsNoTracking().SingleOrDefaultAsync(lead => lead.Id == new LeadId(id));
    }
}
