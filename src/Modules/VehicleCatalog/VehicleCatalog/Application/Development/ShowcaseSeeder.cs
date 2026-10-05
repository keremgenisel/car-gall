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
///
/// <para><b>0010a/D12 — artık YAZMA modelini de yazıyor.</b> Bu spec'e kadar agregatlar
/// bellekte kurulup atılıyordu ve saklanan tek şey okuma modeli satırlarıydı. Yazma modeli
/// gelince o hâl bir yalana dönüşürdü: arkasında araç kaydı olmayan iki yüz vitrin satırı
/// ve bomboş bir panel listesi.</para>
///
/// <para><b>İlanlar YAYINLANMAYA devam ediyor</b> ve bu, panelin `0010a`'da yapamadığı bir
/// şey (X2). Seeder bir geliştirme aracıdır, uç değil; taslak üretseydi vitrin boşalır ve
/// `0001`/`0002`'nin bütün e2e'si düşerdi.</para>
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

        // Yalnız o an AÇIK ve PUBLIC olan opsiyonel alanlar yansıtılır (C4).
        // Tramer seed'de Gizli olduğu için buraya GİRMEZ — AC-13 bunu sınar.
        HashSet<string> publicOptionalFieldKeys =
        [
            .. (await context.VehicleFieldSettings.AsNoTracking().ToListAsync(cancellationToken))
                .Where(setting => setting is { IsCore: false, IsOpen: true, IsPublic: true })
                .Select(setting => setting.FieldKey)
        ];

        List<ShowcaseListing> rows = [];

        // 0010a/D12 — yazma modeli de saklanıyor; vitrin satırları artık arkalarında
        // gerçek birer araç kaydı olan ilanlardan türüyor.
        List<Vehicle> vehicles = [];
        List<Listing> listings = [];

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

            // 0010a/D12 — seeder artık şase de üretiyor. Tekillik `vehicles` üzerindeki
            // kısmi tekil indeksle korunuyor (D11, gözden geçirildi) ve seed aynı şaseyi
            // iki kez üretirse ilk `SaveChanges` patlardı; sıra numarası bunu imkânsız
            // kılıyor.
            Vin vin = Vin.Create($"SEED{index:D13}").Value;

            Vehicle vehicle = Vehicle.Create(
                branchId,
                vehicleModelId,
                specification,
                color,
                2016 + (index % 9),
                45_000 + (index * 1_137),
                price,
                vin,
                clock.UtcNow).Value;

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

            vehicles.Add(vehicle);
            listings.Add(listing);

            if (index % 7 == 6)
            {
                listing.Reserve();
            }

            ListingSlug slug = ListingSlug.Create("Toyota Corolla", vehicle.ModelYear, listing.Id).Value;

            ShowcaseListing? row = ShowcaseListingProjector.Project(
                vehicle,
                listing,
                slug,
                branchNames,
                CoverUrl(index),
                publicOptionalFieldKeys);
            if (row is not null)
            {
                rows.Add(row);
            }
        }

        context.Vehicles.AddRange(vehicles);
        context.Listings.AddRange(listings);
        context.ShowcaseListings.AddRange(rows);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string CoverUrl(int index) =>
        $"/seed-media/cover-{index % 6}.svg";
}
