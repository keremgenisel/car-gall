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

    private ShowcaseListingRequest(IReadOnlyList<BodyTypeSlug> bodyTypes, int page)
    {
        BodyTypes = bodyTypes;
        Page = page;
    }

    internal IReadOnlyList<BodyTypeSlug> BodyTypes { get; }

    internal int Page { get; }

    internal static ShowcaseListingRequest Normalize(IReadOnlyList<string>? rawBodyTypes, int rawPage)
    {
        List<BodyTypeSlug> bodyTypes = [];

        foreach (string raw in rawBodyTypes ?? [])
        {
            // Eşleştirme tam ve harfe duyarlıdır: "SUV" tanınmayan değerdir ve yok sayılır
            // (C13, AC-17). ILIKE/lower() Türkçe collation altında yanlış sonuç verir (C11).
            if (BodyTypeSlug.TryParse(raw, out BodyTypeSlug slug) && !bodyTypes.Contains(slug))
            {
                bodyTypes.Add(slug);
            }
        }

        return new ShowcaseListingRequest(bodyTypes, rawPage < 1 ? 1 : rawPage);
    }

    /// <summary>
    /// Son sayfanın ötesi istendiğinde ilk sayfa gösterilir (AC-14, AC-15). Ziyaretçiye
    /// hata metni gösterilmez; sunucu 200 döner.
    /// </summary>
    internal int ResolvePage(int totalCount)
    {
        int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
        return Page > totalPages ? 1 : Page;
    }
}
