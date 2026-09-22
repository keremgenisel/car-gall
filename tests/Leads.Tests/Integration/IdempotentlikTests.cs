using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// R17, Bilinen tuzak 8 — ağ hatası isteğin sunucuya ulaşmasından SONRA olduysa, uçtaki
/// "tekrar dene" ikinci bir talep yaratır. Hız sınırı bunu yakalamaz: iki istek eşiğin
/// çok altındadır.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class IdempotentlikTests(LeadsDatabase database)
{
    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task IkinciKayitOlusmaz_AyniAnahtarla()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> ilk = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "0192c000-0000-7000-8000-00000000dddd"));

        Result<Guid> ikinci = await harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "0192c000-0000-7000-8000-00000000dddd"));

        Assert.True(ilk.IsSuccess);
        Assert.True(ikinci.IsSuccess);
        Assert.Equal(ilk.Value, ikinci.Value);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());

        // Duyuru da bir kez üretilir: ikinci istek hiç yazma yapmadığı için outbox'a da
        // dokunmaz (AC-30 ile AC-35 birlikte).
        Assert.Equal(1, await context.OutboxMessages.CountAsync());
    }

    /// <summary>
    /// İki eşzamanlı istek yukarıdaki okumayı ikisi de geçebilir; yarışı SELECT değil,
    /// tekil indeks kapatır. Bu testin sahnelediği durum tam olarak budur.
    /// </summary>
    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task IkinciKayitOlusmaz_EszamanliIkiIstek()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Task<Result<Guid>> ilk = harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "0192c000-0000-7000-8000-00000000eeee"));

        Task<Result<Guid>> ikinci = harness.CalistirAsync(
            TalepOlusturmaHarness.Komut(anahtar: "0192c000-0000-7000-8000-00000000eeee"));

        Result<Guid>[] sonuclar = await Task.WhenAll(ilk, ikinci);

        Assert.All(sonuclar, sonuc => Assert.True(sonuc.IsSuccess));
        Assert.Equal(sonuclar[0].Value, sonuclar[1].Value);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task IkiKayitOlusur_FarkliAnahtarlarla()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "anahtar-1"));
        await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "anahtar-2"));

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(2, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "0005/AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task Reddedilir_AnahtarsizIstek()
    {
        await database.ResetAsync();
        TalepOlusturmaHarness harness = new(database);

        Result<Guid> sonuc = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: null));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.MissingIdempotencyKey, sonuc.Error.Code);
    }
}
