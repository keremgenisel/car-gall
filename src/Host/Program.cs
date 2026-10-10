using CarGallery.BuildingBlocks;
using CarGallery.Host;
using CarGallery.Host.Localization;
using CarGallery.Host.Schema;
using CarGallery.Host.Theme;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.Identity;
using CarGallery.Modules.Identity.Contracts.Setup;
using CarGallery.Modules.Leads;
using CarGallery.Modules.Media;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Notifications;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Statik varlıklar sıkıştırılmadan gidiyordu: 261 KB'lık JS paketi ham hâliyle transfer
// ediliyor ve yavaş bağlantıda ilk boyamayı geciktiriyordu (AC-35). Kestrel varsayılan
// olarak sıkıştırmaz; bu bir yapılandırma eksiğidir, uç kodunun sorunu değil.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
    [
        "application/javascript",
        "text/javascript",
        "image/svg+xml",
        "application/json",
    ]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal);

builder.Services
    .AddOptions<LocalizationOptions>()
    .Bind(builder.Configuration.GetSection(LocalizationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

string connectionString = builder.Configuration.GetConnectionString("VehicleCatalog")
    ?? throw new InvalidOperationException("ConnectionStrings:VehicleCatalog yapılandırılmamış.");

LocalizationOptions localization = builder.Configuration
    .GetSection(LocalizationOptions.SectionName)
    .Get<LocalizationOptions>()
    ?? throw new InvalidOperationException($"{LocalizationOptions.SectionName} bölümü yapılandırılmamış.");

// Host yalnızca modülü kaydeder ve endpoint'lerini toplar; iş mantığı burada yaşamaz
// (docs/architecture.md § 2.F, Yasak Listesi 20).
// 0010a — para birimi KURULUM BAŞINA SABİTTİR ve panelden değiştirilemez (C3, P3).
// Üs ayrı veriliyor: KWD/BHD/JOD/OMR üç ondalıklıdır ve üs kaybolursa `1250,500`
// sessizce `1250,50` gibi okunur.
builder.Services.AddVehicleCatalog(
    connectionString,
    localization.DefaultLocale,
    localization.EnabledLocales,
    builder.Configuration["Currency:Code"] ?? "TRY",
    builder.Configuration.GetValue<int?>("Currency:MinorUnitExponent") ?? 2,
    // 0010b/C26 — panel sayfa boyutu: varsayılan 25, üst sınır 100.
    builder.Configuration.GetValue<int?>("AdminPaging:PageSize") ?? 25,
    builder.Configuration.GetValue<int?>("AdminPaging:MaxPageSize") ?? 100);

// Model adı ve kasa tipi etiketleri referans veridir ve VehicleReference'a aittir
// (docs/architecture.md § 3). Catalog onlara yalnız Contracts üzerinden ulaşır.
builder.Services.AddVehicleReference(connectionString);
// Modül kendi arka plan dağıtıcısını da kaydeder: DealershipDbContext internal'dır ve
// host onu göremez — sınırın doğru yerde durduğunun işareti.
builder.Services.AddDealership(connectionString, localization.DefaultLocale);

// 0006 — personel kimliği. Süreler ve eşikler DAĞITIM YAPILANDIRMASINDAN gelir (C20);
// kabul kriterleri yapılandırılan değeri doğrular, koda gömülü bir sabiti değil.
//
// Başlangıç Yönetici parolası appsettings'te DEĞİL, ortam değişkenindedir: sır repoda
// durmaz (docs/conventions.md § 11). Yoksa uygulama başlamaz (D2, AC-53) — üretilmiş bir
// varsayılan, bilinen parolalı bir Yönetici hesabı demektir.
builder.Services.AddIdentity(new IdentitySetup(
    connectionString,
    localization.DefaultLocale,
    localization.EnabledLocales,
    builder.Configuration["Identity:SeedAdmin:LoginId"] ?? "yonetici@cargallery.local",
    builder.Configuration["Identity:SeedAdmin:Password"] ?? string.Empty,
    builder.Configuration["Identity:SeedAdmin:DisplayName"] ?? "Kurulum Yöneticisi",
    builder.Configuration.GetValue<int?>("Identity:AccessLifetimeMinutes") ?? 30,
    builder.Configuration.GetValue<int?>("Identity:RefreshLifetimeDays") ?? 14,
    builder.Configuration.GetValue<int?>("Identity:FailedAttemptThreshold") ?? 5,
    builder.Configuration.GetValue<int?>("Identity:LockoutMinutes") ?? 15,
    builder.Configuration.GetValue<int?>("Identity:MinimumPasswordLength") ?? 12,
    builder.Configuration.GetValue<int?>("Identity:PasswordIterations") ?? 210_000,
    builder.Configuration.GetValue<int?>("Identity:LoginRequestLimit") ?? 20,
    builder.Configuration.GetValue<int?>("Identity:LoginRequestWindowMinutes") ?? 10));

// 0005 — talep modülü. İKİ eşik var: `CapturedLeadLimit` C17'nin sayısıdır (10 dakikada
// 5) ve yalnız KAYIT OLUŞAN isteği sayar; `RequestLimit` kaba tekrarı kesen daha gevşek
// istek sınırıdır. İkisi ayrı, çünkü C17'yi boru hattına koymak formu birkaç kez yanlış
// dolduran dürüst ziyaretçiyi kilitlerdi. Yapılandırılabilir olmalarının tek nedeni
// kanıt üretimidir.
builder.Services.AddLeads(
    connectionString,
    localization.EnabledLocales,
    builder.Configuration.GetValue<int?>("Leads:RateLimit:CapturedLeadLimit") ?? 5,
    builder.Configuration.GetValue<int?>("Leads:RateLimit:RequestLimit") ?? 30,
    builder.Configuration.GetValue<int?>("Leads:RateLimit:WindowMinutes") ?? 10);

// 0009 — talep bildirimi. Sistemden dışarı posta gönderen TEK modül (C1, Yasak Listesi 17).
//
// Hiçbir alan için `??` ile üretilmiş bir varsayılan YOK (AC-26, AC-27, AC-28). Eksik
// yapılandırma burada patlar ve uygulama hiç ayağa kalkmaz; bu bilinçli: üretilmiş bir
// bildirim adresi, her talebin sessizce kimseye gitmemesi demektir ve o sessizlik tam
// olarak bu spec'in varlık nedeni. Kurulum parolasında (0006 D2) verilen karar aynısıdır.
//
// SMTP kullanıcı adı ve parolası appsettings'te DEĞİL, ortam değişkenindedir — sır repoda
// durmaz (docs/conventions.md § 11). Geliştirmede user-secrets.
builder.Services.AddNotifications(new NotificationsSetup(
    connectionString,
    localization.DefaultLocale,
    builder.Configuration["Notifications:SetupNotificationAddress"] ?? string.Empty,
    builder.Configuration["Notifications:PublicBaseUrl"] ?? string.Empty,
    builder.Configuration["Notifications:SenderAddress"] ?? string.Empty,
    builder.Configuration["Notifications:Smtp:Host"] ?? string.Empty,
    builder.Configuration.GetValue<int?>("Notifications:Smtp:Port") ?? 0,
    builder.Configuration["Notifications:Smtp:User"] ?? string.Empty,
    builder.Configuration["Notifications:Smtp:Password"] ?? string.Empty,
    builder.Configuration.GetValue<bool?>("Notifications:Smtp:UseStartTls") ?? true,
    builder.Configuration.GetValue<int?>("Notifications:MaximumAttempts") ?? 0,
    builder.Configuration.GetValue<int?>("Notifications:RetryBaseSeconds") ?? 0,
    builder.Configuration.GetValue<int?>("Notifications:SendTimeoutSeconds") ?? 0));

// 0011 — medya hattı. R2 kovası ve CDN taban adresi DAĞITIM YAPILANDIRMASINDAN gelir
// (C6) ve hiçbirinin `??` ile üretilmiş varsayılanı YOK: filigran kaynağı
// yapılandırılmamışsa her fotoğraf filigransız çıkar ve galeri sahibinin satın aldığı tek
// garanti sessizce kaybolur. Sessizliği fotoğraflar rakip sitede göründüğünde fark edilir
// ve o noktada bütün türevlerin yeniden üretilmesi gerekir.
//
// Depolama anahtarı ve parolası appsettings'te DEĞİL, ortam değişkenindedir — sır repoda
// durmaz (docs/conventions.md § 11). Geliştirmede user-secrets.
//
// Sınırlar (bayt, piksel, deneme) appsettings'te durabilir: sır değil, operasyon ayarı.
builder.Services.AddMedia(new MediaSetup(
    connectionString,
    builder.Configuration["Media:Storage:ServiceUrl"] ?? string.Empty,
    builder.Configuration["Media:Storage:Bucket"] ?? string.Empty,
    builder.Configuration["Media:Storage:AccessKey"] ?? string.Empty,
    builder.Configuration["Media:Storage:SecretKey"] ?? string.Empty,
    builder.Configuration["Media:Storage:Region"] ?? string.Empty,
    builder.Configuration.GetValue<bool?>("Media:Storage:ForcePathStyle") ?? true,
    builder.Configuration["Media:PublicBaseUrl"] ?? string.Empty,
    builder.Configuration["Media:WatermarkObjectKey"] ?? string.Empty,
    builder.Configuration.GetValue<long?>("Media:MaximumByteSize") ?? 0,
    builder.Configuration.GetValue<long?>("Media:MaximumPixelCount") ?? 0,
    builder.Configuration.GetValue<int?>("Media:MaximumAttempts") ?? 0,
    builder.Configuration.GetValue<int?>("Media:RetryBaseSeconds") ?? 0));

// İlk yazma uçları ilk hata sözleşmesini getirdi. `500` yanıtı ASLA iç detay taşımaz:
// stack trace, SQL, dosya yolu, tip adı yok — yalnız `traceId`
// (docs/conventions.md § 5, 0005/AC-39, 0006/AC-59). Bu olmadan geliştirme ortamında
// istisna metni ziyaretçiye giderdi.
builder.Services.AddProblemDetails();

// frontend-002 — kuruluma özel tema. Dört durum AYRIDIR (C5) ve yalnız birincisi sessiz:
// tema hiç verilmezse varsayılan palet çalışır; kısmi tema, tanınmayan anahtar, biçime
// uymayan değer ve kontrastı yetersiz tema uygulamayı HİÇ ayağa kaldırmaz.
//
// Erişilebilirlik tabanının bir yapılandırma hatasıyla sessizce kırılması, kırıldığının
// fark edilmemesi demektir — ve renk hatası gözle yıllarca fark edilmez (D3). Açılan ama
// okunamayan bir vitrin, açılmayan bir vitrinden kötüdür.
Result<ThemeOptions> theme = ThemeOptions.Create(builder.Configuration);

if (theme.IsFailure)
{
    throw new InvalidOperationException(ThemeErrorText.Describe(theme.Error));
}

builder.Services.AddSingleton(theme.Value);

builder.Services.AddSingleton(new LocalePrefixResolver(localization.DefaultLocale, localization.EnabledLocales));
builder.Services.AddSingleton<IndexHtmlRenderer>();
builder.Services.AddSingleton<AdminShellRenderer>();

WebApplication app = builder.Build();

app.UseExceptionHandler();

// KURULUM ADIMI. Migration'ı çalışan uygulama değil, bu tek seferlik koşu uygular
// (docs/decisions/0002). Süreç uygular ve biter; sunucu hiç açılmaz.
if (args.Contains(ModuleSchemaOperations.MigrateArgument, StringComparer.Ordinal))
{
    return await app.Services.ApplyModuleSchemasAsync(
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CarGallery.Schema"));
}

// Çalışan uygulama migration UYGULAMAZ, yalnız uygulanmış mı diye bakar. Eksikse
// açıkça reddeder: sessizce devam etmek hatayı ilk ziyaretçiye ertelerdi ve o hata
// "tablo yok" diye okunurdu, "kurulum adımı atlandı" diye değil.
await app.Services.EnsureModuleSchemasAreCurrentAsync();

// Ters proxy arkasında ziyaretçinin gerçek adresi — 0005'in hız sınırı doğru bölünsün
// diye. Proxy AÇIKÇA listelenmemişse ara katman hiç eklenmez ve X-Forwarded-For yok
// sayılır: güvenilmeyen başlığa güvenmek, sınırı istemcinin kapatabilmesi demektir.
ForwardedHeadersOptions? forwardedHeaders = ForwardedHeadersSetup.Build(builder.Configuration);

if (forwardedHeaders is not null)
{
    app.UseForwardedHeaders(forwardedHeaders);
}

// Sıkıştırma statik dosya servisinden ÖNCE gelmeli, yoksa dosyalar ham geçer.
app.UseResponseCompression();
app.UseStaticFiles();

// Yönlendirme, locale katmanından ÖNCE çalışır ki eşleşen endpoint'ler bilinsin.
app.UseRouting();

// Hız sınırı BORU HATTINDA yaşar; modül yalnız politikayı tanımlar. Bu satır silinirse
// 0005/C17'nin sınırı sessizce kaybolur — Host.Tests bunu gerçek boru hattında sınıyor.
app.UseRateLimiter();

// Kimlik doğrulama da BORU HATTINDA yaşar; modül yalnız şemayı ve politikaları tanımlar.
// Bu üç satır silinirse yönetim yüzeyinin koruması SESSİZCE kaybolur — `Host.Tests`
// bunu gerçek boru hattında sınıyor.
app.UseAuthentication();
app.UseAuthorization();
app.UseIdentity();


app.UseMiddleware<LocalePrefixMiddleware>();

app.MapVehicleCatalog();
app.MapDealership();
app.MapVehicleReference();
app.MapIdentity();
app.MapLeads();
app.MapMedia();

// Geliştirme ve görsel kanıt üretimi yolu. Bayrak kapalıyken hiçbir şey kaydedilmez
// ve bu uç noktalar hiç tanımlanmaz; üretimde erişilebilir bir yüzeyi yoktur.
if (builder.Configuration.GetValue<bool>("Development:SeedShowcase"))
{
    Guid seedModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    // Rota parametresi SVG gövdesine giriyor; serbest metin kabul etmek aynı origin'de
    // yansıtılmış XSS demektir (image/svg+xml içinde script çalışır). Seed yalnızca
    // cover-0..5 üretiyor, bu yüzden parametre int kısıtına bağlandı ve aralık dışı
    // değer 404 döner — kaçışa güvenmek yerine yüzey kapatıldı.
    app.MapGet("/seed-media/cover-{index:int}.svg", (int index) =>
        index is < 0 or > 5
            ? Results.NotFound()
            : Results.Content(
                $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 360" width="480" height="360">
                  <rect width="480" height="360" fill="#3b4a63"/>
                  <path d="M70 230 L110 160 H370 L410 230 V270 H70 Z" fill="#8fa3c4"/>
                  <circle cx="140" cy="270" r="26" fill="#1f2a3d"/>
                  <circle cx="340" cy="270" r="26" fill="#1f2a3d"/>
                  <rect x="130" y="170" width="100" height="50" fill="#cfd9ea"/>
                  <rect x="250" y="170" width="100" height="50" fill="#cfd9ea"/>
                  <text x="240" y="330" text-anchor="middle" font-family="sans-serif" font-size="22" fill="#cfd9ea">cover-{index}</text>
                </svg>
                """,
                "image/svg+xml"));

    // Demo VERİSİ; şema değil. Şemayı kurulum adımı uygular ve yukarıdaki doğrulama
    // buraya gelinmeden önce eksikse zaten reddetmiştir.
    await app.Services.SeedVehicleReferenceForDevelopmentAsync(seedModelId);
    int seedCount = builder.Configuration.GetValue<int?>("Development:SeedListingCount") ?? 200;
    await app.Services.SeedVehicleCatalogForDevelopmentAsync(seedCount, seedModelId);
}

// Kurulum Yönetici hesabı olmadan hiç kimse giriş yapamaz ve sistem kalıcı olarak
// kapalı kalır (0006/D2, Tuzak 1). İdempotenttir: hesap varsa dokunulmaz.
//
// Bu bir DDL DEĞİL, tek satırlık DML'dir; ADR 0002'nin "çalışan uygulama migration
// uygulamaz" kuralını delmez. `identity` şemasının kendisi artık diğer üç modül gibi
// IModuleSchema üzerinden kurulum adımında uygulanıyor ve yukarıda doğrulanıyor.
await app.Services.EnsureSeedAdministratorAsync();

await app.RunAsync();

return 0;

/// <summary>Entegrasyon testlerinin giriş noktası olarak görebilmesi için.</summary>
public partial class Program;
