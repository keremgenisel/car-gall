using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Development;

/// <summary>
/// YALNIZCA geliştirme ve kanıt üretimi içindir; üretimde çağrılmaz. Satırları elle
/// yazmaz — agregatları kurup ShowcaseListingProjector'dan geçirir, yani üretimdeki
/// görünürlük kararının AYNI yolunu kullanır. Elle satır yazan bir seed, kanıt
/// görüntülerini gerçek davranıştan kopartırdı.
/// </summary>
internal sealed class ShowcaseSeeder(VehicleCatalogDbContext context, IClock clock)
{
    private static readonly string[] BodyTypes = ["sedan", "suv", "hatchback", "station-wagon", "coupe", "pickup"];
    private static readonly string[] FuelTypes = ["petrol", "diesel", "hybrid", "electric"];
    private static readonly string[] Transmissions = ["manual", "automatic"];
    private static readonly string[] Drivetrains = ["front-wheel-drive", "rear-wheel-drive", "all-wheel-drive"];
    private static readonly string[] TrimLevels = ["base", "comfort", "premium"];
    private static readonly (string Tr, string Ar, string En)[] Colors =
    [
        ("Beyaz", "أبيض", "White"),
        ("Siyah", "أسود", "Black"),
        ("Gri", "رمادي", "Grey"),
    ];

    internal async Task SeedAsync(int listingCount, Guid vehicleModelId, CancellationToken cancellationToken)
    {
        if (await context.ShowcaseListings.AnyAsync(cancellationToken))
        {
            return;
        }

        Guid branchId = Guid.Parse("0192c000-0000-7000-8000-000000000001");
        Dictionary<string, string> branchNames = new()
        {
            ["tr"] = "Merkez Şube",
            ["ar"] = "الفرع الرئيسي",
            ["en"] = "Main Branch",
        };

        List<ShowcaseListing> rows = [];

        for (int index = 0; index < listingCount; index++)
        {
            string bodyType = BodyTypes[index % BodyTypes.Length];

            // Her altıncı araç "fiyat sorunuz" (P4), her yedincisi rezerve (R6).
            ListingPrice price = index % 6 == 5
                ? ListingPrice.OnRequest()
                : ListingPrice.Of(Money.Create(750_000m + (index * 12_500m), "TRY", 2).Value);

            VehicleSpecification specification = VehicleSpecification.Create(
                BodyTypeSlug.Create(bodyType).Value,
                FuelTypes[index % FuelTypes.Length],
                Transmissions[index % Transmissions.Length],
                Drivetrains[index % Drivetrains.Length],
                TrimLevels[index % TrimLevels.Length]).Value;

            (string tr, string ar, string en) = Colors[index % Colors.Length];
            LocalizedText color = LocalizedText.From(
            [
                new KeyValuePair<Locale, string>(Locale.Create("tr").Value, tr),
                new KeyValuePair<Locale, string>(Locale.Create("ar").Value, ar),
                new KeyValuePair<Locale, string>(Locale.Create("en").Value, en),
            ]);

            Vehicle vehicle = Vehicle.Create(
                branchId,
                vehicleModelId,
                specification,
                color,
                2016 + (index % 9),
                45_000 + (index * 1_137),
                price).Value;

            vehicle.AttachMediaAsset(Guid.CreateVersion7());

            // E10 — opsiyonel alan DEĞERLERİ yazma yolu gelene kadar buradan gelir.
            // Tramer alanı seed'de Gizli'dir; AC-13 tam olarak onu sınar.
            vehicle.SetOptionalFieldValue(
                VehicleFieldKeys.DamageRecordAmount,
                LocalizedText.From(
                [
                    new KeyValuePair<Locale, string>(Locale.Create("tr").Value, $"{index * 1_250} TL"),
                    new KeyValuePair<Locale, string>(Locale.Create("ar").Value, $"{index * 1_250} ليرة"),
                    new KeyValuePair<Locale, string>(Locale.Create("en").Value, $"{index * 1_250} TRY"),
                ]));

            Listing listing = Listing.CreateDraft(vehicle.Id, showcaseSortWeight: listingCount - index);
            listing.Publish(clock);

            if (index % 7 == 6)
            {
                listing.Reserve();
            }

            ShowcaseListing? row = ShowcaseListingProjector.Project(vehicle, listing, branchNames, CoverUrl(index));
            if (row is not null)
            {
                rows.Add(row);
            }
        }

        context.ShowcaseListings.AddRange(rows);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string CoverUrl(int index) =>
        $"/seed-media/cover-{index % 6}.svg";
}
