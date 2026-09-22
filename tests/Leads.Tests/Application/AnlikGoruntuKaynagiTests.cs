using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.Leads.Tests.Application;

/// <summary>
/// Adaptörün tek kararı: Catalog'un verdiği fiyat TALEBE KOPYALANABİLİR Mİ?
///
/// "Fiyat sorunuz" ile "fiyat çözülemedi" AYRI şeylerdir. İlki P4'te tanımlı bir iş
/// durumudur; ikincisi kaynağın bozuk olmasıdır. İkisini aynı değere indirmek, fiyatı
/// olan bir araç için bırakılan talebi kalıcı olarak "fiyat sorunuz" gösterirdi ve
/// kopya sonradan düzeltilemez (L2, Bilinen tuzak 2).
/// </summary>
public sealed class AnlikGoruntuKaynagiTests
{
    private static readonly Guid ListingId = Guid.Parse("0192c000-0000-7000-8000-0000000000b1");

    [Fact(DisplayName = "AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    public async Task Kopyalar_FiyatSorunuzDurumunu()
    {
        ListingSnapshot? sonuc = await CozAsync(ShowcasePriceDto.OnRequestPrice());

        Assert.NotNull(sonuc);
        Assert.NotNull(sonuc.Price);
        Assert.True(sonuc.Price.Value.IsOnRequest);
    }

    [Fact(DisplayName = "AC-17: Talep, ilgilenilen aracın o andaki model adını ve fiyatını kendi içinde taşır")]
    public async Task Kopyalar_GecerliFiyati()
    {
        ListingSnapshot? sonuc = await CozAsync(new ShowcasePriceDto(false, 1_250_000m, "TRY", 2));

        Assert.NotNull(sonuc);
        Assert.NotNull(sonuc.Price);
        Assert.False(sonuc.Price.Value.IsOnRequest);
        Assert.Equal(1_250_000m, sonuc.Price.Value.Amount!.Value.Amount);
    }

    /// <summary>
    /// Bu üç kaynak da `Money.Create`'i reddettirir. Eskiden üçü de sessizce "fiyat
    /// sorunuz"a düşüyordu; artık çözülemediğini söylüyor ve çağıran talebi reddediyor.
    /// </summary>
    [Theory(DisplayName = "AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    [InlineData(-1, "TRY", 2)]
    [InlineData(1250, "TRYX", 2)]
    [InlineData(1250, "try", 2)]
    [InlineData(1250.5005, "TRY", 2)]
    public async Task CozulemezBildirir_BozukFiyatKaynagi(decimal tutar, string kod, int us)
    {
        ListingSnapshot? sonuc = await CozAsync(new ShowcasePriceDto(false, tutar, kod, us));

        Assert.NotNull(sonuc);
        Assert.Null(sonuc.Price);
    }

    [Theory(DisplayName = "AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task CozulemezBildirir_EksikFiyatAlani(bool tutarVar, bool kodVar, bool usVar)
    {
        ShowcasePriceDto fiyat = new(
            false,
            tutarVar ? 1250m : null,
            kodVar ? "TRY" : null,
            usVar ? 2 : null);

        ListingSnapshot? sonuc = await CozAsync(fiyat);

        Assert.NotNull(sonuc);
        Assert.Null(sonuc.Price);
    }

    private static async Task<ListingSnapshot?> CozAsync(ShowcasePriceDto fiyat)
    {
        ListingSnapshotSource kaynak = new(new FakeListingSnapshotQueries(fiyat));

        return await kaynak.FindAsync("toyota-corolla-2019-abc123", CancellationToken.None);
    }

    private sealed class FakeListingSnapshotQueries(ShowcasePriceDto fiyat) : IListingSnapshotQueries
    {
        public Task<ListingSnapshotDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult<ListingSnapshotDto?>(new ListingSnapshotDto(
                ListingId,
                IsReserved: false,
                Guid.Parse("0192c000-0000-7000-8000-0000000000c1"),
                fiyat,
                Guid.Parse("0192c000-0000-7000-8000-000000000001")));
    }
}
