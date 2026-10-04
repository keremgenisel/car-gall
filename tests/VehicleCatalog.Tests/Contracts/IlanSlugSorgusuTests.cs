using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Contracts;

/// <summary>
/// 0009 D5 — bildirimdeki detay bağlantısının slug'ı. Kaldırılmış ilan için `null` döner
/// ve bu bir hata değildir: bağlantı kurulamaz, bildirim yine gider (AC-32).
///
/// <para><b>Bunlar SÖZLEŞME YÜZEYİ testleridir, kriter testi DEĞİL.</b> Kriterlerin
/// tarif ettiği davranış — e-posta gönderimi, kuyruk, idempotentlik, gönderim geçmişi —
/// `Notifications` modülünde yaşıyor ve orada eşlenir. Buradaki testler yalnız yüzeyin
/// doğru veriyi verdiğini gösterir; bu yüzden `AC-` öneki taşımazlar (`CodeRuleTests`
/// deseni). Kanıtlamadığı kriteri iddia eden bir ad, kriter → test tablosunu yalancı yapar.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class IlanSlugSorgusuTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Doner_Slug_YayindakiIlanIcin()
    {
        await database.ResetAsync();

        await using VehicleCatalogDbContext context = database.CreateContext();

        ShowcaseListing ilan = new ShowcaseListingBuilder().WithSlug("toyota-corolla-2019-abc123").Build();
        context.ShowcaseListings.Add(ilan);
        await context.SaveChangesAsync();

        string? slug = await new ListingSlugQueries(context)
            .FindSlugAsync(ilan.ListingId, CancellationToken.None);

        Assert.Equal("toyota-corolla-2019-abc123", slug);
    }

    [Fact]
    public async Task Doner_Null_KaldirilmisIlanIcin()
    {
        await database.ResetAsync();

        await using VehicleCatalogDbContext context = database.CreateContext();

        Assert.Null(await new ListingSlugQueries(context)
            .FindSlugAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
