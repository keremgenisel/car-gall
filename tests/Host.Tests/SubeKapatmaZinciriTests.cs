using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// C17 — bu test iki modülü birlikte kapsar ve modül test projelerinde YAŞAYAMAZ:
/// docs/testing.md § 1 bir test projesinin yalnız kendi modülüne ve o modülün
/// Contracts'ına referans vermesini zorunlu kılar. Host zaten ikisini de kaydediyor.
///
/// Zincir: Dealership şubeyi kapatır → duyuru outbox'a yazılır → dağıtıcı teslim eder →
/// VehicleCatalog o şubedeki ilanları vitrinden düşürür (A8).
/// </summary>
public sealed class SubeKapatmaZinciriTests : IAsyncLifetime
{
    private static readonly Guid KapatilacakSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");
    private static readonly Guid AcikKalanSube = Guid.Parse("00000000-0000-7000-8000-00000000b002");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        // Kurulum adımı fabrikadan ÖNCE koşar: uygulama şemanın güncel olduğunu
        // başlangıçta doğruluyor ve eksikse ayağa kalkmıyor (docs/decisions/0002).
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
        });

        await SeedReferansVerisiAsync();
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0003/AC-32: BranchDeactivated duyurusu VehicleCatalog tarafından alındığında o şubedeki araçlar vitrinden düşer")]
    public async Task VitrindenDuser_SubeKapatildiginda()
    {
        await SeedShowcaseAsync();

        Assert.Equal(1, await SayAsync(KapatilacakSube));
        Assert.Equal(1, await SayAsync(AcikKalanSube));

        // Dealership şubeyi kapatır — duyuru iş verisiyle aynı transaction'da outbox'a düşer.
        await KapatAsync(KapatilacakSube);

        // Arka plan dağıtıcısı teslim edene kadar bekle. Bu bekleme zincirin GERÇEKTEN
        // çalıştığının kanıtıdır: elle handler çağırmıyoruz.
        await BekleAsync(async () => await SayAsync(KapatilacakSube) == 0);

        Assert.Equal(0, await SayAsync(KapatilacakSube));
        Assert.Equal(1, await SayAsync(AcikKalanSube));
    }

    /// <summary>
    /// Yalnız DEMO VERİSİ; şema kurulum adımında uygulandı. İkisinin ayrılması bu
    /// kararın görünür sonucu: eskiden seed metodu yan etki olarak migration da
    /// uyguluyordu ve hiçbir test kurulum adımına bağımlı değildi.
    /// </summary>
    private async Task SeedReferansVerisiAsync()
    {
        // `dealership` şemasını kurulum adımı uyguluyor (docs/decisions/0002); burada
        // ikinci kez uygulamak, testi kurulum adımının kendisine karşı kör ederdi.
        // Geriye vitrin tarafının bağımlılıkları kalıyor: VehicleReference liste
        // sorgusunun, VehicleCatalog ise alıcının kendisi.
        await factory!.Services.SeedVehicleReferenceForDevelopmentAsync(Guid.CreateVersion7());
        await factory.Services.SeedVehicleCatalogForDevelopmentAsync(0, Guid.CreateVersion7());
    }

    private async Task SeedShowcaseAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM vehicle_catalog.showcase_listings;
            INSERT INTO vehicle_catalog.showcase_listings
                (listing_id, vehicle_id, slug, is_reserved, showcase_sort_weight, published_at,
                 body_type_slug, vehicle_model_id, model_year, mileage_kilometers,
                 price_on_request, branch_id, branch_names, cover_asset_id, cover_media_url)
            VALUES
                (gen_random_uuid(), gen_random_uuid(), 'kapatilacak-sube-2020-aaaaaa',
                 false, 1, now(), 'sedan', gen_random_uuid(),
                 2020, 1000, true, @kapatilacak, @adlarA::jsonb, NULL, NULL),
                (gen_random_uuid(), gen_random_uuid(), 'acik-kalan-sube-2021-bbbbbb',
                 false, 1, now(), 'sedan', gen_random_uuid(),
                 2021, 2000, true, @acikKalan, @adlarB::jsonb, NULL, NULL);
            """;
        command.Parameters.AddWithValue("kapatilacak", KapatilacakSube);
        command.Parameters.AddWithValue("acikKalan", AcikKalanSube);
        command.Parameters.AddWithValue("adlarA", "{\"tr\":\"Kadıköy Şubesi\"}");
        command.Parameters.AddWithValue("adlarB", "{\"tr\":\"Ataşehir Şubesi\"}");
        await command.ExecuteNonQueryAsync();
    }

    private async Task KapatAsync(Guid branchId)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

        await using (NpgsqlCommand durum = connection.CreateCommand())
        {
            durum.Transaction = transaction;
            durum.CommandText = $"UPDATE dealership.branches SET status = 3 WHERE id = '{branchId}'";
            await durum.ExecuteNonQueryAsync();
        }

        // Duyuru iş verisiyle AYNI transaction'da yazılır (R11/AC-25). Bu test yazma ucu
        // olmadığı için (D1) aynı garantiyi SQL seviyesinde kurar.
        await using (NpgsqlCommand duyuru = connection.CreateCommand())
        {
            duyuru.Transaction = transaction;
            duyuru.CommandText = """
                INSERT INTO dealership.outbox_messages
                    (id, type, payload, occurred_at, attempt_count, next_attempt_at)
                VALUES (@id, @type, @payload::jsonb, now(), 0, now())
                """;
            duyuru.Parameters.AddWithValue("id", Guid.CreateVersion7());
            duyuru.Parameters.AddWithValue("type", "CarGallery.Modules.Dealership.Contracts.Events.BranchDeactivated");
            duyuru.Parameters.AddWithValue("payload", JsonSerializer.Serialize(new
            {
                EventId = Guid.CreateVersion7(),
                OccurredAt = DateTimeOffset.UtcNow,
                BranchId = branchId,
            }));
            await duyuru.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<int> SayAsync(Guid branchId)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM vehicle_catalog.showcase_listings WHERE branch_id = '{branchId}'";

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task BekleAsync(Func<Task<bool>> kosul)
    {
        DateTimeOffset son = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < son)
        {
            if (await kosul())
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Zincir 30 saniyede tamamlanmadı: duyuru teslim edilmedi.");
    }
}
