using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CarGallery.Modules.Dealership.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Dealership;

/// <summary>
/// Modülün tek giriş noktası. `0003` D1 yazma ucunu `Identity`'ye kadar ertelemişti; `0020`
/// onu yalnız Yönetici politikasının arkasında açar. Diğer modüller veriyi yalnız
/// `Contracts` üzerinden okur (C1).
/// </summary>
public static class DealershipModule
{
    public static IServiceCollection AddDealership(
        this IServiceCollection services,
        string connectionString,
        string defaultLocale,
        IReadOnlyList<string> enabledLocales)
    {
        services.AddDbContext<DealershipDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DealershipDbContext.Schema)));

        Result<Locale> parsed = Locale.Create(defaultLocale);

        if (parsed.IsFailure)
        {
            throw new InvalidOperationException($"Varsayılan locale geçersiz: '{defaultLocale}'.");
        }

        // `0020` C4 — migration'lar ve ardından görünen adın varsayılan dildeki doldurması.
        services.AddScoped<IModuleSchema>(provider => new DealershipSchema(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value));

        // `0020` — görünen adın ve şube adının dil anahtarları etkin dillere karşı süzülür.
        List<Locale> enabled = [];

        foreach (string code in enabledLocales)
        {
            Result<Locale> locale = Locale.Create(code);

            if (locale.IsFailure)
            {
                throw new InvalidOperationException($"Etkin locale geçersiz: '{code}'.");
            }

            enabled.Add(locale.Value);
        }

        if (!enabled.Contains(parsed.Value))
        {
            enabled.Insert(0, parsed.Value);
        }

        services.AddSingleton(new DealershipLocales(parsed.Value, enabled));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<OutboxWriter<DealershipDbContext>>();
        services.AddScoped<DealershipUnitOfWork>();

        // 0007 — invariant koruması ortak desenden gelir; süre yapılandırmadan değil
        // varsayılandan, çünkü kilit süresi bir iş kuralı değil bir güvenlik marjıdır.
        services.AddSingleton(InvariantGuardOptions<DealershipDbContext>.Default);
        services.AddScoped<BranchCommands>();
        services.AddScoped<GalleryCommands>();
        services.AddScoped<DealershipAdminQueries>();
        services.AddScoped(provider => new OutboxDispatcher<DealershipDbContext>(
            provider.GetRequiredService<DealershipDbContext>(),
            provider,
            provider.GetRequiredService<IClock>(),
            new OutboxDispatchOptions(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxDispatcher<DealershipDbContext>>>()));

        // Arka plan dağıtıcısı modülün kendi kaydıdır: outbox tablosu bu modülün
        // şemasındadır ve onu okuyan da bu modülün DbContext'idir (D9).
        services.AddHostedService(provider => new OutboxDispatcherHostedService<DealershipDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxDispatcherHostedService<DealershipDbContext>>>()));

        services.AddScoped<IBranchQueries>(provider => new BranchQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value));

        // 0009 D2 — aynı sınıf, ayrı ve dar bir yüzey (D7).
        services.AddScoped<IBranchContactQueries>(provider => new BranchQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value));

        // 0014 D15 — ziyaretçiye dönük dar yüzey; panelin şube listesinden ayrı.
        // `0027` D6 — "şu an" saat soyutlamasından: modülün kaydettiği `IClock` (testte sabitlenebilir).
        services.AddScoped<IPublicGalleryQueries>(provider => new PublicGalleryQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value,
            provider.GetRequiredService<IClock>()));

        // `0020` D16 — Host'un `index.html`'e bastığı görünen ad; aynı sınıf, dar yüzey.
        services.AddScoped<IGalleryDisplayNameQueries>(provider => new PublicGalleryQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value,
            provider.GetRequiredService<IClock>()));

        // `0026` D17 — Host'un belgesi için ad + logo kimliği; aynı sınıf, ayrı dar yüzey.
        services.AddScoped<IGalleryBrandingQueries>(provider => new PublicGalleryQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value,
            provider.GetRequiredService<IClock>()));

        return services;
    }

    /// <summary>
    /// `0024` D9(c) · P9 — galeri profilini performans ölçümünün en kötü hâline getirir: bugün altı
    /// sosyal hesap, her biri sınırında. YALNIZ geliştirme ve performans ölçümü içindir
    /// (<c>Development:SeedGalleryProfile</c>); üretim yolunda çağrılmaz. Gerçek yazma yolundan
    /// (<see cref="GalleryCommands"/>) geçer: tohum, kuralları atlayan bir SQL değildir.
    /// </summary>
    public static async Task SeedGalleryProfileForDevelopmentAsync(
        this IServiceProvider services,
        Guid? logoMediaAssetId = null,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        AdminGalleryDto galeri = await scope.ServiceProvider.GetRequiredService<DealershipAdminQueries>().GetGalleryAsync(cancellationToken)
            ?? throw new InvalidOperationException("Galeri profili yok; kurulum adımı uygulanmamış.");
        DealershipLocales locales = scope.ServiceProvider.GetRequiredService<DealershipLocales>();

        Dictionary<string, string?> adlar = galeri.DisplayNames.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.Ordinal);
        adlar.TryAdd(locales.Default.Code, galeri.CommercialName);

        Result<int> sonuc = await scope.ServiceProvider.GetRequiredService<GalleryCommands>().UpdateProfileAsync(
            galeri.Version,
            new GalleryProfileInput(galeri.CommercialName, galeri.TaxNumber, galeri.TradeRegistryNumber, adlar, GalleryProfileDevelopmentSeed.SocialLinks()),
            cancellationToken);

        if (sonuc.IsFailure)
        {
            throw new InvalidOperationException($"Galeri profili tohumlanamadı: {sonuc.Error.Code}.");
        }

        // `0025` D10(c) · P9 · AC-62 — her AKTİF şubeye AC-60'ın programı; şubenin diğer alanları
        // okunduğu gibi geri yazılır. `0024`'ün hesapları yukarıda korunur (AC-71).
        DealershipDbContext context = scope.ServiceProvider.GetRequiredService<DealershipDbContext>();
        List<BranchId> aktifler = await context.Branches.AsNoTracking()
            .Where(branch => branch.Status == BranchStatus.Active)
            .Select(branch => branch.Id)
            .ToListAsync(cancellationToken);

        foreach (BranchId id in aktifler)
        {
            AdminBranchDto sube = (await scope.ServiceProvider.GetRequiredService<DealershipAdminQueries>().FindBranchAsync(id.Value, cancellationToken))!;
            Result<int> program = await scope.ServiceProvider.GetRequiredService<BranchCommands>().UpdateAsync(
                id.Value,
                sube.Version,
                new BranchFormInput(
                    sube.Names.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.Ordinal),
                    sube.Addresses.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.Ordinal),
                    sube.Phone,
                    sube.WhatsApp,
                    sube.Email,
                    sube.TimeZone,
                    sube.Slug,
                    sube.Latitude,
                    sube.Longitude,
                    GalleryProfileDevelopmentSeed.OpeningHours()),
                cancellationToken);

            if (program.IsFailure)
            {
                throw new InvalidOperationException($"Şube programı tohumlanamadı: {program.Error.Code}.");
            }
        }

        // `0026` P9 · AC-59 — logo, hesaplardan ve programlardan SONRA, gerçek atama yolundan.
        // Kimliği Host verir (tohum medyası kayıtlıyken); `Dealership` onu doğrulamaz (D2).
        if (logoMediaAssetId is { } logo)
        {
            AdminGalleryDto son = (await scope.ServiceProvider.GetRequiredService<DealershipAdminQueries>().GetGalleryAsync(cancellationToken))!;
            Result<int> atama = await scope.ServiceProvider.GetRequiredService<GalleryCommands>().AssignLogoAsync(son.Version, logo, cancellationToken);

            if (atama.IsFailure)
            {
                throw new InvalidOperationException($"Galeri logosu tohumlanamadı: {atama.Error.Code}.");
            }
        }
    }

    /// <summary>
    /// 0010a — modülün İLK uç kaydı. `0003` bilinçli olarak hiç uç açmamıştı; araç kaydı
    /// formu şube seçimi istediği için açıldı. `0014` ziyaretçinin galeri okumasını ekledi.
    /// `0020` galeri profilinin ve şubelerin yazma uçlarını yalnız Yönetici için açtı (D2).
    /// </summary>
    public static void MapDealership(this IEndpointRouteBuilder builder)
    {
        builder.MapBranchEndpoints();
        builder.MapPublicGalleryEndpoints();
        builder.MapDealershipAdminEndpoints();
    }
}
