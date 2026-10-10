using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

/// <summary>
/// Modülün endpoint'lerini Host projesine bağlanmadan ayağa kaldırır. Test projesi
/// yalnızca kendi modülüne referans verir (docs/testing.md § 1); Host'u içeri almak
/// bu sınırı sessizce delerdi.
/// </summary>
internal sealed class ModuleApi : IAsyncDisposable
{
    private readonly WebApplication app;

    private ModuleApi(WebApplication app) => this.app = app;

    internal HttpClient Client { get; private set; } = null!;

    internal static async Task<ModuleApi> StartAsync(ShowcaseDatabase database)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddVehicleCatalog(
database.ConnectionString, "tr", ["tr", "ar", "en"],
                "TRY",
                2,
                adminPageSize: 25,
                adminMaxPageSize: 100);

        // Model adı VehicleReference'a aittir; bu testlerde o sınırın ötesi sahtelenir.
        builder.Services.Replace(ServiceDescriptor.Singleton<IVehicleModelNameResolver>(new FakeModelNameResolver()));
        builder.Services.Replace(ServiceDescriptor.Singleton<IBodyTypeCatalog>(new FakeBodyTypeCatalog()));
        builder.Services.Replace(ServiceDescriptor.Singleton<IReferenceLabelResolver>(new FakeReferenceLabelResolver()));

        WebApplication app = builder.Build();
        app.MapVehicleCatalog();
        await app.StartAsync();

        return new ModuleApi(app) { Client = app.GetTestClient() };
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
