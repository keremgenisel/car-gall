using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// AC-32'nin cümlesindeki <b>"kaydedildikten sonra"</b>. Eski uygulamada bütçeyi boru
/// hattındaki istek sınırı tüketiyordu, yani İSTEK sayılıyordu: sunucu doğrulamasına
/// düşen dürüst bir ziyaretçi beş denemede kilitlenir ve hiç talep bırakamazdı.
/// Buradaki testler sayacın yalnız KAYIT OLUŞAN istekte ilerlediğini sınıyor.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class HizSiniriTests(LeadsDatabase database)
{
    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Tuketilmez_ReddedilenIsteklerde()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        // Beş istek sunucu doğrulamasına düşüyor: adsız gövde.
        for (int sira = 1; sira <= 5; sira++)
        {
            Result<Guid> reddedilen = await harness.CalistirAsync(
                TalepOlusturmaHarness.Komut(ad: null, anahtar: $"gecersiz-{sira}"));

            Assert.True(reddedilen.IsFailure);
            Assert.Equal(LeadErrors.MissingName, reddedilen.Error.Code);
        }

        // Altıncı istek TAMAMEN GEÇERLİ. Hiç talep kaydedilmediği için bütçe dolu.
        Result<Guid> gecerli = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "gecerli-1"));

        Assert.True(gecerli.IsSuccess);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Reddedilir_AltinciTalep_BesTalepKaydedildiktenSonra()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        for (int sira = 1; sira <= 5; sira++)
        {
            Result<Guid> kabul = await harness.CalistirAsync(
                TalepOlusturmaHarness.Komut(anahtar: $"kabul-{sira}"));

            Assert.True(kabul.IsSuccess);
        }

        Result<Guid> altinci = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "kabul-6"));

        Assert.True(altinci.IsFailure);
        Assert.Equal(LeadErrors.RateLimited, altinci.Error.Code);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(5, await context.Leads.CountAsync());
    }

    /// <summary>
    /// Sayaç İSTEMCİ BAŞINADIR: bir ziyaretçinin bütçesini bitirmesi ötekini etkilemez.
    /// </summary>
    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Etkilenmez_BaskaIstemci()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        for (int sira = 1; sira <= 5; sira++)
        {
            await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: $"ilk-{sira}"));
        }

        Result<Guid> baskaIstemci = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "ikinci-1", istemci: "198.51.100.4"));

        Assert.True(baskaIstemci.IsSuccess);
    }

    /// <summary>
    /// Aynı anahtarla gelen ikinci istek KAYIT OLUŞTURMAZ (AC-35); bu yüzden bütçeyi de
    /// tüketmemelidir. Aksi hâlde uçtaki "tekrar dene" ziyaretçinin hakkını yerdi.
    /// </summary>
    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task Tuketilmez_AyniAnahtarliIkinciIstek()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        for (int sira = 1; sira <= 5; sira++)
        {
            await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "tekrar"));
        }

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());

        // Bütçeden yalnız BİR hak gitti: dört tane daha kaydedilebilir.
        for (int sira = 2; sira <= 5; sira++)
        {
            Result<Guid> kabul = await harness.CalistirAsync(
                TalepOlusturmaHarness.Komut(anahtar: $"farkli-{sira}"));

            Assert.True(kabul.IsSuccess);
        }

        Assert.Equal(5, await database.CreateContext().Leads.CountAsync());
    }

    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Yenilenir_PencereDolunca()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        for (int sira = 1; sira <= 5; sira++)
        {
            await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: $"pencere-{sira}"));
        }

        Assert.True((await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "pencere-6"))).IsFailure);

        harness.Saat.Advance(TimeSpan.FromMinutes(10));

        Result<Guid> yeniPencere = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "pencere-7"));

        Assert.True(yeniPencere.IsSuccess);
    }
}
