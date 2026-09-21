using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;

namespace CarGallery.Modules.VehicleReference.Tests;

[Collection(ReferenceDatabaseCollection.Name)]
public sealed class KasaTipiTests(ReferenceDatabase database)
{
    [Theory(DisplayName = "AC-29: Daraltma çubuğundaki kasa tipi etiketleri TR, AR ve EN locale'lerinde kendi dilinde görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez")]
    [InlineData("tr", "suv", "SUV")]
    [InlineData("ar", "suv", "دفع رباعي")]
    [InlineData("en", "suv", "SUV")]
    [InlineData("tr", "pickup", "Pikap")]
    [InlineData("ar", "pickup", "بيك أب")]
    [InlineData("en", "pickup", "Pickup")]
    public async Task KendiDilindeGorunur_KasaTipiEtiketleri(string locale, string slug, string beklenen)
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        BodyTypeQueries queries = new(context);

        IReadOnlyList<BodyTypeDto> types = await queries.GetAllAsync(locale, CancellationToken.None);

        BodyTypeDto type = types.Single(item => item.Slug == slug);
        Assert.Equal(beklenen, type.Label);
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task HamAnahtarGorunmez_HicbirLocaleDe(string locale)
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        BodyTypeQueries queries = new(context);

        IReadOnlyList<BodyTypeDto> types = await queries.GetAllAsync(locale, CancellationToken.None);

        Assert.NotEmpty(types);
        Assert.All(types, type =>
        {
            Assert.False(string.IsNullOrWhiteSpace(type.Label));
            Assert.DoesNotContain("body_type", type.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(".", type.Label, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task KanonikAdaDuser_CevirisiOlmayanLocaleDe()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        BodyTypeQueries queries = new(context);

        IReadOnlyList<BodyTypeDto> types = await queries.GetAllAsync("de", CancellationToken.None);

        Assert.Equal("SUV", types.Single(item => item.Slug == "suv").Label);
    }

    [Fact]
    public async Task SlugBicimineUyar_TumKanonikKume()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        BodyTypeQueries queries = new(context);

        IReadOnlyList<BodyTypeDto> types = await queries.GetAllAsync("tr", CancellationToken.None);

        // C13: küçük harfli ASCII slug. Büyük harfli değer tanınmayan değerdir (AC-17).
        Assert.All(types, type => Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", type.Slug));
    }

    [Fact]
    public async Task ModelAdiCozumlenir_IstenenLocaleDe()
    {
        Guid modelId = Guid.CreateVersion7();

        await using (VehicleReferenceDbContext write = database.CreateContext())
        {
            write.VehicleModels.Add(new VehicleModel
            {
                Id = modelId,
                CanonicalName = "Toyota Corolla",
                Translations =
                [
                    new VehicleModelTranslation { VehicleModelId = modelId, Locale = "ar", Name = "تويوتا كورولا" },
                ],
            });
            await write.SaveChangesAsync();
        }

        await using VehicleReferenceDbContext context = database.CreateContext();
        VehicleModelQueries queries = new(context);

        IReadOnlyDictionary<Guid, string> arabic = await queries.ResolveNamesAsync([modelId], "ar", CancellationToken.None);
        IReadOnlyDictionary<Guid, string> turkish = await queries.ResolveNamesAsync([modelId], "tr", CancellationToken.None);

        Assert.Equal("تويوتا كورولا", arabic[modelId]);
        Assert.Equal("Toyota Corolla", turkish[modelId]);
    }
}
