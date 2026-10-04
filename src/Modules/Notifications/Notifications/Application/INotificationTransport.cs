using CarGallery.Modules.Notifications.Domain;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>Gönderilecek mesaj. Alıcı adresi BURADA taşınır, kayıtta değil (C5).</summary>
internal sealed record OutboundMessage(string Recipient, string Subject, string Body);

/// <summary>
/// Gönderim sonucu ve hatanın SINIFI (R11).
///
/// Sınıf ayrımı taşıyıcının işidir çünkü yalnız o bilir: sağlayıcının `5xx`'i geçici,
/// "adres reddedildi" kalıcıdır. Üst katman bu ayrımı tahmin etmeye çalışsaydı sağlayıcıya
/// özgü bilgi üst katmana sızardı.
/// </summary>
internal sealed record SendResult(SendFailureKind? Failure, string? Code)
{
    internal static SendResult Success() => new(null, null);

    internal static SendResult Transient(string code) => new(SendFailureKind.Transient, code);

    internal static SendResult Permanent(string code) => new(SendFailureKind.Permanent, code);

    internal bool Succeeded => Failure is null;
}

/// <summary>
/// D4 — gönderici bir soyutlamanın arkasında durur; servise geçiş tek bir sınıfı değiştirir.
/// Kanal soyutlaması burada, uygulaması V1'de yalnız e-posta (X1).
/// </summary>
internal interface INotificationTransport
{
    Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken);
}
