using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Infrastructure;
using CarGallery.Modules.Content.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>
/// `0017` — gerçek PostgreSQL. Sistem sayfaları ve SSS sırası GLOBAL olduğu için her test
/// <see cref="ResetAsync"/> ile migration'ın kurduğu hâle döner: dört metinsiz sistem
/// sayfası, serbest sayfa ve SSS girdisi yok (`docs/testing.md` § 4).
/// </summary>
public sealed class ContentDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    /// <summary>Bağlantı dizesi.</summary>
    internal string ConnectionString => container.GetConnectionString();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using ContentDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    /// <summary>Yeni bir bağlam.</summary>
    internal ContentDbContext CreateContext(IInterceptor? interceptor = null)
    {
        DbContextOptionsBuilder<ContentDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentDbContext.Schema));

        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new ContentDbContext(builder.Options);
    }

    /// <summary>
    /// Migration'ın kurduğu hâle döner. `0019` — sürüm tablosu `TRUNCATE` ile boşalır:
    /// `DELETE` C5'in tetikleyicisine takılır (P5) — kuralın çalıştığının ilk işareti.
    /// </summary>
    internal async Task ResetAsync()
    {
        await using ContentDbContext context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM content.faq_entry_localizations;
            DELETE FROM content.faq_entries;
            DELETE FROM content.page_old_slugs;
            DELETE FROM content.page_localizations;
            DELETE FROM content.pages WHERE system_key IS NULL;
            UPDATE content.pages SET version = 1;
            DELETE FROM content.home_text_localizations;
            UPDATE content.home_texts SET version = 1;
            TRUNCATE content.privacy_notice_versions;
            """);
    }
}

/// <summary>Koleksiyon: testler aynı veritabanını SIRAYLA kullanır.</summary>
[CollectionDefinition(Name)]
public sealed class ContentDatabaseCollection : ICollectionFixture<ContentDatabase>
{
    /// <summary>Koleksiyon adı.</summary>
    public const string Name = "icerik-veritabani";
}

/// <summary>Testlerin ortak yardımcıları: her işlem kendi bağlamında, gerçek komut ve sorguyla.</summary>
internal sealed class Icerik(ContentDatabase database, params string[] locales)
{
    /// <summary>Etkin diller; verilmezse tr, ar, en.</summary>
    internal ContentLocaleOptions Locales { get; } = new("tr", locales.Length == 0 ? ["tr", "ar", "en"] : locales);

    /// <summary>
    /// Komutların sabit saati: komut boyunca değişmez, testin istediği anda ilerletilir
    /// (`0019` AC-1 — kayda geçen an bu değerdir). Yalnız veritabanına yazılan domain
    /// anları üretir; gerçek saatle karşılaştırılmaz (`docs/testing.md` § 4).
    /// </summary>
    internal DateTimeOffset Simdi { get; set; } = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Saati bir dakika ilerletir ve yeni değeri döner.</summary>
    internal DateTimeOffset Ilerlet() => Simdi = Simdi.AddMinutes(1);

    /// <summary>Sistem sayfasının sabit kimliği.</summary>
    internal static Guid Sistem(string key) => ContentPageConfiguration.SystemPageIds[key];

    /// <summary>Komutla bir işlem; verilen dinleyici bağlama takılır (ör. kayıt bariyeri).</summary>
    internal async Task<T> KomutAsync<T>(Func<ContentCommands, Task<T>> islem, IInterceptor? interceptor = null)
    {
        await using ContentDbContext context = database.CreateContext(interceptor);
        return await islem(new ContentCommands(context, Locales, new SabitSaat(Simdi)));
    }

    /// <summary>Panel sorgusuyla bir okuma.</summary>
    internal async Task<T> PanelAsync<T>(Func<ContentAdminQueries, Task<T>> okuma)
    {
        await using ContentDbContext context = database.CreateContext();
        return await okuma(new ContentAdminQueries(context, Locales));
    }

    /// <summary>Public sorguyla bir okuma.</summary>
    internal async Task<T> VitrinAsync<T>(Func<IContentPublicQueries, Task<T>> okuma)
    {
        await using ContentDbContext context = database.CreateContext();
        return await okuma(new ContentPublicQueries(context, Locales));
    }

    /// <summary>`0022` — ana sayfa metinleri komutuyla bir işlem.</summary>
    internal async Task<T> AnaSayfaKomutAsync<T>(Func<HomeTextCommands, Task<T>> islem, IInterceptor? interceptor = null)
    {
        await using ContentDbContext context = database.CreateContext(interceptor);
        return await islem(new HomeTextCommands(context, Locales));
    }

    /// <summary>`0022` — ana sayfa metinleri sorgusuyla bir okuma (panel ve vitrin aynı sınıf).</summary>
    internal async Task<T> AnaSayfaOkuAsync<T>(Func<HomeTextQueries, Task<T>> okuma)
    {
        await using ContentDbContext context = database.CreateContext();
        return await okuma(new HomeTextQueries(context, Locales));
    }

    /// <summary>Sayfanın güncel sürümü.</summary>
    internal async Task<int> SurumAsync(Guid id) =>
        (await PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Version;

    /// <summary>SSS girdisinin güncel sürümü.</summary>
    internal async Task<int> SssSurumAsync(Guid id) =>
        (await PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Single(item => item.Id == id).Version;

    /// <summary>Sayfaya metin yazar; başarılı olmalı.</summary>
    internal async Task YazAsync(Guid id, params PageTextInput[] texts)
    {
        int surum = await SurumAsync(id);
        Result<int> sonuc = await KomutAsync(k => k.SavePageAsync(id, surum, texts, CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? Kodlar(sonuc.Error) : null);
    }

    /// <summary>Dili yayına alır; başarılı olmalı.</summary>
    internal async Task YayinlaAsync(Guid id, string locale)
    {
        int surum = await SurumAsync(id);
        Result<int> sonuc = await KomutAsync(k => k.PublishPageAsync(id, surum, locale, CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? Kodlar(sonuc.Error) : null);
    }

    /// <summary>Metni yazıp yayına alır.</summary>
    internal async Task YazVeYayinlaAsync(Guid id, string locale, string title, string body, string slug)
    {
        await YazAsync(id, new PageTextInput(locale, title, body, slug));
        await YayinlaAsync(id, locale);
    }

    /// <summary>Serbest sayfa oluşturur.</summary>
    internal async Task<Guid> SerbestAsync(params PageTextInput[] texts)
    {
        Result<(Guid Id, int Version)> sonuc = await KomutAsync(k => k.CreatePageAsync(texts, CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? Kodlar(sonuc.Error) : null);
        return sonuc.Value.Id;
    }

    /// <summary>SSS girdisi oluşturur ve verilen dillerde yayına alır.</summary>
    internal async Task<Guid> SssAsync(string tr, string? ar = null, params string[] yayinda)
    {
        List<FaqTextInput> metin = [new("tr", tr, $"{tr} — cevap")];

        if (ar is not null)
        {
            metin.Add(new FaqTextInput("ar", ar, $"{ar} — جواب"));
        }

        Result<(Guid Id, int Version)> sonuc = await KomutAsync(k => k.CreateFaqEntryAsync(metin, CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? Kodlar(sonuc.Error) : null);

        foreach (string locale in yayinda)
        {
            int surum = await SssSurumAsync(sonuc.Value.Id);
            Result<int> yayin = await KomutAsync(k => k.PublishFaqEntryAsync(sonuc.Value.Id, surum, locale, CancellationToken.None));
            Assert.True(yayin.IsSuccess, yayin.IsFailure ? Kodlar(yayin.Error) : null);
        }

        return sonuc.Value.Id;
    }

    /// <summary>Doğrulama hatasının alan → kod eşlemesi.</summary>
    internal static Dictionary<string, string> Alanlar(Error error)
    {
        Assert.Equal("content.validation_failed", error.Code);
        string[] alanlar = error.Parameters!["fields"].Split(',');
        return alanlar.ToDictionary(alan => alan, alan => error.Parameters[alan], StringComparer.Ordinal);
    }

    private static string Kodlar(Error error) =>
        error.Code + " " + string.Join(";", (error.Parameters ?? new Dictionary<string, string>()).Select(pair => $"{pair.Key}={pair.Value}"));

    private sealed class SabitSaat(DateTimeOffset simdi) : IClock
    {
        public DateTimeOffset UtcNow => simdi;
    }
}
