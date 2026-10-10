using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Şemanın GERÇEKTEN ne olduğunu ölçer. `0010a` ve `0010b`'de aynı sınıftan iki kusur
/// yalnız gerçek PostgreSQL'de göründü (jsonb varsayılanı, `numeric` ölçeği).
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class SemaTests(MedyaOrtami ortam)
{
    /// <summary>
    /// Eşzamanlılık belirteci PostgreSQL'in SİSTEM kolonu `xmin`'e eşlenir; migration
    /// aynı adla bir KULLANICI kolonu yaratmamalı.
    ///
    /// <para>Yaratsaydı sistem kolonunu gölgeler, belirteç hiç değişmez ve AC-12'nin
    /// koruması sessizce kaybolurdu — test yeşil kalırken.</para>
    /// </summary>
    [Fact]
    public async Task Yaratmaz_Migration_KullaniciKolonuOlarak_Xmin()
    {
        await using MediaDbContext context = ortam.CreateContext();

        List<string> kolonlar = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'media' AND table_name = 'assets'
                ORDER BY ordinal_position
                """)
            .ToListAsync();

        Assert.NotEmpty(kolonlar);
        Assert.DoesNotContain("xmin", kolonlar);

        // Beklenen kolonların durduğunu da ölçüyoruz: boş bir tabloda üstteki iddia da
        // yeşil kalırdı.
        Assert.Contains("status", kolonlar);
        Assert.Contains("original_object_key", kolonlar);
        Assert.Contains("failure_reason", kolonlar);

        // AC-15 — geri çekilme aralığı kaldırıldığı için `next_attempt_at` de yok:
        // otomatik tekrar olmadan "sırası gelmiş" diye ayrı bir zaman alanı gerekmiyor.
        Assert.DoesNotContain("next_attempt_at", kolonlar);
    }

    /// <summary>
    /// Belirteç GERÇEKTEN değişiyor: bir güncellemeden sonra `xmin` farklı olmalı.
    /// Değişmezse eşzamanlılık kontrolü hiçbir zaman çakışma görmez.
    /// </summary>
    [Fact]
    public async Task Degisir_Belirtec_GuncellemeSonrasi()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(Fixtures.GorselFixture.FiligranPng());

        Guid id = await YuklemeYardimcisi.YukleAsync(ortam);

        uint onceki = await BelirtecAsync(id);

        await using (MediaDbContext yaz = ortam.CreateContext())
        {
            await yaz.Database.ExecuteSqlRawAsync(
                "UPDATE media.assets SET attempt_count = attempt_count + 1 WHERE id = {0}", id);
        }

        Assert.NotEqual(onceki, await BelirtecAsync(id));
    }

    private async Task<uint> BelirtecAsync(Guid id)
    {
        await using MediaDbContext context = ortam.CreateContext();

        List<uint> degerler = await context.Database
            .SqlQueryRaw<uint>(
                """SELECT xmin::text::bigint AS "Value" FROM media.assets WHERE id = {0}""", id)
            .ToListAsync();

        return degerler.Single();
    }
}
