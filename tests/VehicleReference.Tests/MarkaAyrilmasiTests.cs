using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>
/// `0021` R4 · C2, C4 · D3 — kurulum güncellemesi: on iki model sabit eşlemeyle markasına
/// ayrılır. Ölçüm gerçek PostgreSQL'de, güncellemeden ÖNCEKİ şemadan başlar: veritabanı
/// `SeedInitialVehicleModels`'e kadar kurulur, o hâlin adları okunur, sonra güncelleme
/// uygulanır ve aynı adlar sözleşmeden okunur.
/// </summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class MarkaAyrilmasiTests(ReferenceDatabase database)
{
    private const string OncekiMigration = "20261005093818_SeedInitialVehicleModels";

    private static readonly string[] Diller = ["tr", "en", "ar"];

    [Fact(DisplayName = "0021/AC-1: Güncellemeden sonra on iki model dokuz markaya bağlıdır: BMW (3 Serisi), Ford (Focus), Honda (Civic), Hyundai (i20), Mercedes-Benz (C-Serisi), Nissan (Qashqai), Renault (Clio, Megane), Toyota (Corolla, Yaris), Volkswagen (Golf, Passat).")]
    public async Task Baglar_OnIkiModeli_DokuzMarkaya()
    {
        string baglanti = await database.NewDatabaseAsync();
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti);

        var eslesme = await context.VehicleModels
            .Join(context.VehicleBrands, model => model.BrandId, brand => brand.Id, (model, brand) => new { Marka = brand.CanonicalName, Model = model.CanonicalName })
            .ToListAsync();

        Dictionary<string, string[]> markalar = eslesme
            .GroupBy(item => item.Marka)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Model).Order(StringComparer.Ordinal).ToArray());

        Assert.Equal(12, eslesme.Count);
        Assert.Equal(9, markalar.Count);
        Assert.Equal(["3 Serisi"], markalar["BMW"]);
        Assert.Equal(["Focus"], markalar["Ford"]);
        Assert.Equal(["Civic"], markalar["Honda"]);
        Assert.Equal(["i20"], markalar["Hyundai"]);
        Assert.Equal(["C-Serisi"], markalar["Mercedes-Benz"]);
        Assert.Equal(["Qashqai"], markalar["Nissan"]);
        Assert.Equal(["Clio", "Megane"], markalar["Renault"]);
        Assert.Equal(["Corolla", "Yaris"], markalar["Toyota"]);
        Assert.Equal(["Golf", "Passat"], markalar["Volkswagen"]);
    }

    [Fact(DisplayName = "0021/AC-2: Güncellemeden sonra her modelin kanonik adı markasızdır (\"Corolla\", \"3 Serisi\", \"C-Serisi\").")]
    public async Task Markasizdir_KanonikModelAdi()
    {
        string baglanti = await database.NewDatabaseAsync();
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti);

        var adlar = await context.VehicleModels
            .Join(context.VehicleBrands, model => model.BrandId, brand => brand.Id, (model, brand) => new { Marka = brand.CanonicalName, Model = model.CanonicalName })
            .ToListAsync();

        Assert.Contains(adlar, item => item.Model == "Corolla");
        Assert.Contains(adlar, item => item.Model == "3 Serisi");
        Assert.Contains(adlar, item => item.Model == "C-Serisi");
        Assert.All(adlar, item => Assert.DoesNotContain(item.Marka, item.Model, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "0021/AC-3: Güncellemeden önce var olan araçların hepsi aynı model kimliğini taşır.")]
    public async Task Korur_ModelKimliklerini()
    {
        string baglanti = await database.NewDatabaseAsync(OncekiMigration);
        List<Guid> once = await KimliklerAsync(baglanti);

        await GuncelleAsync(baglanti);

        // Araçlar modeli KİMLİKLE tutar (soft reference, `vehicle_catalog` şeması bu kartta
        // değişmez — D15). Kimlik kümesi aynıysa hiçbir aracın modeli değişmemiştir.
        Assert.Equal(12, once.Count);
        Assert.Equal(once, await KimliklerAsync(baglanti));
    }

    [Fact(DisplayName = "0021/AC-4: Güncellemeden önce yayında olan ilanların adresleri değişmez.")]
    public async Task Korur_IlanAdreslerininKaynaginiVeSemayi()
    {
        string baglanti = await database.NewDatabaseAsync(OncekiMigration);
        Dictionary<Guid, string> once = await OncekiKanonikAdlarAsync(baglanti);

        await GuncelleAsync(baglanti);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti);
        IReadOnlyDictionary<Guid, string> sonra =
            await new VehicleModelQueries(context).ResolveCanonicalNamesAsync([.. once.Keys], CancellationToken.None);

        // Adres yazma modelinde durur ve bir daha üretilmez (`0012b` D13); güncelleme ne o
        // tabloya (başka şema) dokunur ne de adresin kaynağı olan kanonik tam adı değiştirir —
        // yani yeniden yayınlanan bir ilanın adresi bile aynı kalıptan çıkar (C2).
        Assert.Equal(once, sonra);
        Assert.Equal(0, await BaskaSemaTablosuAsync(baglanti));
    }

    [Fact(DisplayName = "0021/AC-5: Güncellemeden önce ve sonra vitrin her ilan için aynı tam adı gösterir, üç dilde de: Türkçe \"BMW 3 Serisi\", İngilizce \"BMW 3 Series\", Arapça \"تويوتا كورولا\" ve \"مرسيدس بنز الفئة C\".")]
    public async Task Gosterir_AyniTamAdi_UcDilde()
    {
        string baglanti = await database.NewDatabaseAsync(OncekiMigration);
        Dictionary<(Guid, string), string> once = await OncekiGorunenAdlarAsync(baglanti);

        await GuncelleAsync(baglanti);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti);
        VehicleModelQueries sorgu = new(context);
        List<Guid> kimlikler = [.. once.Keys.Select(key => key.Item1).Distinct()];

        foreach (string dil in Diller)
        {
            IReadOnlyDictionary<Guid, string> sonra = await sorgu.ResolveNamesAsync(kimlikler, dil, CancellationToken.None);

            foreach (Guid kimlik in kimlikler)
            {
                Assert.Equal(once[(kimlik, dil)], sonra[kimlik]);
            }
        }

        IReadOnlyDictionary<Guid, string> tr = await sorgu.ResolveNamesAsync(kimlikler, "tr", CancellationToken.None);
        IReadOnlyDictionary<Guid, string> en = await sorgu.ResolveNamesAsync(kimlikler, "en", CancellationToken.None);
        IReadOnlyDictionary<Guid, string> ar = await sorgu.ResolveNamesAsync(kimlikler, "ar", CancellationToken.None);

        Assert.Equal("BMW 3 Serisi", tr[Bmw3]);
        Assert.Equal("BMW 3 Series", en[Bmw3]);
        Assert.Equal("تويوتا كورولا", ar[Corolla]);
        Assert.Equal("مرسيدس بنز الفئة C", ar[MercedesC]);
    }

    /// <summary>
    /// Plan turu **P5** — eşlemede olmayan bir model varken güncelleme hata ile durur ve
    /// YARIM uygulanmaz: migration tek işlemde koşar, şema ve veri güncelleme öncesindeki gibi
    /// kalır (marka tablosu yok, model adı ve geçmiş kaydı değişmemiş).
    /// </summary>
    [Fact]
    public async Task Durur_EslemedeOlmayanModelde_YarimUygulanmaz()
    {
        string baglanti = await database.NewDatabaseAsync(OncekiMigration);
        await using (NpgsqlConnection connection = new(baglanti))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand ekle = new(
                "INSERT INTO vehicle_reference.vehicle_models (id, canonical_name) VALUES ('0192c000-0000-7000-8000-0000000000ff', 'Lada Niva')",
                connection);
            await ekle.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<PostgresException>(() => GuncelleAsync(baglanti));

        await using NpgsqlConnection kontrol = new(baglanti);
        await kontrol.OpenAsync();
        await using NpgsqlCommand tablo = new("SELECT to_regclass('vehicle_reference.vehicle_brands') IS NULL", kontrol);
        Assert.True((bool)(await tablo.ExecuteScalarAsync())!);
        await using NpgsqlCommand kolon = new(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = 'vehicle_reference' AND table_name = 'vehicle_models' AND column_name = 'brand_id'",
            kontrol);
        Assert.Equal(0L, (long)(await kolon.ExecuteScalarAsync())!);
        await using NpgsqlCommand gecmis = new(
            "SELECT max(\"MigrationId\") FROM vehicle_reference.__ef_migrations_history",
            kontrol);
        Assert.Equal(OncekiMigration, (string)(await gecmis.ExecuteScalarAsync())!);
        Assert.Equal("Toyota Corolla", (await OncekiKanonikAdlarAsync(baglanti))[Corolla]);
    }

    [Fact]
    public async Task GeriAlinir_TamAdlarGeriYazilir()
    {
        string baglanti = await database.NewDatabaseAsync(OncekiMigration);
        Dictionary<(Guid, string), string> once = await OncekiGorunenAdlarAsync(baglanti);
        Dictionary<Guid, string> kanonik = await OncekiKanonikAdlarAsync(baglanti);

        await GuncelleAsync(baglanti);

        await using (VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti))
        {
            await context.GetService<IMigrator>().MigrateAsync(OncekiMigration);
        }

        Assert.Equal(once, await OncekiGorunenAdlarAsync(baglanti));
        Assert.Equal(kanonik, await OncekiKanonikAdlarAsync(baglanti));
    }

    private static readonly Guid Corolla = Guid.Parse("0192c000-0000-7000-8000-00000000000a");
    private static readonly Guid Bmw3 = Guid.Parse("0192c000-0000-7000-8000-0000000000aa");
    private static readonly Guid MercedesC = Guid.Parse("0192c000-0000-7000-8000-0000000000ab");

    private static async Task GuncelleAsync(string baglanti)
    {
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(baglanti);
        await context.Database.MigrateAsync();
    }

    private static async Task<List<Guid>> KimliklerAsync(string baglanti)
    {
        List<Guid> kimlikler = [];
        await using NpgsqlConnection connection = new(baglanti);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("SELECT id FROM vehicle_reference.vehicle_models ORDER BY id", connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            kimlikler.Add(reader.GetGuid(0));
        }

        return kimlikler;
    }

    /// <summary>Güncelleme öncesi hâlde kanonik ad tek kolondur ve tam addır.</summary>
    private static async Task<Dictionary<Guid, string>> OncekiKanonikAdlarAsync(string baglanti)
    {
        Dictionary<Guid, string> adlar = [];
        await using NpgsqlConnection connection = new(baglanti);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("SELECT id, canonical_name FROM vehicle_reference.vehicle_models", connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            adlar[reader.GetGuid(0)] = reader.GetString(1);
        }

        return adlar;
    }

    private static async Task<Dictionary<(Guid, string), string>> OncekiGorunenAdlarAsync(string baglanti)
    {
        Dictionary<(Guid, string), string> adlar = [];
        await using NpgsqlConnection connection = new(baglanti);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            "SELECT vehicle_model_id, locale, name FROM vehicle_reference.vehicle_model_translations",
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            adlar[(reader.GetGuid(0), reader.GetString(1))] = reader.GetString(2);
        }

        return adlar;
    }

    /// <summary>Bu veritabanında yalnız `vehicle_reference` kurulur; başka şemada tablo doğmamalı.</summary>
    private static async Task<long> BaskaSemaTablosuAsync(string baglanti)
    {
        await using NpgsqlConnection connection = new(baglanti);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema NOT IN ('vehicle_reference', 'pg_catalog', 'information_schema')",
            connection);

        return (long)(await command.ExecuteScalarAsync())!;
    }
}
