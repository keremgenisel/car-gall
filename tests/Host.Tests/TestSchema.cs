using CarGallery.BuildingBlocks.Schema;
using CarGallery.Host.Schema;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// Testlerin kurulum adımı. Uygulama artık migration UYGULAMAZ, yalnız doğrular
/// (docs/decisions/0002): şema hazır değilken Host ayağa kalkmaz ve test de
/// kalkamaz.
///
/// Bu, testlerin şemayı eskiden "yan etkiyle" aldığını görünür kılıyordu: geliştirme
/// seed'i hem veri yazıyor hem migration uyguluyordu ve hiçbir test kurulum adımının
/// varlığına bağımlı değildi — üretimde o adım hiç yoktu ve kimse fark etmemişti.
///
/// Modüller burada ELLE sayılıyor çünkü test kendi kabını kuruyor; uygulamanın
/// kendisinde böyle bir liste yok, şemalar DI'dan gelir.
/// </summary>
internal static class TestSchema
{
    internal static async Task ApplyAsync(string connectionString)
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddVehicleCatalog(connectionString, "tr", ["tr", "ar", "en"]);
        services.AddVehicleReference(connectionString);
        services.AddDealership(connectionString, "tr");

        await using ServiceProvider provider = services.BuildServiceProvider();

        await provider.ApplyModuleSchemasAsync(NullLogger.Instance);
    }
}
