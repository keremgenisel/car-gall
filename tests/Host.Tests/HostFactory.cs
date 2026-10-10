using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// Gerçek wwwroot, frontend build'inin çıktısıdır ve repoda durmaz. Bu testler lang/dir
/// enjeksiyonunu doğrular, paketlenmiş bundle'ı değil; bu yüzden sabit bir test kökü
/// kullanırlar ve frontend'in derlenmiş olmasına bağımlı DEĞİLLERDİR.
///
/// Veritabanı GEREKLİ ve bunun iki ayrı sebebi var:
/// (1) uygulama başlangıçta şemanın güncel olduğunu doğruluyor ve eksikse açıkça
/// reddediyor (docs/decisions/0002); (2) kurulum Yönetici hesabı garanti ediliyor,
/// aksi hâlde hiç kimse giriş yapamaz ve sistem kalıcı olarak kapalı kalır (0006/D2).
/// İkisini de teste özel bir bayrakla atlamak, üretimde atlanabilecek bir yol açardı —
/// testin veritabanı alması daha ucuz.
/// </summary>
public sealed class HostFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        // Kurulum adımı uygulamadan ÖNCE koşar; uygulama onu beklediği için tersi olmaz.
        await TestSchema.ApplyAsync(container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
        builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
        IdentityTestSettings.Apply(builder);
        NotificationsTestSettings.Apply(builder);
        MediaTestSettings.Apply(builder);
    }
}
