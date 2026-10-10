using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Tipli opsiyonel değerin kalıcılığı (0010b **D17**, **D22**).
///
/// <para>Gerçek PostgreSQL şart: sınanan şey KOLONUN TİPİ. Bir sahte depo "tutar metin
/// olarak saklanmıyor" iddiasını hiçbir biçimde çürütemezdi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class OpsiyonelAlanKaliciligiTests(ShowcaseDatabase database)
{
    [Fact(DisplayName = "0010b/AC-10: `damage_record_amount` tutar tipindedir ve `Money` ile saklanır; metin olarak saklanmaz")]
    public async Task Saklanir_TramerTutarOlarak()
    {
        Vehicle arac = new VehicleBuilder().WithVin($"OPT{Guid.CreateVersion7():N}"[..24]).Build();
        arac.SetOptionalFieldValue(
            VehicleFieldKeys.DamageRecordAmount,
            OptionalFieldValue.OfAmount(Money.Create(12_500.75m, "TRY", 2).Value));

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(arac);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        Vehicle okunan = await okuma.Vehicles
            .Include(vehicle => vehicle.OptionalFieldValues)
            .SingleAsync(vehicle => vehicle.Id == arac.Id);

        OptionalFieldValue deger = okunan.OptionalFieldValues.Single().Value;

        Assert.Equal(FieldValueKind.Amount, deger.Kind);
        Assert.Equal(12_500.75m, deger.Amount!.Value.Amount);
        Assert.Equal("TRY", deger.Amount.Value.CurrencyCode);
        Assert.Equal(2, deger.Amount.Value.MinorUnitExponent);

        // Metin taşıyıcıları BOŞ: değer bir metin kovasına düşmedi.
        Assert.Null(deger.Text);
        Assert.Null(deger.Localized);
    }

    /// <summary>
    /// Kolonun kendi tipi sorgulanıyor. Yukarıdaki test değerin gidip geldiğini gösterir
    /// ama değer `text` kolonunda da gidip gelirdi; Altın Kural 4'ün sınandığı yer burası.
    /// </summary>
    [Fact(DisplayName = "0010b/AC-10: `damage_record_amount` tutar tipindedir ve `Money` ile saklanır; metin olarak saklanmaz")]
    public async Task Kolon_NumericTipinde()
    {
        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = new(
            """
            SELECT data_type, numeric_precision, numeric_scale
            FROM information_schema.columns
            WHERE table_schema = 'vehicle_catalog'
              AND table_name = 'vehicle_optional_field_values'
              AND column_name = 'value_amount'
            """,
            connection);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "`value_amount` kolonu yok.");
        Assert.Equal("numeric", reader.GetString(0));
        Assert.Equal(19, reader.GetInt32(1));
        Assert.Equal(4, reader.GetInt32(2));
    }

    [Fact]
    public async Task GidipGelir_TarihVeEvetHayir()
    {
        Vehicle arac = new VehicleBuilder().WithVin($"OPT{Guid.CreateVersion7():N}"[..24]).Build();

        // Defterde bugün yalnız tramer var; tip tesisatının geri kalanı bu testle
        // sınanıyor — alan anahtarı uydurma, çünkü ölçülen şey DEPO, komut değil.
        arac.SetOptionalFieldValue("tarih_alani", OptionalFieldValue.OfDate(new DateOnly(2024, 3, 15)));
        arac.SetOptionalFieldValue("bayrak_alani", OptionalFieldValue.OfBoolean(true));

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(arac);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        Vehicle okunan = await okuma.Vehicles
            .Include(vehicle => vehicle.OptionalFieldValues)
            .SingleAsync(vehicle => vehicle.Id == arac.Id);

        Assert.Equal(
            new DateOnly(2024, 3, 15),
            okunan.OptionalFieldValues.Single(item => item.FieldKey == "tarih_alani").Value.Date);

        Assert.True(okunan.OptionalFieldValues.Single(item => item.FieldKey == "bayrak_alani").Value.Boolean);
    }

    [Fact(DisplayName = "0010b/AC-18: Personel ilan başlığını ve açıklamasını TR, EN ve AR için ayrı ayrı yazar ve üçü bağımsız saklanır")]
    public async Task GidipGelir_UcDildeIlanMetni()
    {
        Vehicle arac = new VehicleBuilder().WithVin($"TXT{Guid.CreateVersion7():N}"[..24]).Build();
        Listing ilan = Listing.CreateDraft(arac.Id, showcaseSortWeight: 0);

        ilan.SetText(
            Metin(("tr", "Temiz Corolla"), ("en", "Clean Corolla"), ("ar", "كورولا نظيفة")),
            Metin(("tr", "Bakımları yapıldı"), ("en", "Fully serviced"), ("ar", "صيانة كاملة")));

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(arac);
            yazma.Listings.Add(ilan);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        Listing okunan = await okuma.Listings.SingleAsync(item => item.Id == ilan.Id);

        Locale tr = Locale.Create("tr").Value;
        Locale ar = Locale.Create("ar").Value;

        Assert.Equal("Temiz Corolla", okunan.Title.Resolve(tr, tr));
        Assert.Equal("كورولا نظيفة", okunan.Title.Resolve(ar, tr));
        Assert.Equal("صيانة كاملة", okunan.Description.Resolve(ar, tr));
        Assert.Empty(okunan.MissingLocales([tr, ar]));
    }

    /// <summary>
    /// Karşılaştırıcı olmadan EF `LocalizedText`'i referansla karşılaştırır ve güncelleme
    /// SESSİZCE kaybolur. `0010a`'da aynı tuzağa karşı aynı önlem alınmıştı; burada metin
    /// kolonları için tekrar sınanıyor.
    /// </summary>
    [Fact]
    public async Task Kaydeder_MetinDegisikligini()
    {
        Vehicle arac = new VehicleBuilder().WithVin($"UPD{Guid.CreateVersion7():N}"[..24]).Build();
        Listing ilan = Listing.CreateDraft(arac.Id, showcaseSortWeight: 0);
        ilan.SetText(Metin(("tr", "Önce")), Metin(("tr", "Önce açıklama")));

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(arac);
            yazma.Listings.Add(ilan);
            await yazma.SaveChangesAsync();
        }

        await using (VehicleCatalogDbContext guncelleme = database.CreateContext())
        {
            Listing izlenen = await guncelleme.Listings.SingleAsync(item => item.Id == ilan.Id);
            izlenen.SetText(Metin(("tr", "Sonra")), Metin(("tr", "Sonra açıklama")));
            await guncelleme.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Listing okunan = await okuma.Listings.SingleAsync(item => item.Id == ilan.Id);

        Locale tr = Locale.Create("tr").Value;

        Assert.Equal("Sonra", okunan.Title.Resolve(tr, tr));
    }

    private static LocalizedText Metin(params (string Code, string Text)[] values) =>
        LocalizedText.From(values.Select(item =>
            new KeyValuePair<Locale, string>(Locale.Create(item.Code).Value, item.Text)));
}
