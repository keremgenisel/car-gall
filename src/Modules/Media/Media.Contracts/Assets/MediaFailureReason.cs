namespace CarGallery.Modules.Media.Contracts.Assets;

/// <summary>
/// AC-33 / D8 — başarısızlık sebebinin **kapalı** kümesi. Serbest metin yok, çevrilmiş
/// metin yok (Altın Kural 6, Yasak Listesi 23); gösterim uçta, koddan çevrilir.
///
/// <para><b>Kümenin kapalı olması şart</b> ve gerekçesi kayıtlı: `0010b`'nin *"tek
/// `InvalidSlug` dört alanı temsil ediyordu"* hatası aksi hâlde tekrarlanır. Sonradan
/// değer eklemek bir **sözleşme değişikliğidir** ve öyle görünür.</para>
///
/// <para>`Contracts` sınırında kendi enum'u olarak geçer; tüketici modül kendi enum'unu
/// tanımlar ve eşler (Yasak Listesi 9). Veritabanında çevrilmiş ad değil <b>kod</b>
/// saklanır (Yasak Listesi 22).</para>
/// </summary>
public enum MediaFailureReason
{
    /// <summary>Görsel hiç çözülemedi: bozuk dosya ya da gövdesi kesilmiş yükleme.</summary>
    Undecodable = 1,

    /// <summary>Filigran kaynağı okunamadı; filigransız türev ÜRETİLMEZ (AC-23).</summary>
    WatermarkUnavailable = 2,

    /// <summary>Depolama sağlayıcısı okuma veya yazma isteğini reddetti.</summary>
    StorageUnavailable = 3,
}
