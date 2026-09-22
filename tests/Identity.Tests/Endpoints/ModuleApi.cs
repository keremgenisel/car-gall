using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts.Setup;
using CarGallery.Modules.Identity.Tests.Integration;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CarGallery.Modules.Identity.Tests.Endpoints;

/// <summary>
/// Modülün endpoint'lerini Host projesine bağlanmadan ayağa kaldırır. Test projesi
/// yalnızca kendi modülüne referans verir (docs/testing.md § 1).
///
/// `Dealership` sınırın ötesindedir ve sahtelenir.
///
/// Boru hattı burada modülün KENDİ `UseIdentity()`'siyle kurulur; Host'un aynı çağrıyı
/// yapıp yapmadığı ayrı bir sorudur ve `Host.Tests`'te sınanır — burada sınanmış
/// sayılsaydı, Host'unki eksikken de yeşil kalan bir test olurdu.
/// </summary>
internal sealed class ModuleApi : IAsyncDisposable
{
    private readonly WebApplication app;

    private ModuleApi(WebApplication app)
    {
        this.app = app;

        // `GetTestClient()` çerez TAŞIMAZ; oturum çerezi olmadan her yönetim isteği
        // 401 olurdu ve testler kriteri değil kendi eksiğini ölçerdi.
        //
        // Taban adres `https`: oturum çerezi `Secure` işaretlidir ve `CookieContainer`
        // güvenli bir çerezi `http` üzerinden saklamaz. Testin çerezi taşıyabilmesi için
        // şemanın gerçeği yansıtması gerekiyor.
        Client = new HttpClient(new CookieHandler(app.GetTestServer().CreateHandler()))
        {
            BaseAddress = new Uri("https://localhost"),
        };
    }

    internal HttpClient Client { get; }

    internal static async Task<ModuleApi> StartAsync(IdentityDatabase database)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddIdentity(new IdentitySetup(
            database.ConnectionString,
            "tr",
            ["tr", "ar", "en"],
            SeedYoneticiKimligi,
            SeedYoneticiParolasi,
            "Kurulum Yöneticisi",
            AccessLifetimeMinutes: 30,
            RefreshLifetimeDays: 14,
            FailedAttemptThreshold: 5,
            LockoutMinutes: 15,
            MinimumPasswordLength: 12,
            PasswordIterations: StaffBuilder.TestIterasyon,
            LoginRequestLimit: 1_000,
            LoginRequestWindowMinutes: 10));

        builder.Services.Replace(ServiceDescriptor.Scoped<IBranchValidator, FakeBranchValidator>());

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseIdentity();
        app.MapIdentity();

        await app.StartAsync();
        await app.Services.MigrateIdentityAsync();

        return new ModuleApi(app);
    }

    /// <summary>
    /// Tarayıcı gibi davranır: `Set-Cookie`'yi saklar, sonraki isteklerde geri gönderir.
    /// `SameSite` kısıtını uygulamaz — onu sunucu tarafındaki `CrossOriginGuard`
    /// sınıyor (AC-30), burada sınanan şey oturumun taşınması.
    /// </summary>
    private sealed class CookieHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly CookieContainer cookies = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri!;
            string header = cookies.GetCookieHeader(uri);

            if (header.Length > 0)
            {
                request.Headers.Remove("Cookie");
                request.Headers.Add("Cookie", header);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
            {
                foreach (string setCookie in setCookies)
                {
                    cookies.SetCookies(uri, setCookie);
                }
            }

            return response;
        }
    }

    internal const string SeedYoneticiKimligi = "kurulum@ornek.com";
    internal const string SeedYoneticiParolasi = "kurulum-parolasi-1";

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
