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
        List<ShowcaseListingMedia> media = [];

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

            // `0016b` — galerinin yuvaları. Kapak (liste kartı) bugünkü gibi `cover-n.svg`;
            // detay galerisi türevleri `frame-…` adreslerinden alır (bkz. `SeedGallery`).
            List<ShowcaseListingMedia> galeri = SeedGallery(index);
            foreach (ShowcaseListingMedia yuva in galeri)
            {
                vehicle.AttachMediaAsset(yuva.MediaAssetId);
            }

            // E10 — opsiyonel alan DEĞERLERİ yazma yolu gelene kadar buradan gelir.
            // 0010b/D17 — tramer bir TUTAR; artık `Money` olarak yazılıyor, metin olarak
            // değil. Alan seed'de Gizli'dir ve `0001` AC-13 tam olarak onu sınar.
            vehicle.SetOptionalFieldValue(
                VehicleFieldKeys.DamageRecordAmount,
                OptionalFieldValue.OfAmount(
                    Money.Create(index * 1_250m, currency.Code, currency.MinorUnitExponent).Value));

            // `0016a` AC-34 — parça haritasının üç hâli ilk üç ilanda: dört durumun HEPSİNİ
            // içeren tam harita, 13 parçası orijinal harita ve haritasız ilan. `0016b`'nin
            // şema, "tamamı orijinal" ve değersiz kriterlerinin kanıtı bu üçüyle alınır.
            if (SeedPaintChangeMap(index) is PaintChangeMap harita)
            {
                vehicle.SetOptionalFieldValue(VehicleFieldKeys.PaintChangeMap, OptionalFieldValue.OfPartMap(harita));
            }

            // 0012c D36/AC-31 — öne çıkarma anahtarı İKİ DEĞERLİ ve seed artık o iki değeri
            // yazıyor. Önceki hâli `listingCount - index` ile 200 ilana 200 ayrı ağırlık
            // veriyordu; kararın kendisini VERİ tarafında deliyordu ve "öne çıkarılmış
            // ilan" diye bir şey geliştirme ortamında görünmüyordu — her ilan farklı bir
            // ağırlıktaydı, yani hiçbiri öne çıkarılmış değildi.
            //
            // Öne çıkarılan ilan sayısı vitrinin BİR SAYFASINDAN az: ölçüldü — her yedinci
            // ilanı öne çıkarmak 200 ilanda 29 eder ve vitrinin ilk sayfası (24 kart)
            // tamamen öne çıkarılmışlardan oluşurdu. Rezerve ilanlar (her yedincinin
            // altıncısı) o sayfadan düşer ve `0001`/AC-2'nin rozet kanıtı görünmez olurdu;
            // e2e yakaladı. Yetmişte bir, 200 ilanda üç öne çıkarılmış ilan demek.
            Listing listing = Listing.CreateDraft(
                vehicle.Id,
                showcaseSortWeight: index % 70 == 0 ? Listing.FeaturedWeight : Listing.NormalWeight);

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
                media.AddRange(galeri.Select(yuva => new ShowcaseListingMedia
                {
                    ListingId = row.ListingId,
                    Position = yuva.Position,
                    MediaAssetId = yuva.MediaAssetId,
                    Status = yuva.Status,
                    Variants = yuva.Variants,
                }));
            }
        }

        context.Vehicles.AddRange(vehicles);
        context.Listings.AddRange(listings);
        context.ShowcaseListings.AddRange(rows);
        context.ShowcaseListingMedia.AddRange(media);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// `0016b` — galerinin geliştirme ve kanıt hâlleri, ilk beş ilanda:
    /// <list type="bullet">
    /// <item>0 — 30 hazır görsel; yatay 4:3, dikey 3:4 ve 16:9 sırayla (AC-61, AC-62, AC-64).</item>
    /// <item>1 — 3 hazır görsel; ikincinin adresi 404 döner (AC-69).</item>
    /// <item>2 — başarısız, hazır, başarısız: tek görsel (AC-67, AC-68).</item>
    /// <item>3 — 18 hazır görsel; model yılı 2019 (AC-56).</item>
    /// <item>4 — işleniyor, başarısız: hazır görsel yok (AC-65, AC-66).</item>
    /// </list>
    /// Diğer ilanlar tek hazır görsel taşır. Yuvaların <c>ListingId</c>'si çağıranda yazılır.
    /// </summary>
    private static List<ShowcaseListingMedia> SeedGallery(int index)
    {
        ShowcaseListingMedia Yuva(int konum, ShowcaseMediaStatus durum, int kare, int en, int boy) => new()
        {
            ListingId = Guid.Empty,
            Position = konum,
            MediaAssetId = Guid.CreateVersion7(),
            Status = durum,
            Variants = durum == ShowcaseMediaStatus.Ready ? Turevler(kare, en, boy) : [],
        };

        ShowcaseListingMedia Hazir(int konum, int kare, int en = 4, int boy = 3) =>
            Yuva(konum, ShowcaseMediaStatus.Ready, kare, en, boy);

        return index switch
        {
            0 => [.. Enumerable.Range(0, 30).Select(konum => (konum % 3) switch
            {
                1 => Hazir(konum, konum, 3, 4),
                2 => Hazir(konum, konum, 16, 9),
                _ => Hazir(konum, konum),
            })],
            // İkinci yuvanın karesi aralık dışında: uç 404 döner (AC-69'un kanıtı).
            1 => [Hazir(0, 0), Hazir(1, SeedFrameMissing), Hazir(2, 2)],
            2 =>
            [
                Yuva(0, ShowcaseMediaStatus.Failed, 0, 4, 3),
                Hazir(1, 1),
                Yuva(2, ShowcaseMediaStatus.Failed, 0, 4, 3),
            ],
            3 => [.. Enumerable.Range(0, 18).Select(konum => Hazir(konum, konum))],
            4 =>
            [
                Yuva(0, ShowcaseMediaStatus.Processing, 0, 4, 3),
                Yuva(1, ShowcaseMediaStatus.Failed, 0, 4, 3),
            ],
            _ => [Hazir(0, index % 30)],
        };
    }

    /// <summary>`/seed-media/frame-…` ucunun TANIMADIĞI kare numarası: o adres 404 döner.</summary>
    private const int SeedFrameMissing = 99;

    /// <summary>
    /// `Media`'nın üç türevinin boyut kuralı (en uzun kenar 400 / 800 / kaynak ≤ 1600):
    /// 4:3 kaynak 1200×900 → `large` 1200×900 (1600 değil), `display` 800×600,
    /// `thumbnail` 400×300 — `0016b` AC-10/AC-60'ın sayıları.
    /// </summary>
    private static List<ShowcaseMediaVariant> Turevler(int kare, int en, int boy)
    {
        (int W, int H) Boyut(int uzun) => en >= boy ? (uzun, uzun * boy / en) : (uzun * en / boy, uzun);

        ShowcaseMediaVariant Turev(string ad, int uzun)
        {
            (int w, int h) = Boyut(uzun);
            return new ShowcaseMediaVariant(ad, $"/seed-media/frame-{kare}-{w}x{h}.svg", w, h);
        }

        return [Turev("thumbnail", 400), Turev("display", 800), Turev("large", 1200)];
    }

    private static PaintChangeMap? SeedPaintChangeMap(int index)
    {
        Dictionary<string, string> farklar = index switch
        {
            0 => new(StringComparer.Ordinal)
            {
                ["front_bumper"] = PaintChangeStates.Painted,
                ["trunk_lid"] = PaintChangeStates.Replaced,
                ["left_rear_door"] = PaintChangeStates.LocalPainted,
            },
            1 => new(StringComparer.Ordinal),
            _ => null!,
        };

        if (index > 1)
        {
            return null;
        }

        return PaintChangeMap.Create(
            [.. PaintChangeParts.Ordered.Select(part => new PaintChangeEntry(
                part,
                farklar.TryGetValue(part, out string? durum) ? durum : PaintChangeStates.Original))]).Value;
    }

    private static string CoverUrl(int index) =>
        $"/seed-media/cover-{index % 6}.svg";
}
