using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class ListingPriceTests
{
    [Fact(DisplayName = "0001/AC-21: \"Fiyat sorunuz\" işaretli ilanın kartında tutar yerine \"fiyat sorunuz\" karşılığı gösterilir; 0 veya boş tutar gösterilmez")]
    public void TutarTasimaz_FiyatSorunuzDurumunda()
    {
        ListingPrice price = ListingPrice.OnRequest();

        Assert.True(price.IsOnRequest);
        Assert.Null(price.Amount);
    }

    [Fact]
    public void AyirtEdilir_FiyatiSifirOlanAractan()
    {
        ListingPrice onRequest = ListingPrice.OnRequest();
        ListingPrice zero = ListingPrice.Of(Money.Create(0m, "TRY", 2).Value);

        Assert.NotEqual(onRequest, zero);
        Assert.False(zero.IsOnRequest);
        Assert.Equal(0m, zero.Amount!.Value.Amount);
    }
}
