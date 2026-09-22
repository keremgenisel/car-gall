using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// **L2** — talep kendi gerçeğini korur. Buradaki testlerin hepsi aynı soruyu soruyor:
/// kayıt, kaynağından BAĞIMSIZ mı? Bu yüzden hepsi talebi veritabanından YENİDEN OKUR;
/// bellekteki nesneyi doğrulamak yalnızca "değeri atadık" derdi.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class AnlikGoruntuTests(LeadsDatabase database)
{
    [Fact(DisplayName = "0005/AC-17: Talep, ilgilenilen aracın o andaki model adını ve fiyatını kendi içinde taşır")]
    public async Task Tasir_OAndakiModelAdiniVeFiyatini()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Olustur().Value;
        await KaydetAsync(talep);

        Lead okunan = (await OkuAsync(talep.Id))!;

        Assert.Equal("Toyota Corolla", okunan.Vehicle.CanonicalModelName);
        Assert.Equal(1_250_000m, okunan.Vehicle.PriceAmount);
        Assert.Equal("TRY", okunan.Vehicle.PriceCurrencyCode);
        Assert.Equal(2, okunan.Vehicle.PriceMinorUnitExponent);
    }

    [Fact(DisplayName = "0005/AC-18: Anlık görüntüdeki model adı kanonik Latin adıdır; Arapça sayfadan bırakılan talepte de kanonik ad saklanır")]
    public async Task Saklanir_KanonikLatinAd_ArapcaSayfadanBirakildiginda()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Locale("ar").Olustur().Value;
        await KaydetAsync(talep);

        Lead okunan = (await OkuAsync(talep.Id))!;

        Assert.Equal("ar", okunan.SubmittedLocale.Code);
        Assert.Equal("Toyota Corolla", okunan.Vehicle.CanonicalModelName);
        Assert.All(okunan.Vehicle.CanonicalModelName, karakter => Assert.True(karakter < 0x0600));
    }

    [Fact(DisplayName = "0005/AC-20: Talep oluşturulduktan sonra araç arşivlendiğinde talep okunabilir kalır ve model adı ile fiyatını göstermeye devam eder")]
    public async Task OkunabilirKalir_AracArsivlendiginde()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Olustur().Value;
        await KaydetAsync(talep);

        // Araç arşivlendi: vitrin okuma modelinde satırı artık YOK, kaynak `null` döner.
        // Talep buna rağmen okunur — çünkü hiçbir alanı kaynaktan çözülmüyor.
        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.Equal("Toyota Corolla", okunan.Vehicle.CanonicalModelName);
        Assert.Equal(1_250_000m, okunan.Vehicle.PriceAmount);
        Assert.False(okunan.Vehicle.PriceOnRequest);
    }

    [Fact(DisplayName = "0005/AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    public async Task Tasir_FiyatSorunuzDurumunu()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder()
            .AnlikGoruntu(LeadBuilder.FiyatSorunuzAnlikGoruntusu())
            .Olustur()
            .Value;

        await KaydetAsync(talep);

        Lead okunan = (await OkuAsync(talep.Id))!;

        Assert.True(okunan.Vehicle.PriceOnRequest);
        Assert.Null(okunan.Vehicle.PriceAmount);
        Assert.Null(okunan.Vehicle.PriceCurrencyCode);
        Assert.True(okunan.Vehicle.ReadPrice().IsOnRequest);

        // Kolonun kendisi de NULL: "0" olarak kopyalanmadığı yalnız burada görünür.
        await using LeadsDbContext context = database.CreateContext();
        decimal? tutar = await context.Database
            .SqlQuery<decimal?>($"SELECT vehicle_price_amount AS \"Value\" FROM leads.leads LIMIT 1")
            .SingleAsync();

        Assert.Null(tutar);
    }

    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public async Task UcOndalikSaklanir_KwdTutari()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder()
            .AnlikGoruntu(LeadBuilder.KuveytAnlikGoruntusu())
            .Olustur()
            .Value;

        await KaydetAsync(talep);

        Lead okunan = (await OkuAsync(talep.Id))!;

        Assert.Equal(1250.500m, okunan.Vehicle.PriceAmount);
        Assert.Equal("KWD", okunan.Vehicle.PriceCurrencyCode);
        Assert.Equal(3, okunan.Vehicle.PriceMinorUnitExponent);
        Assert.Equal(1250.500m, okunan.Vehicle.ReadPrice().Amount!.Value.Amount);
    }

    /// <summary>
    /// AC-22'nin sayısı (`1250,500`) kesmeyi yakalayamaz: iki ondalığa kesilse de aynı
    /// sayıdır. Gidiş-dönüşün gerçekten kesmediği yalnız üçüncü ondalığı sıfır OLMAYAN
    /// bir tutarla görülür — `decimal(_,2)` hatası tam olarak burada kırmızı verir.
    /// </summary>
    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public async Task KesilmezVeYuvarlanmaz_UcuncuOndaligiSifirOlmayanKwdTutari()
    {
        await database.ResetAsync();

        VehicleSnapshot anlikGoruntu = VehicleSnapshot.Create(
            Guid.CreateVersion7(),
            "Toyota Corolla",
            PriceSnapshot.Of(Money.Create(1250.505m, "KWD", 3).Value)).Value;

        Lead talep = new LeadBuilder().AnlikGoruntu(anlikGoruntu).Olustur().Value;
        await KaydetAsync(talep);

        Lead okunan = (await OkuAsync(talep.Id))!;

        Assert.Equal(1250.505m, okunan.Vehicle.PriceAmount);
        Assert.NotEqual(1250.50m, okunan.Vehicle.PriceAmount);
        Assert.NotEqual(1250.51m, okunan.Vehicle.PriceAmount);
    }

    private async Task KaydetAsync(Lead talep)
    {
        await using LeadsDbContext context = database.CreateContext();
        context.Leads.Add(talep);
        await database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
            .SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Lead?> OkuAsync(LeadId id)
    {
        await using LeadsDbContext context = database.CreateContext();

        return await context.Leads.AsNoTracking().SingleOrDefaultAsync(lead => lead.Id == id);
    }
}
