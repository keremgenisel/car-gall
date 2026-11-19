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

/// <summary>
/// Şablon anahtarları. `0009`'un ikisi işlemseldir; `0018` pazarlama sınıfını İLK KEZ var
/// ediyor (`0009` X2'nin kapanışı): bülten pazarlamadır, teyit iletisi işlemseldir (L7, C3).
///
/// <para>`0018` **D11** — bülten başlık, satır ve altlık olarak AYRI şablonlardır; "Fiyat
/// sorunuz" ve "Diğer yeni araçlar…" metinleri de şablon kataloğundadır (C8). Şablon motoru
/// döngü bilmiyor; satırı ayrı şablon yapmak motoru değiştirmeden listeyi kurar.</para>
/// </summary>
internal static class TemplateKeys
{
    internal const string LeadReceivedGallery = "lead.received.gallery";
    internal const string LeadReceivedVisitor = "lead.received.visitor";

    internal const string NewsletterConfirmation = "newsletter.confirmation";
    internal const string NewsletterHeader = "newsletter.header";
    internal const string NewsletterRow = "newsletter.row";
    internal const string NewsletterFooter = "newsletter.footer";
    internal const string NewsletterPriceOnRequest = "newsletter.price_on_request";
    internal const string NewsletterMore = "newsletter.more";

    internal static readonly string[] All =
    [
        LeadReceivedGallery,
        LeadReceivedVisitor,
        NewsletterConfirmation,
        NewsletterHeader,
        NewsletterRow,
        NewsletterFooter,
        NewsletterPriceOnRequest,
        NewsletterMore,
    ];
}
