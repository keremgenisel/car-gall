namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// Şablon: (anahtar × locale) → konu + gövde (R9).
///
/// **Kurulum verisidir** ve seed migration ile gelir (C13). Panelden düzenlenmesi kapsam
/// dışıdır (X5); düzenleme ekranı kendi yetki ve önizleme kriterlerini getirir.
///
/// Gövde düz metindir (X6): zengin HTML, marka tasarımı ve e-posta istemci uyumluluk
/// matrisi ayrı bir iştir ve bildirimin işini yapmak için gerekmez.
/// </summary>
internal sealed class NotificationTemplate
{
    internal required string TemplateKey { get; init; }

    internal required string Locale { get; init; }

    internal required string Subject { get; init; }

    internal required string Body { get; init; }
}

/// <summary>Bu spec'in ürettiği iki şablon anahtarı. Üçüncüsü yoktur (X2: pazarlama yok).</summary>
internal static class TemplateKeys
{
    internal const string LeadReceivedGallery = "lead.received.gallery";
    internal const string LeadReceivedVisitor = "lead.received.visitor";

    internal static readonly string[] All = [LeadReceivedGallery, LeadReceivedVisitor];
}
