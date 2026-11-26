using System.Globalization;
using System.Text;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// İlanın kalıcı ve paylaşılabilir adresi (0002 R2, E9).
///
/// Slug İLK YAYINA ALMA anında üretilir ve BİR DAHA DEĞİŞMEZ. Model adı sonradan
/// düzeltilse bile adres kırılmaz; paylaşılmış her bağlantı çalışmaya devam eder.
/// Tekilliği ilan kimliğinin ilk altı karakterinden gelen sonek sağlar.
///
/// Slug her dilde AYNIDIR: Latin, kanonik addan türer (docs/architecture.md § 3).
/// Türkçe karakterler ASCII karşılığına indirgenir — `lower()` ile değil, açık eşlemeyle:
/// Türkçe collation altında `I` ve `İ` sessizce yanlış sonuç verir (Yasak Listesi 27).
///
/// <para><b>Uzunluk sınırı tek yerde:</b> <see cref="MaxLength"/>. Üretim, adresten okuma ve
/// iki kolon (`listings.slug`, `showcase_listings.slug`) aynı sabiti kullanır. Kaçan hata
/// (2026-11-26): `Create` sınır koymuyordu; en uzun marka (80) + model (120) adlı ilanın adresi
/// 213 karaktere çıkıyor, yayın `varchar(160)` aşımıyla `500` veriyor ve ilan taslak kalıyordu.</para>
/// </summary>
internal readonly record struct ListingSlug
{
    internal const string InvalidSource = "listing_slug.invalid_source";

    /// <summary>Adresin en çok uzunluğu; kolonların genişliği de buradan gelir.</summary>
    internal const int MaxLength = 160;

    private const int SuffixLength = 6;

    private static readonly Dictionary<char, string> TurkishFolding = new()
    {
        ['ı'] = "i", ['İ'] = "i", ['ş'] = "s", ['Ş'] = "s", ['ğ'] = "g", ['Ğ'] = "g",
        ['ü'] = "u", ['Ü'] = "u", ['ö'] = "o", ['Ö'] = "o", ['ç'] = "c", ['Ç'] = "c",
    };

    private ListingSlug(string value) => Value = value;

    internal string Value { get; }

    internal static Result<ListingSlug> Create(string canonicalModelName, int modelYear, ListingId listingId)
    {
        // Kontrol MODEL ADI üzerindedir, gövdenin tamamı üzerinde değil: yıl her zaman
        // rakam ürettiği için gövde asla boş kalmaz ve böyle bir koruma ölü kod olurdu.
        // Kanonik ad Latin olmak zorundadır (docs/architecture.md § 3); Arapça bir kanonik
        // ad yalnız yıl ve sonekten oluşan anlamsız bir adres üretirdi.
        string ad = Foldla(canonicalModelName);

        if (ad.Length == 0)
        {
            return Result.Failure<ListingSlug>(new Error(InvalidSource));
        }

        string yıl = modelYear.ToString(CultureInfo.InvariantCulture);
        ad = Kisalt(ad, MaxLength - yıl.Length - SuffixLength - 2);
        // Sonek kimliğin SON altı karakterinden gelir, ilkinden değil: Guid v7'nin baş
        // baytları zaman damgasıdır ve ilk altı hex karakter ~65 saniyede bir değişir —
        // aynı dakikada yayınlanan iki ilan aynı soneki alır ve tekil adres çakışırdı.
        string tam = listingId.Value.ToString("N", CultureInfo.InvariantCulture);
        string sonek = tam[^SuffixLength..];
        return Result.Success(new ListingSlug($"{ad}-{yıl}-{sonek}"));
    }

    /// <summary>Adresten gelen değeri kabul eder; biçimi tutmayan değer tanınmaz (AC-5).</summary>
    internal static bool TryParse(string value, out ListingSlug slug)
    {
        slug = default;

        if (value is not { Length: > 0 and <= MaxLength })
        {
            return false;
        }

        foreach (char character in value)
        {
            bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
            if (!allowed)
            {
                return false;
            }
        }

        if (value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        slug = new ListingSlug(value);
        return true;
    }

    public override string ToString() => Value;

    /// <summary>
    /// Ad parçasını sığdırır: sınırı aşan ad SON KELİME SINIRINDA (tire) kesilir, yıl ve kimlik
    /// soneki her zaman korunur — tekillik soneke dayanır, ad kısalınca bozulmaz. Tek kelimelik
    /// ve sınırdan uzun ad sınırda kesilir. Sığan ad olduğu gibi kalır.
    /// </summary>
    private static string Kisalt(string ad, int sigacak)
    {
        if (ad.Length <= sigacak)
        {
            return ad;
        }

        int sinir = ad.LastIndexOf('-', sigacak);
        return (sinir > 0 ? ad[..sinir] : ad[..sigacak]).TrimEnd('-');
    }

    private static string Foldla(string source)
    {
        StringBuilder builder = new(source.Length);
        bool öncekiTire = true;

        foreach (char character in source)
        {
            if (TurkishFolding.TryGetValue(character, out string? karşılık))
            {
                builder.Append(karşılık);
                öncekiTire = false;
                continue;
            }

            char küçük = char.ToLowerInvariant(character);

            if (küçük is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                builder.Append(küçük);
                öncekiTire = false;
                continue;
            }

            if (!öncekiTire)
            {
                builder.Append('-');
                öncekiTire = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
