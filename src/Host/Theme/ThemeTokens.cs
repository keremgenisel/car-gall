namespace CarGallery.Host.Theme;

/// <summary>Token'ın kim tarafından belirlendiği.</summary>
internal enum ThemeTokenKind
{
    /// <summary>Dağıtım yapılandırmasından gelir (R5). Tema verildiyse ZORUNLUDUR (R6).</summary>
    Configurable,

    /// <summary>Başka bir token'dan türetilir; yapılandırılamaz ama değeri bilinir.</summary>
    Derived,

    /// <summary>`tokens.css`'te sabit. Host değerini HİÇ bilmez.</summary>
    Fixed,
}

/// <summary>Token tanımı. Değer BURADA YOK — varsayılanlar `tokens.css`'te yaşar (R6).</summary>
internal sealed record ThemeToken(string Key, ThemeTokenKind Kind, string? DerivedFrom = null);

/// <summary>Denetlenecek kontrast çifti ve eşiği.</summary>
internal sealed record ContrastPair(string Foreground, string Background, decimal Threshold, string Criterion);

/// <summary>
/// Kapalı token listesi ve kontrast çift tablosu — spec frontend-002 Context § Token
/// tablosu ve § Çift tablosu (D11).
///
/// <para><b>Burada DEĞER yoktur ve bu bilinçlidir.</b> Varsayılanlar `tokens.css`'te
/// yaşar; host onları bilmez. R6 kısmi temayı bu yüzden yasaklıyor: host varsayılanı
/// bilseydi palet iki yerde yaşardı. Burada yaşayan şey <b>hangi anahtarların var olduğu</b>
/// ve <b>hangi çiftin hangi eşiği karşılaması gerektiği</b> — ikisi de değer değil, kural.</para>
///
/// <para><b>Yapılandırma anahtarı = CSS token adı</b> (baştaki <c>--</c> hariç). İki ayrı
/// ad kullanmak, aralarında üçüncü bir eşleme tablosu doğurur ve o tablo bir gün ayrışır.
/// Hata mesajları bu yüzden doğrudan grep edilebilir bir ad söyler (AC-11, AC-22).</para>
///
/// <para><b>C12 — eşik tek yerde:</b> R7'nin iki kapısı da <see cref="Pairs"/>'ı okur.
/// Başlangıçtaki yapılandırma kapısı ve CI'daki varsayılan palet testi aynı satırlardan
/// beslenir; ikisi ayrı yazılsaydı eşik iki yerde yaşardı.</para>
/// </summary>
internal static class ThemeTokens
{
    internal const string SectionName = "Theme";

    internal const string Surface = "color-surface";
    internal const string SurfaceMuted = "color-surface-muted";
    internal const string Ink = "color-ink";
    internal const string InkMuted = "color-ink-muted";
    internal const string InkInverse = "color-ink-inverse";
    internal const string AccentFill = "color-accent-fill";
    internal const string AccentText = "color-accent-text";
    internal const string AccentSurface = "color-accent-surface";
    internal const string OnAccentFill = "color-on-accent-fill";
    internal const string FocusRing = "color-focus-ring";
    internal const string Border = "color-border";
    internal const string Danger = "color-danger";
    internal const string DangerSurface = "color-danger-surface";
    internal const string Backdrop = "color-backdrop";
    internal const string FontFamilyBase = "font-family-base";
    internal const string FontFamilyArabic = "font-family-arabic";

    /// <summary>
    /// Kapalı liste. Tablonun dışında varsayılan yoktur ve tabloda olmayan anahtar
    /// yapılandırılamaz (R2, AC-12).
    ///
    /// <para><b>D13</b> — kontrast çiftine giren her token yapılandırılabilir. Kapı bir
    /// çifti ancak iki ucunu da biliyorsa denetleyebilir; bir ucu `tokens.css`'te sabit
    /// kalsaydı host onu bilemez (R6) ve çift denetimsiz kalırdı.</para>
    /// </summary>
    internal static IReadOnlyList<ThemeToken> All { get; } =
    [
        new(Surface, ThemeTokenKind.Configurable),
        new(SurfaceMuted, ThemeTokenKind.Configurable),
        new(Ink, ThemeTokenKind.Configurable),
        new(InkMuted, ThemeTokenKind.Configurable),
        new(InkInverse, ThemeTokenKind.Configurable),
        new(AccentFill, ThemeTokenKind.Configurable),
        new(AccentText, ThemeTokenKind.Configurable),
        new(AccentSurface, ThemeTokenKind.Configurable),
        new(Border, ThemeTokenKind.Configurable),
        new(Danger, ThemeTokenKind.Configurable),
        new(DangerSurface, ThemeTokenKind.Configurable),

        // Türetilmiş: dolgudan DEĞİL gövde metninden türetilir (Token tablosu). Dolgudan
        // türetmek, limon dolgunun üstüne limon metin yazmak demekti.
        new(OnAccentFill, ThemeTokenKind.Derived, Ink),
        new(FocusRing, ThemeTokenKind.Derived, AccentText),

        // Sabit: metin taşımaz, kontrast çiftine girmez; alfa taşıdığı için biçimi de
        // farklıdır (C11, `#RRGGBBAA`).
        new(Backdrop, ThemeTokenKind.Fixed),

        // X9/D9 — tipografi V1'de yapılandırılamaz: yapılandırma bir aile ADI verebilir
        // ama DOSYASINI veremez ve dosyası olmayan aile adında tarayıcı sessizce başka
        // fonta düşer (tuzak 3).
        new(FontFamilyBase, ThemeTokenKind.Fixed),
        new(FontFamilyArabic, ThemeTokenKind.Fixed),
    ];

    /// <summary>Tema verildiyse verilmek ZORUNLU olan anahtarlar (R6, AC-11).</summary>
    internal static IReadOnlyList<string> ConfigurableKeys { get; } =
    [
        .. All.Where(token => token.Kind is ThemeTokenKind.Configurable).Select(token => token.Key)
    ];

    /// <summary>
    /// Çiftler spec'ten değil, CSS'te GERÇEKTEN var olan kullanımlardan türetildi. Son
    /// altı satır ilk taslakta yoktu ve üçü bugün kullanılıyor: `color-ink-muted` on yerde
    /// yardımcı metin, `color-danger` beş yerde hata metni, `color-ink-inverse` rozet metni.
    ///
    /// <para>Eşik 3:1 yalnız odak halkasında: halka bir metin değil arayüz öğesidir
    /// (docs/frontend.md § 7). Geri kalan her çift metin taşır ve 4.5:1'dir — bir token'ın
    /// "bazen 24px kullanılıyor" olması normal boyuttaki eşiğini düşürmez (AC-21).</para>
    /// </summary>
    internal static IReadOnlyList<ContrastPair> Pairs { get; } =
    [
        new(AccentText, Surface, 4.5m, "AC-17"),
        new(AccentText, SurfaceMuted, 4.5m, "AC-17"),
        new(Ink, Surface, 4.5m, "AC-18"),
        new(Ink, SurfaceMuted, 4.5m, "AC-18"),
        new(OnAccentFill, AccentFill, 4.5m, "AC-19"),
        new(FocusRing, Surface, 3m, "AC-20"),
        new(FocusRing, SurfaceMuted, 3m, "AC-20"),
        new(FocusRing, AccentFill, 3m, "AC-20"),
        new(InkMuted, Surface, 4.5m, "AC-31"),
        new(InkMuted, SurfaceMuted, 4.5m, "AC-31"),
        new(AccentText, AccentSurface, 4.5m, "AC-32"),
        new(Danger, Surface, 4.5m, "AC-33"),
        new(Danger, DangerSurface, 4.5m, "AC-33"),
        new(InkInverse, Ink, 4.5m, "AC-34"),
    ];

    /// <summary>Türetilmiş token'ı kaynağına indirger; kapı böylece değerini bilir.</summary>
    internal static string Resolve(string key) =>
        All.FirstOrDefault(token => token.Key == key)?.DerivedFrom ?? key;
}
