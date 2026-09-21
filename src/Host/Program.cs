using CarGallery.Host;
using CarGallery.Host.Localization;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton(new LocalePrefixResolver(localization.DefaultLocale, localization.EnabledLocales));
builder.Services.AddSingleton<IndexHtmlRenderer>();

WebApplication app = builder.Build();

app.UseStaticFiles();

// Yönlendirme, locale katmanından ÖNCE çalışır ki eşleşen endpoint'ler bilinsin.
app.UseRouting();
app.UseMiddleware<LocalePrefixMiddleware>();

app.MapVehicleCatalog();
app.MapVehicleReference();

// Geliştirme ve görsel kanıt üretimi yolu. Bayrak kapalıyken hiçbir şey kaydedilmez
// ve bu uç noktalar hiç tanımlanmaz; üretimde erişilebilir bir yüzeyi yoktur.
if (builder.Configuration.GetValue<bool>("Development:SeedShowcase"))
{
    Guid seedModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    app.MapGet("/seed-media/{name}", (string name) => Results.Content(
        $"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 360" width="480" height="360">
          <rect width="480" height="360" fill="#3b4a63"/>
          <path d="M70 230 L110 160 H370 L410 230 V270 H70 Z" fill="#8fa3c4"/>
          <circle cx="140" cy="270" r="26" fill="#1f2a3d"/>
          <circle cx="340" cy="270" r="26" fill="#1f2a3d"/>
          <rect x="130" y="170" width="100" height="50" fill="#cfd9ea"/>
          <rect x="250" y="170" width="100" height="50" fill="#cfd9ea"/>
          <text x="240" y="330" text-anchor="middle" font-family="sans-serif" font-size="22" fill="#cfd9ea">{name}</text>
        </svg>
        """,
        "image/svg+xml"));

    await app.Services.SeedVehicleReferenceForDevelopmentAsync(seedModelId);
    int seedCount = builder.Configuration.GetValue<int?>("Development:SeedListingCount") ?? 200;
    await app.Services.SeedVehicleCatalogForDevelopmentAsync(seedCount, seedModelId);
}

app.Run();

/// <summary>Entegrasyon testlerinin giriş noktası olarak görebilmesi için.</summary>
public partial class Program;
