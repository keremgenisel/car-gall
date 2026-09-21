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
app.UseMiddleware<LocalePrefixMiddleware>();

app.MapVehicleCatalog();
app.MapVehicleReference();

app.Run();

/// <summary>Entegrasyon testlerinin giriş noktası olarak görebilmesi için.</summary>
public partial class Program;
