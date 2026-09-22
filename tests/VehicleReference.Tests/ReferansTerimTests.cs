using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Infrastructure;

namespace CarGallery.Modules.VehicleReference.Tests;

[Collection(ReferenceDatabaseCollection.Name)]
public sealed class ReferansTerimTests(ReferenceDatabase database)
{
    [Theory(DisplayName = "0002/AC-21: Detay sayfasındaki tüm etiketler TR, AR ve EN locale'lerinde kendi dilinde görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez")]
    [InlineData("tr", "FuelType", "diesel", "Dizel")]
    [InlineData("ar", "FuelType", "diesel", "ديزل")]
    [InlineData("en", "FuelType", "diesel", "Diesel")]
    [InlineData("tr", "TransmissionType", "automatic", "Otomatik")]
    [InlineData("ar", "DrivetrainType", "all-wheel-drive", "دفع رباعي")]
    [InlineData("tr", "TrimLevel", "premium", "Premium")]
    public async Task KendiDilindeGorunur_ReferansTerimEtiketleri(string locale, string kind, string slug, string beklenen)
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        ReferenceTermQueries queries = new(context);

        IReadOnlyDictionary<(string, string), string> etiketler =
            await queries.ResolveLabelsAsync([(kind, slug)], locale, CancellationToken.None);

        Assert.Equal(beklenen, etiketler[(kind, slug)]);
    }

    [Fact]
    public async Task KanonikAdaDuser_CevirisiOlmayanLocaleDe()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        ReferenceTermQueries queries = new(context);

        IReadOnlyDictionary<(string, string), string> etiketler =
            await queries.ResolveLabelsAsync([("FuelType", "diesel")], "de", CancellationToken.None);

        Assert.Equal("Diesel", etiketler[("FuelType", "diesel")]);
    }

    [Fact]
    public async Task AyniSlugFarkliTurdeKarismaz()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        ReferenceTermQueries queries = new(context);

        // "base" yalnız donanım paketidir; yakıt türü olarak sorulursa karşılığı yoktur.
        IReadOnlyDictionary<(string, string), string> etiketler =
            await queries.ResolveLabelsAsync([("FuelType", "base")], "tr", CancellationToken.None);

        Assert.Empty(etiketler);
    }
}
