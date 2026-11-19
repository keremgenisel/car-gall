using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Tests.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CarGallery.Modules.Leads.Tests.Endpoints;

/// <summary>
/// Modülün endpoint'lerini Host projesine bağlanmadan ayağa kaldırır. Test projesi
/// yalnızca kendi modülüne referans verir (docs/testing.md § 1); Host'u içeri almak bu
/// sınırı sessizce delerdi.
///
/// `VehicleCatalog`, `VehicleReference` ve `Content` (`0019`) sınırın ötesindedir ve sahtelenir.
///
/// Hız sınırı bu boru hattında KASITLI OLARAK açılmaz: burada sınanan şey hata
/// sözleşmesi ve gövdelerdir. Sınırın kendisi Host'un boru hattında yaşıyor ve orada —
/// `Host.Tests` içinde — sınanıyor; buraya ikinci bir `UseRateLimiter()` koymak,
/// Host'unki eksikken bile yeşil kalan bir test üretirdi.
/// </summary>
internal sealed class ModuleApi(
    WebApplication app,
    FakeListingSnapshotSource ilanlar,
    FakeCanonicalModelNameSource modelAdlari,
    FakePrivacyNoticeSource aydinlatmaMetni) : IAsyncDisposable
{
    internal HttpClient Client { get; } = app.GetTestClient();

    internal FakeListingSnapshotSource Ilanlar { get; } = ilanlar;

    internal FakeCanonicalModelNameSource ModelAdlari { get; } = modelAdlari;

    internal FakePrivacyNoticeSource AydinlatmaMetni { get; } = aydinlatmaMetni;

    internal static async Task<ModuleApi> StartAsync(LeadsDatabase database)
    {
        FakeListingSnapshotSource ilanlar = new();
        FakeCanonicalModelNameSource modelAdlari = new();
        FakePrivacyNoticeSource aydinlatmaMetni = new();

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLeads(database.ConnectionString, ["tr", "ar", "en"]);
        builder.Services.Replace(ServiceDescriptor.Singleton<IListingSnapshotSource>(ilanlar));
        builder.Services.Replace(ServiceDescriptor.Singleton<ICanonicalModelNameSource>(modelAdlari));
        builder.Services.Replace(ServiceDescriptor.Singleton<IPrivacyNoticeSource>(aydinlatmaMetni));

        WebApplication app = builder.Build();
        app.MapLeads();
        await app.StartAsync();

        return new ModuleApi(app, ilanlar, modelAdlari, aydinlatmaMetni);
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
