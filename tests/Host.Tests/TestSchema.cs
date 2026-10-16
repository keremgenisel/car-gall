using CarGallery.BuildingBlocks.Schema;
using CarGallery.Host.Schema;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.Identity;
using CarGallery.Modules.Leads;
using CarGallery.Modules.Media;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Notifications;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Identity.Contracts.Setup;
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
        services.AddVehicleCatalog(connectionString, "tr", ["tr", "ar", "en"], "TRY", 2, 25, 100);
        services.AddVehicleReference(connectionString);
        services.AddDealership(connectionString, "tr");

        // 0006 ile `identity` de bir modül şemasıdır; listeden düşerse Host başlangıçta
        // "şema güncel değil" diyerek reddeder ve Host.Tests'in tamamı kırılır.
        services.AddIdentity(new IdentitySetup(
            connectionString,
            "tr",
            ["tr", "ar", "en"],
            IdentityTestSettings.SeedAdminLoginId,
            IdentityTestSettings.SeedAdminPassword,
            "Kurulum Yöneticisi",
            AccessLifetimeMinutes: 30,
            RefreshLifetimeDays: 14,
            FailedAttemptThreshold: 5,
            LockoutMinutes: 15,
            MinimumPasswordLength: 12,
            PasswordIterations: 1000,
            LoginRequestLimit: 20,
            LoginRequestWindowMinutes: 10,
            TwoFactorIssuer: "Test Galeri",
            TwoFactorEncryptionKey: IdentityTestSettings.TwoFactorEncryptionKey,
            RecoveryCodeCount: 10,
            RecoveryCodeIterations: 1_000,
            PendingEnrollmentMinutes: 10,
            PendingLoginMinutes: 5));

        // 0005 ile `leads` de bir modül şemasıdır.
        services.AddLeads(connectionString, ["tr", "ar", "en"], 5, 10);

        // 0009 ile `notifications` de bir modül şemasıdır. Bu satır olmadan Host
        // başlangıçta "şema güncel değil" diyerek reddediyordu ve Host.Tests'in 31 testi
        // birden düştü — ADR 0002'nin doğrulamasının amacı tam olarak buydu.
        services.AddNotifications(new NotificationsSetup(
            connectionString,
            "tr",
            "galeri@ornek.com",
            "http://localhost",
            "bildirim@ornek.com",
            SmtpHost: "localhost",
            SmtpPort: 1025,
            SmtpUser: "test",
            SmtpPassword: "test",
            SmtpUseStartTls: false,
            MaximumAttempts: 5,
            RetryBaseSeconds: 30,
            SendTimeoutSeconds: 20));

        // 0011 ile `media` de bir modül şemasıdır. Listeden düşerse Host başlangıçta
        // "şema güncel değil" diyerek reddeder ve Host.Tests'in TAMAMI kırılır — `0009`'da
        // bu tam olarak yaşandı ve 31 test birden düştü (ADR 0002'nin doğrulaması).
        //
        // Değerler gerçek bir depolama sağlayıcısına işaret etmiyor ve etmesi de
        // gerekmiyor: bu adım yalnız ŞEMAYI uygular; `MediaOptions` doğrulaması kuruluşta
        // koştuğu için değerlerin DOLU olması yeterli.
        services.AddMedia(new MediaSetup(
            connectionString,
            StorageServiceUrl: "http://localhost:54566",
            StorageBucket: "cargallery-media-test",
            StorageAccessKey: "test",
            StorageSecretKey: "test",
            StorageRegion: "us-east-1",
            StorageForcePathStyle: true,
            PublicBaseUrl: "http://localhost:54566/cargallery-media-test/",
            WatermarkObjectKey: "private/brand/watermark.png",
            MaximumByteSize: 15_728_640,
            MaximumPixelCount: 50_000_000,
            AllowedImageFormats: ["jpeg", "png", "webp"],
            EnabledLocales: ["tr", "ar", "en"]));

        await using ServiceProvider provider = services.BuildServiceProvider();

        await provider.ApplyModuleSchemasAsync(NullLogger.Instance);
    }
}
