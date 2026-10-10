using CarGallery.Modules.VehicleCatalog.Application.Development;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// 0010a/D12 — geliştirme seed'i YAZMA modelini de yazar.
///
/// <para>Bu spec'e kadar agregatlar bellekte kurulup atılıyordu. Yazma modeli gelince o
/// hâl bir yalana dönüşürdü: arkasında araç kaydı olmayan iki yüz vitrin satırı ve bomboş
/// bir panel listesi. Kanıt görüntüleri de o yalanı gösterirdi.</para>
///
/// <para>Kendi veritabanını kurar: paylaşılan fikstür başka testlerin yazdığı satırları
/// taşıyor ve "seed kaç satır yazdı" sorusu ancak boş bir şemada ölçülebilir
/// (docs/testing.md § 4).</para>
/// </summary>
public sealed class SeedYazmaModeliTests : IAsyncLifetime
{
    /// <summary>Kurulumun tek para birimi; seed tramer tutarını `Money` ile yazıyor (0010b D17).</summary>
    private static readonly CatalogCurrency Para = new("TRY", 2);

    private readonly Testcontainers.PostgreSql.PostgreSqlContainer container =
        new Testcontainers.PostgreSql.PostgreSqlBuilder("postgres:17-alpine").WithDatabase("cargallery").Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using VehicleCatalogDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    [Fact]
    public async Task Yazar_AracVeIlanKayitlarini_VitrinSatirlariylaBirlikte()
    {
        const int sayi = 12;

        await using (VehicleCatalogDbContext yazma = CreateContext())
        {
            await new ShowcaseSeeder(yazma, new FixedClock(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)), Para)
                .SeedAsync(sayi, Guid.Parse("0192c000-0000-7000-8000-00000000000a"), CancellationToken.None);
        }

        await using VehicleCatalogDbContext okuma = CreateContext();

        Assert.Equal(sayi, await okuma.Vehicles.CountAsync());
        Assert.Equal(sayi, await okuma.Listings.CountAsync());

        // Her aracın bir ilk fiyat kaydı var (A6).
        Assert.Equal(sayi, await okuma.PriceChanges.CountAsync());

        // Vitrin satırları da duruyor ve her birinin arkasında gerçek bir araç var.
        List<Guid> vitrinAracIdleri = await okuma.ShowcaseListings
            .AsNoTracking()
            .Select(row => row.VehicleId)
            .ToListAsync();

        Assert.NotEmpty(vitrinAracIdleri);

        List<Guid> aracIdleri = await okuma.Vehicles.AsNoTracking().Select(vehicle => vehicle.Id.Value).ToListAsync();

        Assert.All(vitrinAracIdleri, id => Assert.Contains(id, aracIdleri));
    }

    /// <summary>
    /// Seed'in ürettiği şaseler TEKİL olmak zorunda: aynı şase iki kez üretilse kısmi
    /// tekil indeks ilk koşuda patlar ve geliştirme ortamı hiç kalkmaz.
    /// </summary>
    [Fact]
    public async Task Uretir_TekilSaseler()
    {
        await using (VehicleCatalogDbContext yazma = CreateContext())
        {
            await new ShowcaseSeeder(yazma, new FixedClock(DateTimeOffset.UnixEpoch), Para)
                .SeedAsync(30, Guid.CreateVersion7(), CancellationToken.None);
        }

        await using VehicleCatalogDbContext okuma = CreateContext();

        List<string> saseler = await okuma.Vehicles.AsNoTracking().Select(vehicle => vehicle.Vin.Value).ToListAsync();

        Assert.Equal(saseler.Count, saseler.Distinct(StringComparer.Ordinal).Count());
    }

    private VehicleCatalogDbContext CreateContext()
    {
        DbContextOptionsBuilder<VehicleCatalogDbContext> builder = new();
        builder.UseNpgsql(
            container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema));

        return new VehicleCatalogDbContext(builder.Options);
    }
}
