using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>
/// Plan turu **P2** — tekillik ön denetimi yarışta geçilebilir; ikinci yazmayı tekil indeks
/// düşürür ve komut hatayı DOĞRU koda çevirir (`500` değil). Yarış belirlenimci kurulur:
/// komutun bağlamı kaydetmeden hemen önce (ön denetimden SONRA) başka bir bağlantı çakışan
/// satırı yazar — gerçek bir eşzamanlı istek tam olarak bu aralığa düşer.
/// </summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class TekillikYarisiTests(ReferenceDatabase database)
{
    [Fact]
    public async Task Doner_SlugTaken_KisaltmaYarisiIndekstenDusunce()
    {
        string db = await database.NewDatabaseAsync();
        BrandCommands komut = new(Baglam(db, "INSERT INTO vehicle_reference.vehicle_brands (id, canonical_name, canonical_key, slug, is_active, show_on_home, version) VALUES (gen_random_uuid(), 'Alfa-Romeo', 'ALFA-ROMEO', 'alfa-romeo', TRUE, FALSE, 1)"), MarkaYonetimiTests.Diller);

        Result<(Guid, int)> sonuc = await komut.CreateAsync(new ReferenceNameInput("Alfa Romeo", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.BrandSlugTaken, sonuc.Error.Code);
    }

    [Fact]
    public async Task Doner_NameTaken_AdYarisiIndekstenDusunce()
    {
        string db = await database.NewDatabaseAsync();
        BrandCommands komut = new(Baglam(db, "INSERT INTO vehicle_reference.vehicle_brands (id, canonical_name, canonical_key, slug, is_active, show_on_home, version) VALUES (gen_random_uuid(), 'LANCIA', 'LANCIA', 'lancia-x', TRUE, FALSE, 1)"), MarkaYonetimiTests.Diller);

        Result<(Guid, int)> sonuc = await komut.CreateAsync(new ReferenceNameInput("Lancia", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.BrandNameTaken, sonuc.Error.Code);
    }

    [Fact]
    public async Task Doner_ModelNameTaken_ModelYarisiIndekstenDusunce()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await MarkaYonetimiTests.KimlikAsync(db, "toyota");
        ModelCommands komut = new(Baglam(db, $"INSERT INTO vehicle_reference.vehicle_models (id, brand_id, canonical_name, canonical_key) VALUES (gen_random_uuid(), '{toyota}', 'C-HR', 'C-HR')"), MarkaYonetimiTests.Diller);

        Result<(Guid, int)> sonuc = await komut.CreateAsync(toyota, new ReferenceNameInput("c-hr", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.ModelNameTaken, sonuc.Error.Code);
    }

    /// <summary>Kaydetmeden hemen önce çakışan satırı BAŞKA bir bağlantıdan yazan bağlam.</summary>
    private static VehicleReferenceDbContext Baglam(string baglanti, string cakisan)
    {
        DbContextOptionsBuilder<VehicleReferenceDbContext> builder = new();
        builder.UseNpgsql(
            baglanti,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema));
        builder.AddInterceptors(new CakisanYazici(baglanti, cakisan));
        return new VehicleReferenceDbContext(builder.Options);
    }

    private sealed class CakisanYazici(string baglanti, string sql) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await using NpgsqlConnection connection = new(baglanti);
            await connection.OpenAsync(cancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
