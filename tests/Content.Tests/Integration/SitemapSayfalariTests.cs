using CarGallery.Modules.Content.Application;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>
/// `0004` **D4** · **P9** — sitemap'in içerik adresleri, ziyaretçinin açabildiği sayfalarla
/// aynı kuraldan. Uçtan uca kriterler (AC-56, AC-61, AC-89) `Host.Tests/SitemapTests`'te.
/// </summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class SitemapSayfalariTests(ContentDatabase database) : IAsyncLifetime
{
    private readonly Icerik icerik = new(database);

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Doner_YalnizODildeYayindakiSayfalari()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("privacy"), "tr", "KVKK", "x", "kvkk");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("privacy"), "en", "Privacy", "x", "privacy");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazAsync(Icerik.Sistem("cookies"), new PageTextInput("tr", "Çerez", "x", "cerez"));

        Assert.Equal(["hakkimizda", "kvkk"], await ListAsync("tr"));
        Assert.Equal(["privacy"], await ListAsync("en"));
        Assert.Empty(await ListAsync("ar"));
    }

    [Fact]
    public async Task Dahil_Etmez_GirdisizSss()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "SSS", "x", "sss");
        await icerik.SssAsync("A");

        Assert.Empty(await ListAsync("tr"));

        Guid a = await icerik.SssAsync("B");
        int surum = await icerik.SssSurumAsync(a);
        await icerik.KomutAsync(k => k.PublishFaqEntryAsync(a, surum, "tr", CancellationToken.None));

        Assert.Equal(["sss"], await ListAsync("tr"));
    }

    [Fact]
    public async Task BosDoner_EtkinOlmayanDilde() =>
        Assert.Empty(await ListAsync("de"));

    private Task<IReadOnlyList<string>> ListAsync(string locale) =>
        icerik.VitrinAsync(q => q.ListVisibleSlugsAsync(locale, CancellationToken.None));
}
