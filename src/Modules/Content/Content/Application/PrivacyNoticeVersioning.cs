using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;

namespace CarGallery.Modules.Content.Application;

/// <summary>
/// `0019` P3 — sürüm doğurma kuralının TEK yeri. Komut sayfayı yükler yüklemez
/// <see cref="Capture"/> ile `privacy`'nin yayındaki dillerinin kopyasını alır, değişikliği
/// uygular ve kaydetmeden hemen önce <see cref="Record"/>'u çağırır. Sürüm, sayfa
/// değişikliğiyle AYNI <c>SaveChangesAsync</c>'e girer: reddedilen kayıt (409, slug yarışı)
/// geride sürüm bırakmaz (C3, Bilinen tuzak 5).
/// </summary>
internal static class PrivacyNoticeVersioning
{
    /// <summary>
    /// Değişiklikten ÖNCEKİ hâl: yayındaki her dilin başlığı ve gövdesi. Sayfa `privacy`
    /// değilse <c>null</c> — diğer sayfalar sürüm tutmaz (R12, D10).
    /// </summary>
    internal static IReadOnlyDictionary<string, (string Title, string Body)>? Capture(ContentPage page)
    {
        if (page.SystemKey != SystemPageKeys.Privacy)
        {
            return null;
        }

        return page.Localizations
            .Where(satir => satir.Status == PublicationStatus.Published)
            .ToDictionary(satir => satir.Locale, satir => (satir.Title, satir.Body), StringComparer.Ordinal);
    }

    /// <summary>
    /// D5, D16, D17 — şimdi yayında olan bir dil, önce yayında DEĞİLSE (taslaktan yayına
    /// geçiş) ya da başlığı veya gövdesi saklanan değerden ORDİNAL olarak farklıysa sürüm
    /// doğurur. Zaten yayındaki dile yayın çağrısı, yalnız slug değişikliği, değişmeden
    /// gönderilen dil ve taslak dilde kayıt doğurmaz. Yayında olmayan dilin işaretçisi
    /// boşalır: işaretçi yalnız yayındaki metni gösterir (P2).
    /// </summary>
    internal static void Record(
        ContentDbContext context,
        ContentPage page,
        IReadOnlyDictionary<string, (string Title, string Body)>? before,
        DateTimeOffset now)
    {
        if (before is null)
        {
            return;
        }

        foreach (PageLocalization satir in page.Localizations)
        {
            if (satir.Status != PublicationStatus.Published)
            {
                satir.PublishedVersionId = null;
                continue;
            }

            bool degisti = !before.TryGetValue(satir.Locale, out (string Title, string Body) onceki)
                || !string.Equals(onceki.Title, satir.Title, StringComparison.Ordinal)
                || !string.Equals(onceki.Body, satir.Body, StringComparison.Ordinal);

            if (!degisti)
            {
                continue;
            }

            PrivacyNoticeVersion surum = PrivacyNoticeVersion.Record(satir.Locale, satir.Title, satir.Body, now);
            context.PrivacyNoticeVersions.Add(surum);
            satir.PublishedVersionId = surum.Id;
        }
    }
}
