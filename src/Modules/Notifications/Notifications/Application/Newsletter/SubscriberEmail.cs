using MimeKit;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D10** — abonenin adresi. Ham metin hiçbir yerde kimlik olmaz: girdi ayrıştırılır
/// ve MimeKit'in çözdüğü adres kullanılır.
///
/// <para><b>Ayrıştırıcı tek başına yetmez</b> (QA ölçümü, MimeKit 4.18.1): <c>ali</c>,
/// <c>ali@ornek</c> ve <c>Ali &lt;ali@ornek.com&gt;</c> kabul ediliyor. Üç ek kural var:
/// çözülen adres girdinin KENDİSİ olmalı (görünen ad, köşeli ayraç, yorum yok); alan adı
/// olmalı ve en az bir nokta taşımalı; uzunluk 254'ü aşmamalı (RFC 5321 yolu).</para>
///
/// <para><b>Kimlik küçük harfli eştir.</b> <c>Ali@Ornek.com</c> ile <c>ali@ornek.com</c> aynı
/// adrestir ve bu, saklamada tekil indeksle korunur (AC-21, AC-22). Bedeli (**D10**): yerel
/// kısmı harf duyarlı nadir sunucularda iki ayrı adres tek sayılır.</para>
/// </summary>
internal readonly record struct SubscriberEmail(string Address, string Normalized)
{
    internal const int MaximumLength = 254;

    internal static bool TryParse(string? input, out SubscriberEmail email)
    {
        email = default;

        string trimmed = (input ?? string.Empty).Trim();

        if (trimmed.Length is 0 or > MaximumLength)
        {
            return false;
        }

        if (!MailboxAddress.TryParse(trimmed, out MailboxAddress? parsed)
            || !string.IsNullOrEmpty(parsed.Name)
            || !string.Equals(parsed.Address, trimmed, StringComparison.Ordinal))
        {
            return false;
        }

        int at = parsed.Address.LastIndexOf('@');

        if (at <= 0 || at == parsed.Address.Length - 1)
        {
            return false;
        }

        string domain = parsed.Address[(at + 1)..];

        if (!domain.Contains('.', StringComparison.Ordinal)
            || domain.StartsWith('.')
            || domain.EndsWith('.'))
        {
            return false;
        }

        email = new SubscriberEmail(parsed.Address, parsed.Address.ToLowerInvariant());
        return true;
    }
}
