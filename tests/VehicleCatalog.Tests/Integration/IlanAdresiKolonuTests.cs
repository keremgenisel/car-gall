using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0002` E9 · kaçan hata (2026-11-26) — adresin uzunluk sınırı TEK kaynaktan gelir: üretim
/// (<see cref="ListingSlug.MaxLength"/>), okuma (`TryParse`) ve iki kolonun gerçek genişliği. Kolon
/// sabitten dar kalırsa en uzun adın yayını yine `varchar` aşımıyla `500` verir.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class IlanAdresiKolonuTests(ShowcaseDatabase database)
{
    [Theory(DisplayName = "0002 E9 (kaçan hata): Adres kolonlarının gerçek genişliği ListingSlug.MaxLength'tir — yazma modeli ve vitrin okuma modeli")]
    [InlineData("listings")]
    [InlineData("showcase_listings")]
    public async Task Genislik_TekSabittenGelir(string tablo)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        int genislik = await context.Database
            .SqlQuery<int>($"""
                SELECT character_maximum_length AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'vehicle_catalog' AND table_name = {tablo} AND column_name = 'slug'
                """)
            .SingleAsync();

        Assert.Equal(ListingSlug.MaxLength, genislik);
    }
}
