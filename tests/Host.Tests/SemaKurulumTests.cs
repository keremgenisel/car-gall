using CarGallery.BuildingBlocks.Schema;
using CarGallery.Host.Schema;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// docs/decisions/0002 — **uygulayan kurulum adımıdır, çalışan uygulama yalnız
/// doğrular.**
///
/// Bu ayrımın iki yarısı da burada sınanıyor. Yalnız "kurulum adımı çalışıyor" demek
/// yetmez: asıl risk adımın ATLANMASIDIR ve o durumda uygulamanın sessizce açılıp ilk
/// sorguda patlaması, hatayı "tablo yok" diye okutur — "kurulum adımı atlandı" diye
/// değil.
/// </summary>
public sealed class SemaKurulumTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    [Fact(DisplayName = "Kurulum adımı her modülün şemasını uygular")]
    public async Task Uygular_HerModulunSemasini()
    {
        await TestSchema.ApplyAsync(container.GetConnectionString());

        foreach (string sema in new[] { "vehicle_catalog", "vehicle_reference", "dealership" })
        {
            Assert.True(await SemaVarAsync(sema), $"'{sema}' şeması oluşmadı.");
        }
    }

    /// <summary>
    /// Modül listesi ELLE TUTULMUYOR: şemalar DI'dan geliyor. Bu test, sayının kayıtlı
    /// modül sayısıyla birlikte büyüdüğünü sabitler — yeni bir modül eklendiğinde
    /// kurulum adımına katılmayı unutmak mümkün olmasın.
    /// </summary>
    [Fact(DisplayName = "Kurulum adımı kayıtlı her modülü görür")]
    public async Task Gorur_KayitliHerModulu()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddVehicleCatalog(container.GetConnectionString(), "tr", ["tr", "ar", "en"], "TRY", 2, 25, 100);
        services.AddVehicleReference(container.GetConnectionString());
        services.AddDealership(container.GetConnectionString(), "tr");

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        string[] adlar = [.. scope.ServiceProvider.GetServices<IModuleSchema>().Select(schema => schema.Name).Order()];

        Assert.Equal(["Dealership", "VehicleCatalog", "VehicleReference"], adlar);
    }

    [Fact(DisplayName = "Kurulum adımı hiç şema bulamazsa sessizce başarılı olmaz")]
    public async Task Patlar_HicSemaYokken()
    {
        ServiceCollection services = new();
        services.AddLogging();

        await using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException hata = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ApplyModuleSchemasAsync(NullLogger.Instance));

        Assert.Contains("modül şeması bulunamadı", hata.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kararın asıl bedeli: dağıtım bir adım kazandı ve o adım ATLANABİLİR. Uygulama bu
    /// yüzden sessizce açılmıyor — açık bir mesajla reddediyor ve mesaj çalıştırılacak
    /// komutu söylüyor.
    /// </summary>
    [Fact(DisplayName = "Kurulum adımı atlanmışsa uygulama başlamaz ve ne yapılacağını söyler")]
    public async Task Baslamaz_KurulumAdimiAtlanmissa()
    {
        // Şema UYGULANMADI: taze veritabanı.
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());

                // 0006 başlangıç sözleşmesini değiştirdi: başlangıç Yönetici parolası
                // olmadan uygulama HİÇ kurulmaz ve şema doğrulamasına bile gelemez.
                // Bu testlerin ölçtüğü şey şema kapısı, o yüzden bu ayar verilmeli.
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
            });

        InvalidOperationException hata = Assert.Throws<InvalidOperationException>(
            () => factory.CreateClient());

        Assert.Contains("şeması güncel değil", hata.Message, StringComparison.Ordinal);
        Assert.Contains(ModuleSchemaOperations.MigrateArgument, hata.Message, StringComparison.Ordinal);

        await Task.CompletedTask;
    }

    [Fact(DisplayName = "Kurulum adımı uygulandıktan sonra uygulama başlar")]
    public async Task Baslar_KurulumAdimindanSonra()
    {
        await TestSchema.ApplyAsync(container.GetConnectionString());

        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());

                // 0006 başlangıç sözleşmesini değiştirdi: başlangıç Yönetici parolası
                // olmadan uygulama HİÇ kurulmaz ve şema doğrulamasına bile gelemez.
                // Bu testlerin ölçtüğü şey şema kapısı, o yüzden bu ayar verilmeli.
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
            });

        HttpResponseMessage yanit = await factory.CreateClient().GetAsync("/api/catalog/field-settings");

        Assert.True(yanit.IsSuccessStatusCode);
    }

    /// <summary>İkinci koşu hiçbir şey değiştirmez; kurulum adımı idempotenttir.</summary>
    [Fact(DisplayName = "Kurulum adımı iki kez çalıştırılabilir")]
    public async Task Idempotenttir_KurulumAdimi()
    {
        await TestSchema.ApplyAsync(container.GetConnectionString());
        await TestSchema.ApplyAsync(container.GetConnectionString());

        Assert.True(await SemaVarAsync("dealership"));
    }

    private async Task<bool> SemaVarAsync(string sema)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = @sema";
        command.Parameters.AddWithValue("sema", sema);

        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}
