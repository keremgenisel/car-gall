namespace CarGallery.Host.Theme;

/// <summary>
/// Kararlı hata kodları: `&lt;alan&gt;.&lt;durum&gt;`, snake_case
/// (docs/conventions.md § 5). Burada yalnız KOD vardır; hangi token olduğu
/// <see cref="CarGallery.BuildingBlocks.Error.Parameters"/> ile taşınır — kodun içine
/// gömülse kod artık kararlı olmazdı.
/// </summary>
internal static class ThemeErrors
{
    /// <summary>R6 — tema kısmen verildi; yapılandırılabilir bir anahtar eksik (AC-11).</summary>
    internal const string TokenMissing = "theme.token_missing";

    /// <summary>Tabloda olmayan veya yapılandırılamayan anahtar (AC-12, AC-16).</summary>
    internal const string UnknownToken = "theme.unknown_token";

    /// <summary>C11 — altı haneli hex dışında bir biçim (AC-13).</summary>
    internal const string InvalidColorFormat = "theme.invalid_color_format";

    /// <summary>R7 — çift eşiği karşılamıyor (AC-17…AC-21, AC-31…AC-34).</summary>
    internal const string ContrastBelowThreshold = "theme.contrast_below_threshold";
}
