using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `frontend-003` **AC-55** (AP-08 revizyonu, Takım Yöneticisi kararı 2026-11-26) — karşılaştırma
/// ekranı her sütunu mevcut detay okumasıyla okur (D14) ve yalnız yanıttaki nitelikleri basar
/// (AC-54). Alan ayarı panelde değiştirilemez; `0002` AC-12/13'teki gibi veritabanında kapatılır.
/// Zincir GERÇEK: alan ayarı tablosu, yansıtma yazıcısı (açık ve public alanları ayardan okur) ve
/// detay sorgusu, gerçek PostgreSQL'de.
///
/// <para>"Açıkken VAR" iddiası aynı testte: yalnız "yok" iddiası boş bir yanıtta da geçerdi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class KarsilastirmaAlanAyariTests(ShowcaseDatabase database)
{
    private const string Alan = "frontend_003_not";

    private static readonly DateTimeOffset Simdi = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "frontend-003/AC-55 (a): Alan açık ve public iken karşılaştırmanın okuduğu detay yanıtında nitelik VAR; alan veritabanında kapatılıp ilan yeniden yansıtılınca YOK; açılınca geri gelir")]
    public async Task Izler_AlanAyarini_DetayOkumasinda()
    {
        await database.ResetAsync();
        await AyarAsync(FieldFormState.Optional, ekle: true);

        Vehicle arac = new VehicleBuilder().Build();
        arac.SetOptionalFieldValue(
            Alan,
            OptionalFieldValue.OfLocalizedText(LocalizedText.From([new(Locale.Create("tr").Value, "Takas kabul edilir")])));
        Listing ilan = Listing.CreateDraft(arac.Id, showcaseSortWeight: 0);
        ListingSlug adres = ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value;
        ilan.Publish(adres, new FixedClock(Simdi));

        await YansitAsync(arac, ilan);
        Assert.Contains(Alan, await AnahtarlarAsync(adres.Value));

        await AyarAsync(FieldFormState.Closed);
        await YansitAsync(arac, ilan);
        Assert.DoesNotContain(Alan, await AnahtarlarAsync(adres.Value));
        Assert.Contains("body_type", await AnahtarlarAsync(adres.Value));

        await AyarAsync(FieldFormState.Optional);
        await YansitAsync(arac, ilan);
        Assert.Contains(Alan, await AnahtarlarAsync(adres.Value));
    }

    private async Task AyarAsync(FieldFormState durum, bool ekle = false)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        if (ekle)
        {
            context.VehicleFieldSettings.Add(new VehicleFieldSetting
            {
                FieldKey = Alan,
                FormState = durum,
                PublicVisibility = FieldPublicVisibility.Visible,
                IsCore = false,
            });
        }
        else
        {
            VehicleFieldSetting ayar = await context.VehicleFieldSettings.SingleAsync(item => item.FieldKey == Alan);
            ayar.FormState = durum;
        }

        await context.SaveChangesAsync();
    }

    private async Task YansitAsync(Vehicle arac, Listing ilan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        await new ShowcaseProjectionWriter(context, new SayanSubeSorgusu(), new FakeMediaAssetQueries())
            .ProjectAsync(arac, ilan, read: null, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<string>> AnahtarlarAsync(string slug)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        ShowcaseListingDetailDto? detay = await new ShowcaseListingDetailQueries(
                context,
                new FakeModelNameResolver(),
                new FakeReferenceLabelResolver(),
                ShowcaseQueryHarness.Locales)
            .FindAsync(slug, "tr", CancellationToken.None);

        Assert.NotNull(detay);
        return [.. detay.Attributes.Select(item => item.Key)];
    }
}
