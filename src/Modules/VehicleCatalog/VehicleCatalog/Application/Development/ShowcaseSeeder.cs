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
internal sealed class ShowcaseSeeder(VehicleCatalogDbContext context, IClock clock, CatalogCurrency currency)
{
    /// <summary>
    /// Seed metni. Her beşinci araçta Arapça eksik — gösterge gerçek veriyle görülsün.
    /// </summary>
    private static IEnumerable<KeyValuePair<Locale, string>> SeedText(string model, int index, bool baslik)
    {
        string tr = baslik ? $"{model} {2016 + (index % 9)} — temiz kayıt" : $"{model}, bakımları yapılmış, {index} numaralı seed kaydı.";
        string en = baslik ? $"{model} {2016 + (index % 9)} — clean record" : $"{model}, fully serviced, seed record {index}.";
        string ar = baslik ? $"{model} {2016 + (index % 9)} — سجل نظيف" : $"{model}، صيانة كاملة، سجل {index}.";

        yield return new KeyValuePair<Locale, string>(Locale.Create("tr").Value, tr);
        yield return new KeyValuePair<Locale, string>(Locale.Create("en").Value, en);

        if (index % 5 != 4)
        {
            yield return new KeyValuePair<Locale, string>(Locale.Create("ar").Value, ar);
        }
    }

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
            // 0010b/D17 — tramer bir TUTAR; artık `Money` olarak yazılıyor, metin olarak
            // değil. Alan seed'de Gizli'dir ve `0001` AC-13 tam olarak onu sınar.
            vehicle.SetOptionalFieldValue(
                VehicleFieldKeys.DamageRecordAmount,
                OptionalFieldValue.OfAmount(
                    Money.Create(index * 1_250m, currency.Code, currency.MinorUnitExponent).Value));

            // 0012c D36/AC-31 — öne çıkarma anahtarı İKİ DEĞERLİ ve seed artık o iki değeri
            // yazıyor. Önceki hâli `listingCount - index` ile 200 ilana 200 ayrı ağırlık
            // veriyordu; kararın kendisini VERİ tarafında deliyordu ve "öne çıkarılmış
            // ilan" diye bir şey geliştirme ortamında görünmüyordu — her ilan farklı bir
            // ağırlıktaydı, yani hiçbiri öne çıkarılmış değildi. Her yedinci ilan öne
            // çıkarılıyor ki panelin rozeti ve vitrin sırası seed'de görülebilsin.
            Listing listing = Listing.CreateDraft(
                vehicle.Id,
                showcaseSortWeight: index % 7 == 0 ? Listing.FeaturedWeight : Listing.NormalWeight);

            // 0010b — seed ilan metni de yazıyor. Her beşinci araçta Arapça metin BİLEREK
            // eksik: "eksik dil" göstergesinin (R12) geliştirme verisinde görülebilir bir
            // karşılığı olmasa, kanıt üretimi sahte veriye muhtaç kalırdı.
            listing.SetText(
                LocalizedText.From(SeedText("Toyota Corolla", index, baslik: true)),
                LocalizedText.From(SeedText("Toyota Corolla", index, baslik: false)));

            // 0012b D13 — adres artık YAZMA modelinde ve ilk yayında bir kez yazılıyor;
            // seed de o yoldan geçiyor. Projeksiyon onu kopyalıyor.
            ListingSlug slug = ListingSlug.Create("Toyota Corolla", vehicle.ModelYear, listing.Id).Value;

            listing.Publish(slug, clock);

            vehicles.Add(vehicle);
            listings.Add(listing);

            if (index % 7 == 6)
            {
                listing.Reserve();
            }

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
