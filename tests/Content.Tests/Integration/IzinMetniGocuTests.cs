using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>
/// `0019` — kartın `content` migration'ı: başlangıç sürümü (AC-16, AC-17), migration sonrası
/// canlı metin değişmezi (AC-15) ve geri alınamazlık (AC-48). Her test boş bir veritabanını
/// ÖNCEKİ migration'a YUKARI çıkararak kurar (`Down`'a dayanmaz — D23), veriyi o şemada
/// yazar ve kartın migration'ını uygular.
/// </summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class IzinMetniGocuTests(ContentDatabase database)
{
    private const string OncekiMigration = "20261119054439_HomeTexts";

    private static readonly Guid Privacy = Icerik.Sistem(SystemPageKeys.Privacy);
    private static readonly Guid About = Icerik.Sistem(SystemPageKeys.About);

    [Fact(DisplayName = "0019/AC-16: Kartın migration'ı uygulandığında privacy tr ve en'de yayında, ar'da taslaksa tr ve en için o anki metinle birer başlangıç sürümü oluşur, ar için oluşmaz")]
    public async Task Dogar_BaslangicSurumu_YayindakiDillerde()
    {
        await using ContentDbContext context = await KarttanOnceKurAsync();

        await context.Database.MigrateAsync();

        List<PrivacyNoticeVersion> surumler = await context.PrivacyNoticeVersions.AsNoTracking().OrderBy(s => s.Locale).ToListAsync();
        Assert.Equal(
            [("en", "Privacy Notice", "Initial en body.", true), ("tr", "KVKK Metni", "Başlangıç tr gövdesi.", true)],
            surumler.Select(s => (s.Locale, s.Title, s.Body, s.IsInitial)));
        Assert.All(surumler, s => Assert.Equal(7, s.Id.Version));
    }

    [Fact(DisplayName = "0019/AC-15: privacy hangi dilde yayındaysa o dilin en son sürümünün başlığı ve gövdesi canlı metinle birebir aynıdır; bu, kartın migration'ından sonra ve AC-1…AC-14'ün her işlem dizisinin sonunda doğrulanır")]
    public async Task Esittir_EnSonSurumCanliMetne_MigrationdanSonra()
    {
        await using ContentDbContext context = await KarttanOnceKurAsync();

        await context.Database.MigrateAsync();

        await CanliMetin.DogrulaAsync(context);
    }

    [Fact(DisplayName = "0019/AC-17: Başlangıç sürümü okunduğunda başlangıç sürümü olduğu ayırt edilir; editörün yayınıyla doğan sürüm başlangıç sürümü olarak görünmez")]
    public async Task AyirtEdilir_BaslangicSurumu()
    {
        await using ContentDbContext context = await KarttanOnceKurAsync();
        await context.Database.MigrateAsync();
        PrivacyNoticeQueries sorgu = new(context, new ContentLocaleOptions("tr", ["tr", "ar", "en"]));

        PrivacyNoticeLookup tr = await sorgu.FindPublishedAsync("tr", CancellationToken.None);
        PrivacyNoticeVersionDto? baslangic = await sorgu.FindVersionAsync(tr.VersionId!.Value, CancellationToken.None);

        Assert.True(baslangic!.IsInitial);
        Assert.Equal(("tr", "KVKK Metni", "Başlangıç tr gövdesi."), (baslangic.Locale, baslangic.Title, baslangic.Body));
    }

    [Fact(DisplayName = "0019/AC-48: Bu kartın iki migration'ının Down'u çağrıldığında NotSupportedException atar; sürüm satırları ve taleplerin sürüm bağları yerinde kalır")]
    public async Task Atar_Down_SurumlerYerindeKalir()
    {
        await using ContentDbContext context = await KarttanOnceKurAsync();
        await context.Database.MigrateAsync();
        List<Guid> once = await context.PrivacyNoticeVersions.AsNoTracking().Select(s => s.Id).OrderBy(id => id).ToListAsync();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.GetService<IMigrator>().MigrateAsync(OncekiMigration));

        await using ContentDbContext sonra = Baglam(context.Database.GetConnectionString()!);
        Assert.Equal(once, await sonra.PrivacyNoticeVersions.AsNoTracking().Select(s => s.Id).OrderBy(id => id).ToListAsync());
        Assert.Equal(2, once.Count);
        Assert.Equal(
            2,
            await sonra.Pages.AsNoTracking().Where(p => p.Id == Privacy).SelectMany(p => p.Localizations).CountAsync(l => l.PublishedVersionId != null));
    }

    /// <summary>
    /// Yeni bir veritabanı, kartın migration'ının ÖNCESİNE kurulur: `privacy` tr ve en'de
    /// yayında, ar'da taslak; `about` tr'de yayında (yalnız `privacy` sürüm alır — R12).
    /// </summary>
    private async Task<ContentDbContext> KarttanOnceKurAsync()
    {
        string ad = $"goc_{Guid.NewGuid():N}";

        await using (NpgsqlConnection ana = new(database.ConnectionString))
        {
            await ana.OpenAsync();
            await using NpgsqlCommand olustur = new($"CREATE DATABASE {ad}", ana);
            await olustur.ExecuteNonQueryAsync();
        }

        string baglanti = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = ad }.ConnectionString;
        ContentDbContext context = Baglam(baglanti);
        await context.GetService<IMigrator>().MigrateAsync(OncekiMigration);

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO content.page_localizations (page_id, locale, title, body, slug, status) VALUES
                ({0}, 'tr', 'KVKK Metni', 'Başlangıç tr gövdesi.', 'kvkk', 'published'),
                ({0}, 'en', 'Privacy Notice', 'Initial en body.', 'privacy', 'published'),
                ({0}, 'ar', 'مسودة', 'نص مسودة.', 'khususiya', 'draft'),
                ({1}, 'tr', 'Hakkımızda', 'Biz kimiz.', 'hakkimizda', 'published');
            """,
            Privacy,
            About);

        return context;
    }

    private static ContentDbContext Baglam(string baglanti)
    {
        DbContextOptionsBuilder<ContentDbContext> builder = new();
        builder.UseNpgsql(
            baglanti,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentDbContext.Schema));

        return new ContentDbContext(builder.Options);
    }
}
