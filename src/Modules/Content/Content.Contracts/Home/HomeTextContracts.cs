namespace CarGallery.Modules.Content.Contracts.Home;

/// <summary>
/// `0022` R1, R2 — ziyaretçinin dilindeki ana sayfa metinleri. <c>null</c> alan "galeri bu
/// dilde yazmadı" demektir ve uç o dilin varsayılan metnini basar (D3); varsayılan metin
/// çeviri kataloğunda yaşar, burada taşınmaz (C2). Değerler DÜZ METİNDİR (R3, D4).
/// </summary>
public sealed record HomeTextsDto(
    string? IntroTitle,
    string? IntroBody,
    string? FormTitle,
    string? FeaturedTitle,
    string? EmptyTitle,
    string? EmptyBody);

/// <summary>`0022` C1 — ana sayfa metinlerinin public okuma yüzeyi; Host yalnız bunu görür.</summary>
public interface IHomeTextQueries
{
    /// <summary>
    /// Verilen dilde kaydedilmiş metinler; etkin olmayan dilde ya da hiç kayıt yokken bütün
    /// alanları <c>null</c> olan bir sonuç (C2).
    /// </summary>
    Task<HomeTextsDto> FindAsync(string locale, CancellationToken cancellationToken);
}
