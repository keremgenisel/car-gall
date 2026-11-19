using System.Globalization;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>Kurulan ileti ya da kurulamamasının sınıfı ve kodu.</summary>
internal sealed record Composition(OutboundMessage? Message, SendFailureKind? Failure, string? FailureCode)
{
    internal static Composition Ready(OutboundMessage message) => new(message, null, null);

    internal static Composition Permanent(string code) => new(null, SendFailureKind.Permanent, code);
}

/// <summary>
/// `0018` — aboneliğe bağlı iki iletinin gövdesi: teyit iletisi (işlemsel) ve bülten
/// (pazarlama). Kuyruk, tekrar deneme ve hata sınıfları `0009`'undur (R10); burası yalnız
/// alıcıyı ve gövdeyi kurar.
///
/// <para><b>Alıcı adresi kayıttan değil KAYNAKTAN okunur</b> (C5): aboneliğin o anki kaydı.
/// Abonelik etkin değilse bülten gönderilmez ve KALICI başarısız işaretlenir (**D22**, AC-46);
/// teyit bekliyor değilse teyit iletisi de gönderilmez.</para>
///
/// <para><b>Küme dağıtımda yeniden hesaplanmaz</b> (**D21**): ilanlar dönem ↔ ilan
/// kayıtlarından gelir; yalnız değişken veri — model adı, fiyat, şube adı ve DURUM — kaynaktan
/// okunur. Hiçbiri Yayında kalmadıysa ileti kalıcı başarısızdır (AC-65; R9).</para>
///
/// <para><b>Okumalar tek atlamadır</b> (C12, **D11**): ilan özeti `VehicleCatalog`'dan (model
/// KİMLİĞİ döner), model adı `VehicleReference`'tan, şube ve galeri adı `Dealership`'ten.</para>
/// </summary>
internal sealed class NewsletterMessageComposer(
    NotificationsDbContext context,
    TemplateResolver templates,
    IListingSummaryQueries listings,
    IVehicleModelQueries models,
    IBranchContactQueries branches,
    IGalleryDisplayNameQueries gallery,
    UnsubscribeTokens unsubscribeTokens,
    ConfirmationTokens confirmationTokens,
    NewsletterOptions newsletterOptions,
    NotificationsOptions options)
{
    /// <summary>**D11** — bir bülten en çok bu kadar satır taşır.</summary>
    internal const int MaximumRows = 20;

    internal async Task<Composition> ComposeAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        NewsletterSubscription? subscription = message.SubscriptionId is Guid id
            ? await context.Subscriptions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            : null;

        if (subscription is null)
        {
            return Composition.Permanent(NotificationErrors.SubscriptionInactive);
        }

        return message.PeriodId is Guid periodId
            ? await ComposeNewsletterAsync(message, subscription, periodId, cancellationToken)
            : await ComposeConfirmationAsync(message, subscription, cancellationToken);
    }

    /// <summary>
    /// **D28** — anahtar gönderim kaydının kimliğinden TÜRETİLİR; burada üretilmez ve
    /// veritabanına yazılmaz. Her deneme aynı bağlantıyı taşır. Kaydın özeti abonelikteki özet
    /// değilse yerine yeni bir istek gelmiştir: bu ileti gönderilmez — geçersiz bir bağlantı
    /// taşırdı (**D7**).
    /// </summary>
    private async Task<Composition> ComposeConfirmationAsync(
        NotificationMessage message,
        NewsletterSubscription subscription,
        CancellationToken cancellationToken)
    {
        (string token, byte[] hash) = confirmationTokens.For(message.Id);

        if (subscription.Status != SubscriptionStatus.PendingConfirmation
            || subscription.ConfirmationTokenHash is null
            || !hash.AsSpan().SequenceEqual(subscription.ConfirmationTokenHash))
        {
            return Composition.Permanent(NotificationErrors.SubscriptionInactive);
        }

        Result<ResolvedTemplate> template = await templates.ResolveAsync(
            TemplateKeys.NewsletterConfirmation,
            LocaleFor(message.Locale),
            options.DefaultLocale,
            cancellationToken);

        if (template.IsFailure)
        {
            return Composition.Permanent(NotificationErrors.TemplateMissing);
        }

        string locale = template.Value.Locale;

        Dictionary<string, string> values = new(StringComparer.Ordinal)
        {
            ["teyitBaglantisi"] = Link(locale, "newsletter/confirm", token),
            ["galeriAdi"] = await GalleryNameAsync(locale, cancellationToken),
        };

        return Composition.Ready(new OutboundMessage(
            subscription.Email,
            TemplateRenderer.Render(template.Value.Subject, values),
            TemplateRenderer.Render(template.Value.Body, values)));
    }

    private async Task<Composition> ComposeNewsletterAsync(
        NotificationMessage message,
        NewsletterSubscription subscription,
        Guid periodId,
        CancellationToken cancellationToken)
    {
        // D22 — dağıtım ANINDAKİ durum: çıkmış (ya da çıkıp yeniden abone olmuş ama teyit
        // etmemiş) aboneye kuyrukta bekleyen bülten de gitmez (R20).
        if (subscription.Status != SubscriptionStatus.Active)
        {
            return Composition.Permanent(NotificationErrors.SubscriptionInactive);
        }

        List<NewsletterPeriodListing> period = await context.PeriodListings
            .AsNoTracking()
            .Where(item => item.PeriodId == periodId)
            .OrderByDescending(item => item.PublishedAt)
            .ThenBy(item => item.ListingId)
            .ToListAsync(cancellationToken);

        // D21 — değişken veri ve DURUM kaynaktan; küme dönemden.
        Dictionary<Guid, ListingSummaryDto> current = (await listings.FindPublishedAsync(
                [.. period.Select(item => item.ListingId)],
                cancellationToken))
            .ToDictionary(item => item.ListingId);

        List<ListingSummaryDto> rows = [.. period.Where(item => current.ContainsKey(item.ListingId)).Select(item => current[item.ListingId])];

        if (rows.Count == 0)
        {
            return Composition.Permanent(NotificationErrors.NewsletterEmpty);
        }

        // D31 — dil bir kez seçilir: başlık hangi dilde çözüldüyse parçalar da o dilde.
        Result<ResolvedTemplate> header = await templates.ResolveAsync(
            TemplateKeys.NewsletterHeader,
            LocaleFor(message.Locale),
            options.DefaultLocale,
            cancellationToken);

        if (header.IsFailure)
        {
            return Composition.Permanent(NotificationErrors.TemplateMissing);
        }

        string locale = header.Value.Locale;
        Dictionary<string, ResolvedTemplate> parts = [];

        foreach (string key in new[]
        {
            TemplateKeys.NewsletterRow,
            TemplateKeys.NewsletterFooter,
            TemplateKeys.NewsletterPriceOnRequest,
            TemplateKeys.NewsletterMore,
        })
        {
            Result<ResolvedTemplate> part = await templates.ResolveAsync(key, locale, options.DefaultLocale, cancellationToken);

            if (part.IsFailure)
            {
                return Composition.Permanent(NotificationErrors.TemplateMissing);
            }

            parts[key] = part.Value;
        }

        List<ListingSummaryDto> shown = [.. rows.Take(MaximumRows)];

        IReadOnlyDictionary<Guid, string> modelNames = await models.ResolveNamesAsync(
            [.. shown.Select(row => row.VehicleModelId).Distinct()],
            locale,
            cancellationToken);

        Dictionary<Guid, string> branchNames = [];

        foreach (Guid branchId in shown.Select(row => row.BranchId).Distinct())
        {
            BranchNotificationContactDto? branch = await branches.FindContactAsync(branchId, locale, cancellationToken);
            branchNames[branchId] = branch?.Name ?? string.Empty;
        }

        string galleryName = await GalleryNameAsync(locale, cancellationToken);
        List<string> blocks = [TemplateRenderer.Render(header.Value.Body, new Dictionary<string, string>(StringComparer.Ordinal) { ["galeriAdi"] = galleryName })];

        foreach (ListingSummaryDto row in shown)
        {
            blocks.Add(TemplateRenderer.Render(parts[TemplateKeys.NewsletterRow].Body, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["modelAdi"] = modelNames.TryGetValue(row.VehicleModelId, out string? name) ? name : string.Empty,
                ["modelYili"] = NewsletterFormat.Year(row.ModelYear, locale),
                ["km"] = NewsletterFormat.Mileage(row.MileageKilometers, locale),
                ["fiyat"] = Price(row, locale, parts[TemplateKeys.NewsletterPriceOnRequest].Body),
                ["subeAdi"] = branchNames[row.BranchId],
                ["ilanBaglantisi"] = Link(locale, "cars/" + Uri.EscapeDataString(row.Slug), null),
            }));
        }

        // D11 — 20'den fazla ilan varsa SAYI TAŞIMAYAN cümle ve vitrin listesinin bağlantısı.
        if (rows.Count > MaximumRows)
        {
            blocks.Add(TemplateRenderer.Render(parts[TemplateKeys.NewsletterMore].Body, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["vitrinBaglantisi"] = Link(locale, "cars", null),
            }));
        }

        blocks.Add(TemplateRenderer.Render(parts[TemplateKeys.NewsletterFooter].Body, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["galeriAdi"] = galleryName,
            ["cikisBaglantisi"] = Link(locale, "newsletter/unsubscribe", unsubscribeTokens.Create(subscription.Id)),
        }));

        return Composition.Ready(new OutboundMessage(
            subscription.Email,
            TemplateRenderer.Render(header.Value.Subject, new Dictionary<string, string>(StringComparer.Ordinal) { ["galeriAdi"] = galleryName }),
            string.Join("\n\n", blocks.Where(block => block.Length > 0))));
    }

    /// <summary>AC-75 — "Fiyat sorunuz" 0 DEĞİLDİR: tutar yoktur, şablon kataloğunun metni basılır.</summary>
    private static string Price(ListingSummaryDto row, string locale, string onRequest) =>
        row.Price.OnRequest || row.Price.Amount is null || row.Price.CurrencyCode is null
            ? onRequest
            : NewsletterFormat.Money(row.Price.Amount.Value, row.Price.CurrencyCode, row.Price.MinorUnitExponent ?? 0, locale);

    /// <summary>AC-82 — abonenin dili kurulumda artık etkin değilse varsayılan dil.</summary>
    private string LocaleFor(string locale) => newsletterOptions.IsEnabled(locale) ? locale : options.DefaultLocale;

    /// <summary>
    /// **D26** — mutlak bağlantı: <c>{PublicBaseUrl}/{locale}/{yol}</c>; anahtar sorgu
    /// parametresinde. Ön yüzün rotası bu kalıpla aynıdır (AC-51).
    /// </summary>
    private string Link(string locale, string path, string? token)
    {
        string relative = string.Create(CultureInfo.InvariantCulture, $"/{locale}/{path}");

        if (token is not null)
        {
            relative += "?token=" + Uri.EscapeDataString(token);
        }

        return new Uri(options.PublicBaseUrl, relative).ToString();
    }

    /// <summary>**D18** — galerinin abonenin dilindeki görünen adı; yoksa `0020`'nin düşüşü, o da yoksa boş (satır düşer).</summary>
    private async Task<string> GalleryNameAsync(string locale, CancellationToken cancellationToken) =>
        await gallery.FindDisplayNameAsync(locale, cancellationToken) ?? string.Empty;
}
