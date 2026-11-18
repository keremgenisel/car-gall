namespace CarGallery.Modules.Content.Contracts.Pages;

/// <summary>Sayfanın yayında olduğu BAŞKA bir dildeki adresi (`0017` R15, D14).</summary>
public sealed record ContentPageAlternateDto(string Locale, string Slug);

/// <summary>SSS sayfasında o dilde yayındaki bir girdi (`0017` R19).</summary>
public sealed record ContentFaqItemDto(string Question, string Answer);

/// <summary>
/// `0017` C4 — ziyaretçiye dönük sayfa: YALNIZ yayındaki dil. Gövde ham saklanan alt
/// kümedir (R16); HTML değildir ve ziyaretçi tarafı onu basarken arındırır (R17, D26).
/// </summary>
public sealed record ContentPagePublicDto(
    string? SystemKey,
    string Title,
    string Body,
    IReadOnlyList<ContentPageAlternateDto> Alternates,
    IReadOnlyList<ContentFaqItemDto> FaqEntries);

/// <summary>Altlığın "Kurumsal" kolonundaki bağlantı: kimlik, o dildeki başlık ve slug (R21, C13).</summary>
public sealed record ContentNavigationLinkDto(string SystemKey, string Title, string Slug);

/// <summary>`0017` — `Content`'in public okuma yüzeyi; Host ve uçlar yalnız bunu görür (AC-129).</summary>
public interface IContentPublicQueries
{
    /// <summary>
    /// Verilen dilde YAYINDAKİ ve o slug'ı taşıyan sayfa; yoksa <c>null</c> (R11, R12).
    /// Girdisiz SSS yayında sayılmaz (R20).
    /// </summary>
    Task<ContentPagePublicDto?> FindBySlugAsync(string locale, string slug, CancellationToken cancellationToken);

    /// <summary>Altlığın sistem sayfası bağlantıları, sabit sırada, yalnız o dilde yayında olanlar (R21).</summary>
    Task<IReadOnlyList<ContentNavigationLinkDto>> NavigationAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// `0017` D13/D27 — verilen dilde ESKİ bir slug'sa sayfanın o dildeki GÜNCEL slug'ı;
    /// değilse <c>null</c>. Zincir yoktur: her eski slug doğrudan güncele çözülür (D33).
    /// </summary>
    Task<string?> FindCurrentSlugAsync(string locale, string oldSlug, CancellationToken cancellationToken);
}

/// <summary>
/// `0017` C11/D11 — tek parçalı vitrin adreslerinde içerik sayfasına verilemeyen adlar.
/// Kodda TEK yerde yaşar: `Content` slug'ı reddederken, Host eski slug okumasını atlarken
/// aynı listeyi okur. Vitrine eklenen her tek parçalı bölüm buraya eklenir.
/// </summary>
public static class ContentReservedSlugs
{
    /// <summary>Ayrılmış adlar.</summary>
    public static IReadOnlyList<string> All { get; } = ["cars", "admin", "api", "assets"];

    /// <summary>Ad ayrılmış mı (büyük-küçük harfe duyarlı; slug zaten küçük harftir).</summary>
    public static bool Contains(string slug) => All.Contains(slug, StringComparer.Ordinal);
}
