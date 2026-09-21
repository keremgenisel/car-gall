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

    internal static ShowcaseListingRequest Normalize(
        IReadOnlyList<string>? rawBodyTypes,
        int rawPage,
        IReadOnlySet<string> canonicalSlugs,
        int? rawPageSize = null)
    {
        List<BodyTypeSlug> bodyTypes = [];

        foreach (string raw in rawBodyTypes ?? [])
        {
            // Tanınmak için İKİ koşul: biçim kanonik slug biçiminde olacak VE değer
            // kanonik kümede bulunacak. Yalnız biçime bakmak "banana" gibi biçimi geçerli
            // ama var olmayan bir değeri daraltmaya sokar ve boş liste üretirdi (AC-17).
            // Eşleştirme tam ve harfe duyarlıdır: "SUV" kümede yoktur (C13); ILIKE/lower()
            // Türkçe collation altında yanlış sonuç verir (C11, Yasak Listesi 27).
            if (BodyTypeSlug.TryParse(raw, out BodyTypeSlug slug)
                && canonicalSlugs.Contains(slug.Value)
                && !bodyTypes.Contains(slug))
            {
                bodyTypes.Add(slug);
            }
        }

        int pageSize = rawPageSize is null or < 1 ? PageSize : Math.Min(rawPageSize.Value, PageSize);

        return new ShowcaseListingRequest(bodyTypes, rawPage < 1 ? 1 : rawPage, pageSize);
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
