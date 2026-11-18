namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// `0015` R5, R7 — vitrin sıralamaları. <see cref="Default"/> adrese yazılmaz (D9) ve öne
/// çıkarma ağırlığını kullanan TEK sıralamadır (D7).
/// </summary>
internal enum ShowcaseSort
{
    Default,
    Newest,
    PriceAscending,
    PriceDescending,
    MileageAscending,
    YearDescending,
    YearAscending,
}

internal static class ShowcaseSortParser
{
    /// <summary>
    /// C12 — eşleşme TAM ve harfe duyarlı (`0001` C13'ün slug kuralı). Tanınmayan değer bir
    /// hata değildir; varsayılan sıralamaya düşer (AC-15).
    /// </summary>
    internal static ShowcaseSort Parse(string? raw) => raw switch
    {
        "newest" => ShowcaseSort.Newest,
        "price-asc" => ShowcaseSort.PriceAscending,
        "price-desc" => ShowcaseSort.PriceDescending,
        "mileage-asc" => ShowcaseSort.MileageAscending,
        "year-desc" => ShowcaseSort.YearDescending,
        "year-asc" => ShowcaseSort.YearAscending,
        _ => ShowcaseSort.Default,
    };
}
