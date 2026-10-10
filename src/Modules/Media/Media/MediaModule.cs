using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Media.Endpoints;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Media;

/// <summary>Modülün tek giriş noktası (`docs/conventions.md` § 1).</summary>
public static class MediaModule
{
    /// <summary>
    /// Multipart çerçevelemesi için ayrılan pay: sınır çizgisi, bölüm başlıkları ve alan
    /// adı. Dosyanın kendi boyutunu `UploadMediaAssetCommand` denetliyor; boru hattının
    /// sınırı yalnızca kaba bir arka duvar.
    ///
    /// <para><c>FormOptions.ValueLengthLimit</c> AÇIKÇA dokunulmuyor. Önceki hâli
    /// <c>int.MaxValue</c> yazıyordu ve bu, yüklenen dosyaya hiçbir şey kazandırmadan
    /// host GENELİNDEKİ dosya-olmayan form değeri sınırını (varsayılan 4 MB) kaldırıyordu
    /// — yüklenen dosyayı <c>MultipartBodyLengthLimit</c> yönetiyor. Bugün başka form ucu
    /// yok, ama bir sonraki form ucu o gevşemeyi miras alacaktı.</para>
    /// </summary>
    private const long MultipartOverheadAllowance = 8 * 1024;

    public static IServiceCollection AddMedia(this IServiceCollection services, MediaSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        services.AddDbContext<MediaDbContext>(options =>
            options.UseNpgsql(
                setup.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    MediaDbContext.Schema)));

        // ADR 0002 — migration'ı çalışan uygulama değil kurulum adımı uygular ve modüller
        // Host'ta elle sayılmaz. Bu kayıt olmadan `media` şeması hiç oluşmaz ve eksiklik
        // yalnız ilk yüklemede görünürdü.
        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<MediaDbContext>(
            provider.GetRequiredService<MediaDbContext>(),
            "Media"));

        services.AddSingleton<IClock, SystemClock>();

        // C6/AC-31…33 — yapılandırma KURULUŞTA doğrulanır. Eksikse burada patlar ve
        // uygulama hiç ayağa kalkmaz; çalışma anında patlamaz. Filigran kaynağı
        // yapılandırılmamışsa her fotoğraf filigransız çıkar ve galeri sahibinin satın
        // aldığı tek garanti SESSİZCE kaybolur.
        Result<MediaOptions> options = MediaOptions.Create(setup);

        if (options.IsFailure)
        {
            throw new InvalidOperationException(
                $"Media yapılandırması eksik veya geçersiz: {options.Error.Code}");
        }

        services.AddSingleton(options.Value);

        // D10(b) — boyut sınırı BORU HATTINDA uygulanır, uç gövdeyi tamponlamadan önce.
        // Uçta kontrol etmek, sınırı aşan gövdenin zaten belleğe alınmış olması demekti.
        //
        // **Sınır biraz YUKARIDA ve bu bir hatanın bedeliydi.** İki sınır birebir aynı
        // eşik olduğunda komutun `media.too_large` dalına HTTP yoluyla hiç ulaşılamıyordu:
        // multipart gövdesi dosyanın kendisinden büyüktür (sınır çizgisi, başlıklar, alan
        // adı) ve boru hattı her zaman komuttan ÖNCE patlıyordu. Ölçüldü: sınır 39.344
        // iken 39.344 baytlık gövde `202`, sınır 39.343 iken aynı gövde `500` —
        // `MediaErrors.TooLarge` ölü koddu.
        //
        // Pay, multipart çerçevelemesini karşılıyor: dosyanın KENDİ boyutu artık komutun
        // kararı, boru hattı ise yalnız kaba bir arka duvar.
        services.Configure<FormOptions>(form =>
        {
            form.MultipartBodyLengthLimit = options.Value.MaximumByteSize + MultipartOverheadAllowance;
        });

        services.AddSingleton(provider => new MediaUrlResolver(
            provider.GetRequiredService<MediaOptions>()));

        services.AddSingleton<IImageProcessor>(_ => new SkiaImageProcessor());

        // Depolama istemcisi SINGLETON: `AmazonS3Client` iş parçacığı güvenlidir ve HTTP
        // bağlantı havuzunu kendi tutar. İstek başına yeni istemci, her yüklemede yeni
        // bağlantı havuzu demekti.
        //
        // Kayıtlar FABRİKA lambda'sı ile ve bu bir hatanın bedeliydi: `AddSingleton<IObjectStore,
        // S3ObjectStore>()` yazdığımda DI "uygun kurucu bulunamadı" dedi, çünkü modülün
        // içindeki her şey `internal` ve kurucu da `internal`. Hata yalnız
        // `WebApplicationFactory`'nin `ValidateOnBuild` doğrulamasında göründü — üretimde
        // uygulama ayağa kalkıp İLK YÜKLEMEDE patlardı. Modül dosyalarının geri kalanı
        // zaten bu deseni kullanıyor (`DealershipModule`).
        services.AddSingleton<IObjectStore>(provider => new S3ObjectStore(
            provider.GetRequiredService<MediaOptions>()));

        // KONKRET tip olarak kaydediliyor, `IOutboxWriter` arayüzü olarak DEĞİL — ve bu
        // ölçülmüş bir karar.
        //
        // `IOutboxWriter` non-generic bir arayüz ve dört modül onu kendi `DbContext`'iyle
        // kaydediyor; DI'da **son kayıt kazanıyor**. `AddMedia` Host'ta `AddLeads`'ten
        // SONRA çağrıldığı için `Leads`'in `IOutboxWriter`'ı bir anda
        // `OutboxWriter<MediaDbContext>` oldu: `LeadCaptured` olayı `media` şemasının
        // outbox'ına yazıldı, `Leads`'in dağıtıcısı kendi tablosunda hiçbir şey bulamadı
        // ve bildirim zinciri SESSİZCE durdu. `TalepBildirimiZinciriTests` 30 saniye
        // bekleyip 0 kayıt gördü.
        //
        // Konkret tip kullanmak `Media`'yı sıraya bağımlı olmaktan çıkarıyor ve başka
        // modülün outbox'ına yazma yolunu kapatıyor (Yasak Listesi 16). Arayüzün kendisi
        // hâlâ bu tuzağı taşıyor — PR'da açık konu olarak raporlandı.
        services.AddScoped<OutboxWriter<MediaDbContext>>();
        services.AddScoped<UploadMediaAssetCommand>();
        services.AddScoped<RetryMediaAssetCommand>();
        services.AddScoped<MediaProcessingService>();
        services.AddScoped<IMediaAssetQueries, MediaAssetQueries>();

        services.AddScoped(provider => new OutboxDispatcher<MediaDbContext>(
            provider.GetRequiredService<MediaDbContext>(),
            provider,
            provider.GetRequiredService<IClock>(),
            new OutboxDispatchOptions(),
            provider.GetRequiredService<ILogger<OutboxDispatcher<MediaDbContext>>>()));

        // Arka plan dağıtıcısı modülün kendi kaydıdır: outbox tablosu bu modülün
        // şemasındadır ve onu okuyan da bu modülün `DbContext`'idir (Yasak Listesi 16).
        services.AddHostedService(provider => new OutboxDispatcherHostedService<MediaDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxDispatcherHostedService<MediaDbContext>>>()));

        // D1/D14 — işleme hattı. Kayıt veritabanında durduğu için yeniden başlatma
        // bekleyen işi kaybetmez.
        services.AddHostedService(provider => new MediaProcessingHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<MediaProcessingHostedService>>()));

        return services;
    }

    public static void MapMedia(this IEndpointRouteBuilder builder) => builder.MapMediaAdminEndpoints();
}
