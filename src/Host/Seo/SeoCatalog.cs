using System.Text.Json;
using System.Text.RegularExpressions;

namespace CarGallery.Host.Seo;

/// <summary>
/// `0004` **D12** · **P2** — belgenin sabit metinleri ön yüz deposunun çeviri dosyalarından:
/// ekranda zaten görünen metin VİTRİN kataloğundan (`frontend/src/shared/i18n/locales/{dil}.json`),
/// SEO'ya özgü metin SEO kümesinden (`…/locales/seo/{dil}.json`). Host'ta metin tutulmaz. Dosyalar
/// derleme çıktısına `i18n/` altında bağlanır (`Host.csproj`, `Dockerfile`).
///
/// <para><b>Eksik anahtarla başlamaz:</b> Host'un kullandığı her anahtar etkin her dilde dolu
/// olmalı; değilse uygulama başlarken hangi dilde hangi anahtarın eksik olduğunu söyleyerek düşer.
/// Belgeyi sessizce metinsiz basmaktansa yanlış kurulumu hiç açmamak (`0009` AC-27'nin deseni).</para>
///
/// <para><b>Biçim:</b> ön yüzün ICU kalıplarından yalnız <c>{ad}</c> ve <c>{ad, number}</c>
/// kullanılır; sayının yazımını çağıran <see cref="CarGallery.BuildingBlocks.Formatting.ShowcaseFormat"/>
/// ile önceden yapar. Başka ICU biçimi (çoğul, seçim) bu anahtarlarda yoktur.</para>
/// </summary>
internal sealed partial class SeoCatalog
{
    internal const string HomeIntroBody = "home.intro.body";
    internal const string PriceOnRequest = "detail.priceOnRequest";
    internal const string MileageValue = "detail.mileageValue";
    internal const string NotFoundTitle = "content.notFound.title";
    internal const string HomeFallbackTitle = "seo.home.fallbackTitle";
    internal const string ListTitle = "seo.list.title";
    internal const string ListDescription = "seo.list.description";
    internal const string DetailDescription = "seo.detail.description";
    internal const string DocumentFallbackTitle = "seo.document.fallbackTitle";
    internal const string FavoritesTitle = "seo.favorites.title";
    internal const string CompareTitle = "seo.compare.title";
    internal const string ContactTitle = "seo.contact.title";
    internal const string ContactDescription = "seo.contact.description";

    /// <summary>Host'un okuduğu bütün anahtarlar; başlangıç denetimi ve katalog kapısı bunu kullanır.</summary>
    internal static IReadOnlyList<string> UsedKeys { get; } =
    [
        HomeIntroBody, PriceOnRequest, MileageValue, NotFoundTitle,
        HomeFallbackTitle, ListTitle, ListDescription, DetailDescription, DocumentFallbackTitle,
        FavoritesTitle, CompareTitle,
    ];

    internal const string Directory = "i18n";

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogs;

    private SeoCatalog(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogs,
        IReadOnlySet<string> listQueryParameters)
    {
        this.catalogs = catalogs;
        ListQueryParameters = listQueryParameters;
    }

    /// <summary>`0004` **D23** · **P3** — listenin okuduğu parametre adları (`listQueryParams.json`).</summary>
    internal IReadOnlySet<string> ListQueryParameters { get; }

    internal string Text(string locale, string key) => catalogs[locale][key];

    /// <summary><c>{ad}</c> ve <c>{ad, number}</c> yer tutucularını verilen (önceden biçimlenmiş) değerlerle doldurur.</summary>
    internal string Format(string locale, string key, IReadOnlyDictionary<string, string> values) =>
        Placeholder().Replace(Text(locale, key), match => values.TryGetValue(match.Groups["ad"].Value, out string? deger)
            ? deger
            : throw new InvalidOperationException($"'{key}' anahtarının '{match.Groups["ad"].Value}' değeri verilmedi."));

    internal static SeoCatalog Load(string baseDirectory, IEnumerable<string> enabledLocales)
    {
        string kok = Path.Combine(baseDirectory, Directory);
        Dictionary<string, IReadOnlyDictionary<string, string>> kataloglar = new(StringComparer.Ordinal);
        List<string> eksikler = [];

        foreach (string dil in enabledLocales)
        {
            Dictionary<string, string> birlesik = new(StringComparer.Ordinal);

            foreach (string dosya in new[] { Path.Combine(kok, "vitrin", $"{dil}.json"), Path.Combine(kok, "seo", $"{dil}.json") })
            {
                if (!File.Exists(dosya))
                {
                    eksikler.Add($"{dil}: {Path.GetRelativePath(baseDirectory, dosya)} yok");
                    continue;
                }

                foreach ((string anahtar, string deger) in Oku(dosya))
                {
                    birlesik[anahtar] = deger;
                }
            }

            eksikler.AddRange(UsedKeys
                .Where(anahtar => !birlesik.TryGetValue(anahtar, out string? deger) || string.IsNullOrWhiteSpace(deger))
                .Select(anahtar => $"{dil}: {anahtar}"));

            kataloglar[dil] = birlesik;
        }

        string parametreDosyasi = Path.Combine(kok, "listQueryParams.json");

        if (!File.Exists(parametreDosyasi))
        {
            eksikler.Add($"{Path.GetRelativePath(baseDirectory, parametreDosyasi)} yok");
        }

        if (eksikler.Count > 0)
        {
            throw new InvalidOperationException(
                "Belge metinleri eksik (0004 D12); uygulama başlamıyor: " + string.Join("; ", eksikler));
        }

        HashSet<string> parametreler = [.. Oku(parametreDosyasi).Values];

        return new SeoCatalog(kataloglar, parametreler);
    }

    private static Dictionary<string, string> Oku(string dosya) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dosya))
            ?? throw new InvalidOperationException($"{dosya} okunamadı.");

    [GeneratedRegex(@"\{(?<ad>[A-Za-z]+)(\s*,\s*number)?\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
