using System.Globalization;
using CarGallery.BuildingBlocks;

namespace CarGallery.Host.Theme;

/// <summary>
/// Doğrulanmış kuruluma özel tema (R5, R6, R7).
///
/// <para><b>Dört durum birbirinden AYRIDIR</b> (C5) ve ilki dışında hiçbiri sessizce
/// varsayılana düşmez:</para>
/// <list type="number">
///   <item>Hiç tema verilmedi → <see cref="IsDefault"/>; arayüz `tokens.css`'in paletiyle çalışır (AC-10).</item>
///   <item>Kısmi tema → uygulama ayağa kalkmaz, eksik token'ı söyler (AC-11).</item>
///   <item>Tanınmayan anahtar veya biçime uymayan değer → ayağa kalkmaz (AC-12, AC-13, AC-16).</item>
///   <item>Kontrastı yetersiz tema → ayağa kalkmaz, hangi çift hangi eşikte düştü söylenir (AC-22).</item>
/// </list>
///
/// <para><b>Neden "ya tam ya hiç"</b> (R6): kısmi temaya izin vermek, host'un
/// karşılaştırma için varsayılanları da bilmesini — paletin iki yerde yaşamasını —
/// gerektirirdi. Varsayılanlar `tokens.css`'te yaşar ve bir kural yalnızca bir dosyada
/// yaşar.</para>
///
/// <para><b>Türetilmiş token'lar ENJEKTE EDİLMEZ.</b> `tokens.css`'te
/// <c>--color-on-accent-fill: var(--color-ink)</c> olarak duruyorlar ve `:root`
/// <c>&lt;html&gt;</c> elementinin kendisi: inline stille bastığımız <c>--color-ink</c>
/// aynı elementte hesaplandığı için <c>var()</c> doğru değeri bulur. Türetilmiş değeri de
/// basmak, aynı gerçeği iki kez yazmak olurdu.</para>
/// </summary>
internal sealed class ThemeOptions
{
    private ThemeOptions(IReadOnlyList<KeyValuePair<string, string>> values) => Values = values;

    internal static ThemeOptions Default { get; } = new([]);

    /// <summary>Enjekte edilecek token'lar; liste sırası kararlıdır (test edilebilirlik).</summary>
    internal IReadOnlyList<KeyValuePair<string, string>> Values { get; }

    internal bool IsDefault => Values.Count == 0;

    /// <summary>
    /// <c>&lt;html&gt;</c> etiketine basılacak `style` özniteliğinin GÖVDESİ (D10).
    /// Tema verilmediyse boş döner ve öznitelik hiç yazılmaz.
    ///
    /// <para>Kaçış gerekmiyor çünkü her değer C11'in altı haneli hex kalıbından geçti:
    /// tırnak, noktalı virgül veya açılı parantez taşıyan bir değer buraya gelemez.
    /// Doğrulama olmasaydı bu satır serbest metni HTML özniteliğine basan bir yüzey
    /// olurdu — `0005`'in seed-media ucunda aynı risk "aynı origin'de yansıtılmış XSS"
    /// diye yazılıydı.</para>
    /// </summary>
    internal string StyleAttributeValue =>
        string.Join(';', Values.Select(value => $"--{value.Key}:{value.Value}"));

    internal static Result<ThemeOptions> Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Dictionary<string, string> provided = Read(configuration.GetSection(ThemeTokens.SectionName));

        // 1. durum — hiç tema verilmedi. TEK sessiz varsayılana düşme yolu bu (C5).
        if (provided.Count == 0)
        {
            return Result.Success(Default);
        }

        Result<Dictionary<string, ThemeColor>> colors = Validate(provided);

        if (colors.IsFailure)
        {
            return Result.Failure<ThemeOptions>(colors.Error);
        }

        Result contrast = CheckContrast(colors.Value);

        if (contrast.IsFailure)
        {
            return Result.Failure<ThemeOptions>(contrast.Error);
        }

        return Result.Success(new ThemeOptions(
        [
            .. ThemeTokens.ConfigurableKeys.Select(key =>
                new KeyValuePair<string, string>(key, provided[key].ToLowerInvariant()))
        ]));
    }

    /// <summary>
    /// Yapılandırmadan gelen ham anahtarlar. Alt bölüm taşıyan bir anahtar (ör.
    /// <c>Theme:color-ink:x</c>) değeri <c>null</c> olur ve biçim doğrulamasında düşer —
    /// sessizce yok sayılmaz.
    /// </summary>
    private static Dictionary<string, string> Read(IConfiguration section) =>
        section.GetChildren().ToDictionary(
            child => child.Key,
            child => child.Value ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

    private static Result<Dictionary<string, ThemeColor>> Validate(Dictionary<string, string> provided)
    {
        // 3. durum, birinci yarı — tabloda OLMAYAN veya yapılandırılamayan anahtar.
        // `font-family-base` tabloda VAR ama yapılandırılamaz: tipografi, köşe ve aralık
        // anahtarları bu dalda reddedilir (AC-16, X9/D9).
        string? unknown = provided.Keys.FirstOrDefault(key =>
            !ThemeTokens.ConfigurableKeys.Contains(key, StringComparer.OrdinalIgnoreCase));

        if (unknown is not null)
        {
            return Result.Failure<Dictionary<string, ThemeColor>>(
                Error.With(ThemeErrors.UnknownToken, "token", unknown));
        }

        // 2. durum — kısmi tema. "Tam"ın tanımı yapılandırılabilir sütunudur (R6).
        string? missing = ThemeTokens.ConfigurableKeys.FirstOrDefault(key => !provided.ContainsKey(key));

        if (missing is not null)
        {
            return Result.Failure<Dictionary<string, ThemeColor>>(
                Error.With(ThemeErrors.TokenMissing, "token", missing));
        }

        Dictionary<string, ThemeColor> colors = new(StringComparer.OrdinalIgnoreCase);

        // 3. durum, ikinci yarı — biçim. Hiçbir değer enjekte EDİLMEZ (AC-13): sözlük
        // tamamlanmadan dönülüyor ve çağıran yalnız başarılı sonuçtan değer okuyabiliyor.
        foreach (string key in ThemeTokens.ConfigurableKeys)
        {
            if (!ThemeContrast.TryParseSixDigit(provided[key], out ThemeColor color))
            {
                return Result.Failure<Dictionary<string, ThemeColor>>(
                    Error.With(ThemeErrors.InvalidColorFormat, "token", key));
            }

            colors[key] = color;
        }

        return Result.Success(colors);
    }

    /// <summary>
    /// 4. durum — kontrast. Çiftler ve eşikler <see cref="ThemeTokens.Pairs"/>'dan okunur;
    /// burada ikinci bir eşik tanımı YOKTUR (C12).
    /// </summary>
    private static Result CheckContrast(Dictionary<string, ThemeColor> colors)
    {
        foreach (ContrastPair pair in ThemeTokens.Pairs)
        {
            ThemeColor foreground = colors[ThemeTokens.Resolve(pair.Foreground)];
            ThemeColor background = colors[ThemeTokens.Resolve(pair.Background)];

            decimal ratio = ThemeContrast.Ratio(foreground, background);

            if (ratio >= pair.Threshold)
            {
                continue;
            }

            // AC-22 — hangi ÇİFTİN hangi EŞİĞİ karşılamadığı söylenir. Ölçülen oran da
            // taşınıyor: "yetersiz" demek, ne kadar yetersiz olduğunu söylemeden
            // düzeltilemez bir hata mesajıdır.
            return Result.Failure(new Error(ThemeErrors.ContrastBelowThreshold, new Dictionary<string, string>
            {
                ["foreground"] = pair.Foreground,
                ["background"] = pair.Background,
                ["threshold"] = pair.Threshold.ToString("0.0", CultureInfo.InvariantCulture),
                ["measured"] = Math.Round(ratio, 2).ToString("0.00", CultureInfo.InvariantCulture),
                ["criterion"] = pair.Criterion,
            }));
        }

        return Result.Success();
    }
}
