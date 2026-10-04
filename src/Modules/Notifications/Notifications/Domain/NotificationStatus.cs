namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// Kuyruk satırının durumu. Veritabanında KOD olarak saklanır; çevrilmiş adı saklanmaz
/// (Yasak Listesi 22).
///
/// <see cref="NoChannel"/> bir HATA DEĞİLDİR: 0005 R4 telefonu tek iletişim kanalı olarak
/// kabul ediyor ve e-postası olmayan ziyaretçiye karşılık gönderilemez. Kaydı tutulmazsa
/// galeri "neden ulaşılmadı" sorusunu hiçbir yerden cevaplayamaz (0009 R6, AC-11).
/// </summary>
internal enum NotificationStatus
{
    Queued = 1,
    Sent = 2,
    Failed = 3,
    NoChannel = 4,
}

/// <summary>Kanal soyutlaması burada; V1'de yalnız e-posta uygulanır (X1).</summary>
internal enum NotificationChannel
{
    Email = 1,
}

/// <summary>
/// Gönderim hatasının sınıfı (R11).
///
/// Ayrım kozmetik değil: geçersiz bir adrese beş kez denemek kuyruğu kalıcı hatalarla
/// doldurur ve gerçek geçici hataların denemelerini geciktirir.
/// </summary>
internal enum SendFailureKind
{
    Transient = 1,
    Permanent = 2,
}
