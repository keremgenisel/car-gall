using System.Globalization;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Ziyaretçiden gelen ham girdiyi normalize eder. C14: geçersiz daraltma veya sayfa
/// girdisi HATA DEĞİLDİR — sunucu her zaman normalize edilmiş listeyi döner. Bu yüzden
/// normalizasyon uç noktada değil burada yaşar ve kendi testleri vardır.
/// </summary>
internal sealed record ShowcaseListingRequest
{
    /// <summary>R6: sayfa başına 24 ilan.</summary>
    internal const int PageSize = 24;

    private ShowcaseListingRequest(IReadOnlyList<BodyTypeSlug> bodyTypes, int page, int pageSize)
    {
        BodyTypes = bodyTypes;
        Page = page;
        ResolvedPageSize = pageSize;
    }

    internal IReadOnlyList<BodyTypeSlug> BodyTypes { get; }

    internal int Page { get; }

    internal int ResolvedPageSize { get; }

    /// <summary>`0015` R1 — tanınan yakıt slug'ları; boşsa eksen uygulanmaz.</summary>
    internal IReadOnlyList<string> FuelTypes { get; private init; } = [];

    internal IReadOnlyList<string> TransmissionTypes { get; private init; } = [];

    /// <summary>`null` = eksen yok (verilmedi ya da geçersizdi — R15, ikisi aynı sonucu verir).</summary>
    internal ShowcaseIntRange? Year { get; private init; }

    internal ShowcaseIntRange? Mileage { get; private init; }

    /// <summary>Para biriminin ANA biriminde (R15). Verildiyse "fiyat sorunuz" ilan elenir (R4).</summary>
    internal ShowcaseIntRange? Price { get; private init; }

    internal ShowcaseSort Sort { get; private init; }

    internal static ShowcaseListingRequest Normalize(
        IReadOnlyList<string>? rawBodyTypes,
        int rawPage,
        IReadOnlySet<string> canonicalSlugs,
        int? rawPageSize = null) =>
        Normalize(
            new ShowcaseListingFilter(rawBodyTypes ?? [], rawPage, string.Empty, rawPageSize),
            canonicalSlugs,
            EmptySet,
            EmptySet);

    /// <summary>
    /// `0015` — bütün eksenler. Geçersiz girdi HATA DEĞİLDİR (`0001` C14): tanınmayan slug,
    /// tanınmayan sıralama ve geçersiz aralık sessizce yok sayılır; diğer eksenler uygulanır.
    /// </summary>
    internal static ShowcaseListingRequest Normalize(
        ShowcaseListingFilter filter,
        IReadOnlySet<string> bodyTypeSlugs,
        IReadOnlySet<string> fuelSlugs,
        IReadOnlySet<string> transmissionSlugs)
    {
        List<BodyTypeSlug> bodyTypes = [];

        foreach (string raw in filter.BodyTypes)
        {
            // Tanınmak için İKİ koşul: biçim kanonik slug biçiminde olacak VE değer
            // kanonik kümede bulunacak. Yalnız biçime bakmak "banana" gibi biçimi geçerli
            // ama var olmayan bir değeri daraltmaya sokar ve boş liste üretirdi (AC-17).
            // Eşleştirme tam ve harfe duyarlıdır: "SUV" kümede yoktur (C13); ILIKE/lower()
            // Türkçe collation altında yanlış sonuç verir (C11, Yasak Listesi 27).
            if (BodyTypeSlug.TryParse(raw, out BodyTypeSlug slug)
                && bodyTypeSlugs.Contains(slug.Value)
                && !bodyTypes.Contains(slug))
            {
                bodyTypes.Add(slug);
            }
        }

        int? rawPageSize = filter.PageSize;
        int pageSize = rawPageSize is null or < 1 ? PageSize : Math.Min(rawPageSize.Value, PageSize);

        return new ShowcaseListingRequest(bodyTypes, filter.Page < 1 ? 1 : filter.Page, pageSize)
        {
            FuelTypes = KnownSlugs(filter.FuelTypes, fuelSlugs),
            TransmissionTypes = KnownSlugs(filter.TransmissionTypes, transmissionSlugs),
            Year = ShowcaseIntRange.Parse(filter.Year),
            Mileage = ShowcaseIntRange.Parse(filter.Mileage),
            Price = ShowcaseIntRange.Parse(filter.Price),
            Sort = ShowcaseSortParser.Parse(filter.Sort),
        };
    }

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// `0015` C12 — kasa tipiyle aynı kural: tam ve harfe duyarlı küme üyeliği (C1);
    /// `Diesel` `diesel` değildir (AC-17). Tekrar ve boş değer düşer.
    /// </summary>
    private static List<string> KnownSlugs(IReadOnlyList<string> raw, IReadOnlySet<string> canonical)
    {
        List<string> known = [];

        foreach (string value in raw)
        {
            if (canonical.Contains(value) && !known.Contains(value, StringComparer.Ordinal))
            {
                known.Add(value);
            }
        }

        return known;
    }

    /// <summary>
    /// Son sayfanın ötesi istendiğinde ilk sayfa gösterilir (AC-14, AC-15). Ziyaretçiye
    /// hata metni gösterilmez; sunucu 200 döner.
    /// </summary>
    internal int ResolvePage(int totalCount)
    {
        int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)ResolvedPageSize);
        return Page > totalPages ? 1 : Page;
    }
}

/// <summary>
/// `0015` R15 — sınırlar DAHİLDİR (`≥` alt, `≤` üst); tek sınır geçerlidir.
/// </summary>
internal sealed record ShowcaseIntRange(int? Min, int? Max)
{
    /// <summary>
    /// Bir sınır verilmiş ama işaretsiz bir tam sayı DEĞİLSE (`abc`, `-5`, `500000.5`) ya da
    /// alt sınır üstten büyükse EKSENİN TAMAMI yok sayılır (`null`). Sınırları yer
    /// değiştirmek niyet tahmini olurdu (D10). Boş değer verilmemiş sayılır.
    /// </summary>
    internal static ShowcaseIntRange? Parse(ShowcaseRange raw)
    {
        if (!TryBound(raw.Min, out int? min) || !TryBound(raw.Max, out int? max))
        {
            return null;
        }

        if (min is null && max is null)
        {
            return null;
        }

        return min > max ? null : new ShowcaseIntRange(min, max);
    }

    private static bool TryBound(string? raw, out int? bound)
    {
        bound = null;

        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        // NumberStyles.None: işaret, boşluk, ondalık ayırıcı ve binlik ayırıcı KABUL EDİLMEZ.
        // "-5" ve "500000.5" burada düşer; `,`/`.` karmaşası hiç doğmaz.
        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
        {
            bound = parsed;
            return true;
        }

        return false;
    }
}
