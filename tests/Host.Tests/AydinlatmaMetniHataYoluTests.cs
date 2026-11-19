using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0019` R10 — talep anındaki yayın durumu okunamıyor ya da tutarsızsa talep YAZILMAZ ve
/// hiçbir talebe "yayında değildi" yazılmaz (D9, D14, D25). Bozuk durumlar gerçek
/// veritabanında kurulur — sayfa satırı silinir, sürümsüz bir yayın yazılır, okumanın
/// dokunduğu tablo yerinden alınır — ve her test kendi taze ana bilgisayarını (taze hız
/// bütçesi) kullanır.
/// </summary>
public sealed class AydinlatmaMetniHataYoluTests : IAsyncLifetime
{
    private AydinlatmaMetniOrtami ortam = null!;

    public async Task InitializeAsync() => ortam = await AydinlatmaMetniOrtami.KurAsync();

    public async Task DisposeAsync() => await ortam.DisposeAsync();

    [Fact(DisplayName = "0019/AC-32: privacy sayfa satırı yokken bırakılan talep kaydedilmez ve hiçbir talebe \"yayında değildi\" yazılmaz")]
    public async Task Kaydedilmez_SayfaSatiriYokken()
    {
        HttpClient client = ortam.Fabrika().CreateClient();
        long once = await ortam.SayAsync("SELECT count(*) FROM leads.leads");
        await ortam.SqlAsync($"DELETE FROM content.pages WHERE id = '{AydinlatmaMetniOrtami.Privacy}'");

        try
        {
            HttpResponseMessage yanit = await AydinlatmaMetniOrtami.TalepAsync(client, "tr", "ac32");

            Assert.Equal(HttpStatusCode.InternalServerError, yanit.StatusCode);
            Assert.Equal(once, await ortam.SayAsync("SELECT count(*) FROM leads.leads"));
        }
        finally
        {
            await ortam.SqlAsync(
                $"INSERT INTO content.pages (id, system_key, version, created_at) VALUES ('{AydinlatmaMetniOrtami.Privacy}', 'privacy', 1, '2026-11-18T00:00:00Z')");
        }
    }

    [Fact(DisplayName = "0019/AC-33: tr yayındayken tr için hiç sürüm yoksa bırakılan tr talebi kaydedilmez")]
    public async Task Kaydedilmez_YayindaAmaSurumuYokken()
    {
        HttpClient client = ortam.Fabrika().CreateClient();
        await ortam.YeniKurulumAsync();
        long once = await ortam.SayAsync("SELECT count(*) FROM leads.leads");
        long surumler = await ortam.SayAsync("SELECT count(*) FROM content.privacy_notice_versions");

        // C10'un kırıldığı hâl: eski uygulama migration'dan sonra yayınladı — metin yayında,
        // sürümü yok.
        await ortam.SqlAsync(
            $"""
            INSERT INTO content.page_localizations (page_id, locale, title, body, slug, status, published_version_id)
            VALUES ('{AydinlatmaMetniOrtami.Privacy}', 'tr', 'Sürümsüz', 'Sürümsüz yayın.', 'kvkk', 'published', NULL)
            """);

        try
        {
            HttpResponseMessage yanit = await AydinlatmaMetniOrtami.TalepAsync(client, "tr", "ac33");

            Assert.Equal(HttpStatusCode.InternalServerError, yanit.StatusCode);
            Assert.Equal(once, await ortam.SayAsync("SELECT count(*) FROM leads.leads"));
            Assert.Equal(surumler, await ortam.SayAsync("SELECT count(*) FROM content.privacy_notice_versions"));
        }
        finally
        {
            await ortam.YeniKurulumAsync();
        }
    }

    [Fact(DisplayName = "0019/AC-44: Talep anında sürüm okuması hata verdiğinde talep kaydedilmez, hiçbir talebe \"yayında değildi\" yazılmaz, hız bütçesi tüketilmez (aynı istemciden ardından gelen geçerli talepler bütçenin tamamını kullanabilir) ve yanıt 500 application/problem+json'dur; gövdede istisna metni ve yığın izi yoktur")]
    public async Task Tuketmez_Butceyi_OkumaHatasinda()
    {
        WebApplicationFactory<Program> fabrika = ortam.Fabrika();
        HttpClient client = fabrika.CreateClient();
        await ortam.YeniKurulumAsync();
        long once = await ortam.SayAsync("SELECT count(*) FROM leads.leads");

        // Okumanın dokunduğu tablo yerinden alınır: gerçek bir veritabanı hatası.
        await ortam.SqlAsync("ALTER TABLE content.privacy_notice_versions RENAME TO privacy_notice_versions_yok");
        HttpResponseMessage hatali;
        string govde;

        try
        {
            hatali = await AydinlatmaMetniOrtami.TalepAsync(client, "tr", "ac44-hata");
            govde = await hatali.Content.ReadAsStringAsync();
        }
        finally
        {
            await ortam.SqlAsync("ALTER TABLE content.privacy_notice_versions_yok RENAME TO privacy_notice_versions");
        }

        Assert.Equal(HttpStatusCode.InternalServerError, hatali.StatusCode);
        Assert.Equal("application/problem+json", hatali.Content.Headers.ContentType?.MediaType);
        foreach (string isaret in new[] { "Exception", "   at ", "Npgsql", "privacy_notice", "relation", "StackTrace", "SELECT" })
        {
            Assert.DoesNotContain(isaret, govde, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(once, await ortam.SayAsync("SELECT count(*) FROM leads.leads"));

        // Bütçe (spec'in 5'i) yenmemiş olmalı: beşi de kabul, altıncısı 429.
        for (int sira = 1; sira <= 5; sira++)
        {
            Assert.Equal(HttpStatusCode.Created, (await AydinlatmaMetniOrtami.TalepAsync(client, "tr", $"ac44-{sira}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await AydinlatmaMetniOrtami.TalepAsync(client, "tr", "ac44-6")).StatusCode);
        Assert.Equal(0, await ortam.SayAsync("SELECT count(*) FROM leads.leads WHERE idempotency_key = 'ac44-hata'"));
    }
}
