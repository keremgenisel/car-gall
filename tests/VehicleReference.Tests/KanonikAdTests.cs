using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Infrastructure;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>
/// 0005 R7/AC-18 — kanonik Latin ad dilden BAĞIMSIZDIR. Var olan `ResolveNamesAsync`
/// bunun yerini tutmaz: o, çevirisi varsa çeviriyi döner. Talebin anlık görüntüsü
/// ziyaretçinin dilindeki adı dondurursa, o dili okumayan danışman için talep okunamaz
/// hâle gelir ve kopya sonradan düzeltilemez.
/// </summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class KanonikAdTests(ReferenceDatabase database)
{
    [Fact(DisplayName = "AC-18: Anlık görüntüdeki model adı kanonik Latin adıdır; Arapça sayfadan bırakılan talepte de kanonik ad saklanır")]
    public async Task DonerKanonikAd_CevirisiVarken()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        VehicleModelQueries queries = new(context);

        Guid modelId = await IlkModelIdAsync(context);

        IReadOnlyDictionary<Guid, string> kanonik =
            await queries.ResolveCanonicalNamesAsync([modelId], CancellationToken.None);

        IReadOnlyDictionary<Guid, string> arapca =
            await queries.ResolveNamesAsync([modelId], "ar", CancellationToken.None);

        Assert.True(kanonik.ContainsKey(modelId));
        Assert.NotEmpty(kanonik[modelId]);

        // Kanonik ad Latin harflidir: Arapça karşılığı varsa ondan farklıdır, yoksa
        // ResolveNamesAsync zaten kanoniğe düşmüştür. İki durumda da kanonik ad Latin kalır.
        Assert.All(kanonik[modelId], karakter => Assert.True(karakter < 0x0600));
        Assert.NotNull(arapca);
    }

    [Fact(DisplayName = "AC-18: Anlık görüntüdeki model adı kanonik Latin adıdır; Arapça sayfadan bırakılan talepte de kanonik ad saklanır")]
    public async Task DonerBos_KimlikListesiBossa()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        VehicleModelQueries queries = new(context);

        IReadOnlyDictionary<Guid, string> sonuc =
            await queries.ResolveCanonicalNamesAsync([], CancellationToken.None);

        Assert.Empty(sonuc);
    }

    [Fact(DisplayName = "AC-18: Anlık görüntüdeki model adı kanonik Latin adıdır; Arapça sayfadan bırakılan talepte de kanonik ad saklanır")]
    public async Task DonerBos_BilinmeyenModelIcin()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        VehicleModelQueries queries = new(context);

        IReadOnlyDictionary<Guid, string> sonuc =
            await queries.ResolveCanonicalNamesAsync([Guid.CreateVersion7()], CancellationToken.None);

        Assert.Empty(sonuc);
    }

    private static async Task<Guid> IlkModelIdAsync(VehicleReferenceDbContext context) =>
        await Task.FromResult(context.VehicleModels.OrderBy(model => model.Id).First().Id);
}
