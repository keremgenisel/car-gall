namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// Kararlı hata kodları. Kullanıcıya gösterilecek metin TAŞIMAZ — uç kodu kendi
/// katalogundan çevirir (C7, Yasak Listesi 23, `docs/conventions.md` § 5).
///
/// Adlandırma `&lt;alan&gt;.&lt;durum&gt;[.&lt;ayrıntı&gt;]`; kod bir kez yayınlandıktan sonra
/// ANLAMI DEĞİŞMEZ.
/// </summary>
internal static class MediaErrors
{
    /// <summary>D10(a) — tür içerikten belirlendi ve desteklenen kümede değil (AC-3, AC-4).</summary>
    internal const string UnsupportedType = "media.unsupported_type";

    /// <summary>D10(b) — yapılandırılan bayt sınırı aşıldı (AC-5).</summary>
    internal const string TooLarge = "media.too_large";

    /// <summary>D10(c) — çözülmüş piksel bütçesi aşıldı (AC-6). Bayt sınırı bunu görmez.</summary>
    internal const string PixelBudgetExceeded = "media.pixel_budget_exceeded";

    /// <summary>Görsel hiç çözülemedi: bozuk dosya ya da gövdesi kesilmiş yükleme.</summary>
    internal const string Undecodable = "media.undecodable";

    /// <summary>AC-14 — tekrar deneme yalnız `başarısız` durumdan tetiklenebilir.</summary>
    internal const string NotRetryable = "media.not_retryable";

    internal const string NotFound = "media.not_found";

    /// <summary>
    /// D4/AC-18 — filigran kaynağı okunamadı. Varlık BAŞARISIZ olur; filigransız türev
    /// üretilmez, çünkü galeri sahibinin satın aldığı garanti tam olarak budur.
    /// </summary>
    internal const string WatermarkUnavailable = "media.watermark_unavailable";

    /// <summary>M1/AC-10 — tanımlı olmayan durum geçişi.</summary>
    internal const string InvalidStatusTransition = "media.invalid_status_transition";

    /// <summary>Depolama sağlayıcısı yazma/okuma isteğini reddetti.</summary>
    internal const string StorageUnavailable = "media.storage_unavailable";
}
