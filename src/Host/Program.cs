using CarGallery.Host;
using CarGallery.Host.Localization;
using CarGallery.Host.Schema;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.Identity;
using CarGallery.Modules.Identity.Contracts.Setup;
using CarGallery.Modules.Leads;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;

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
builder.Services.AddVehicleCatalog(
    connectionString,
    localization.DefaultLocale,
    localization.EnabledLocales);

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

// 0005 — talep modülü. Hız sınırının eşiği yapılandırmadan gelir; VARSAYILAN spec'in
// kendi sayısıdır (C17: 10 dakikada 5). Yapılandırılabilir olmasının tek nedeni kanıt
// üretimidir: görsel kanıt üç locale'de başarılı gönderim ve hata senaryoları ister ve
// bunlar aynı IP'den kolayca eşiği aşar.
builder.Services.AddLeads(
    connectionString,
    localization.EnabledLocales,
    builder.Configuration.GetValue<int?>("Leads:RateLimit:PermitLimit") ?? 5,
    builder.Configuration.GetValue<int?>("Leads:RateLimit:WindowMinutes") ?? 10);

// İlk yazma uçları ilk hata sözleşmesini getirdi. `500` yanıtı ASLA iç detay taşımaz:
// stack trace, SQL, dosya yolu, tip adı yok — yalnız `traceId`
// (docs/conventions.md § 5, 0005/AC-39, 0006/AC-59). Bu olmadan geliştirme ortamında
// istisna metni ziyaretçiye giderdi.
builder.Services.AddProblemDetails();

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
app.MapVehicleReference();
app.MapIdentity();
app.MapLeads();

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
