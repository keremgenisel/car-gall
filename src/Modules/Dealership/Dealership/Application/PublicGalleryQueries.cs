using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

internal sealed class PublicGalleryQueries(DealershipDbContext context, Locale defaultLocale, IClock? clock = null, DealershipMapOptions? maps = null)
    : IPublicGalleryQueries, IGalleryDisplayNameQueries, IGalleryBrandingQueries
{
    /// <summary>
    /// `0026` D17 · AC-76 — ad ve logo kimliği TEK projeksiyon sorgusunda; ticari unvan, vergi ve
    /// sicil numarası süreç belleğine inmez (`0020` AC-16).
    /// </summary>
    public async Task<GalleryBrandingDto?> FindBrandingAsync(string locale, CancellationToken cancellationToken)
    {
        var row = await context.Galleries
            .AsNoTracking()
            .Select(gallery => new { gallery.DisplayNames, gallery.LogoMediaAssetId })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new GalleryBrandingDto(row.DisplayNames.Resolve(Resolve(locale), defaultLocale), row.LogoMediaAssetId);
    }

    /// <summary>
    /// `0020` D16 — Host'un `index.html`'e bastığı ad. Projeksiyon sorguda: ticari unvan,
    /// vergi ve sicil numarası süreç belleğine hiç inmez (AC-16).
    /// </summary>
    public async Task<string?> FindDisplayNameAsync(string locale, CancellationToken cancellationToken)
    {
        var row = await context.Galleries
            .AsNoTracking()
            .Select(gallery => new { gallery.DisplayNames })
            .SingleOrDefaultAsync(cancellationToken);

        return row?.DisplayNames.Resolve(Resolve(locale), defaultLocale);
    }

    public async Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken) =>
        (await ReadAsync(locale, cancellationToken))?.Gallery;

    /// <summary>`0028` P1 — aynı okuma; koordinat yalnız Host'un iletişim belgesine (D13 `geo`) gider.</summary>
    public Task<ContactGalleryDto?> GetContactAsync(string locale, CancellationToken cancellationToken) =>
        ReadAsync(locale, cancellationToken);

    private async Task<ContactGalleryDto?> ReadAsync(string locale, CancellationToken cancellationToken)
    {
        // `0020` D15 — ticari unvan DEĞİL görünen ad (AC-13, AC-16).
        // `0024` D7 — sosyal hesaplar aynı okumada; vergi ve sicil numarası yine okunmaz.
        var gallery = await context.Galleries
            .AsNoTracking()
            .Select(item => new { item.DisplayNames, item.SocialLinks })
            .SingleOrDefaultAsync(cancellationToken);

        if (gallery is null)
        {
            return null;
        }

        Locale resolved = Resolve(locale);

        // AC-10 — ziyaretçinin dilinde yoksa varsayılan dildeki ad. O da yoksa (kurulum adımı
        // atlanmış bir veritabanı) boş: R5a "adın yeri boş kalır" diyor, unvana düşmek değil.
        string displayName = gallery.DisplayNames.Resolve(resolved, defaultLocale) ?? string.Empty;

        // AC-48 — süzgeç SORGUDA: geçici kapalı ve kapatılmış şube süreç belleğine hiç
        // inmez. Projeksiyon da sorguda (D15): WhatsApp ve slug okunmaz. `0028` D5 — e-posta ve
        // koordinat okunur; koordinat ziyaretçi DTO'suna girmez, yalnız harita adresinin sorgusudur.
        var rows = await context.Branches
            .AsNoTracking()
            .Where(branch => branch.Status == BranchStatus.Active)
            .Select(branch => new
            {
                branch.Names,
                branch.Addresses,
                branch.Phone,
                branch.Email,
                branch.Coordinate,
                branch.OpeningHours,
                branch.SpecialHours,
                branch.TimeZone,
            })
            .ToListAsync(cancellationToken);

        // `0027` D6 — "şimdi" yalnız saat soyutlamasından; her şube kendi saat diliminde (C2).
        // DI daima modülün `IClock`'unu verir; varsayılan yalnız saati sormayan doğrudan kurulumlar için.
        DateTimeOffset now = (clock ?? new SystemClock()).UtcNow;

        // D13 — varsayılan dildeki adın ORDINAL sırası: kültürden bağımsız ve deterministik.
        // Ziyaretçinin dilindeki ada göre sıralamak aynı şubeleri her dilde başka sırada gösterirdi.
        var ordered = rows
            .OrderBy(row => row.Names.Resolve(defaultLocale, defaultLocale) ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        List<PublicBranchDto> branches =
        [
            .. ordered.Select(row =>
            {
                (PublicOpenNowDto? openNow, IReadOnlyList<PublicSpecialDayDto> specialHours) =
                    BranchSchedule.ForVisitor(row.OpeningHours, row.SpecialHours, row.TimeZone, now);

                // `0028` D7 — koordinat yoksa VARSAYILAN dildeki adres, istek dili ne olursa olsun (AC-98).
                (string LinkUrl, string? EmbedUrl)? map = BranchMapAddress.Build(
                    row.Coordinate,
                    row.Addresses.Resolve(defaultLocale, defaultLocale) ?? string.Empty,
                    resolved.Code,
                    maps?.EmbedApiKey);

                return new PublicBranchDto(
                    // D8 — ziyaretçinin dilinde yoksa varsayılan dildeki metin (AC-44, AC-68).
                    // Varsayılan dildeki ad ve adres zorunlu (0003 AC-6), satır boş kalmaz.
                    row.Names.Resolve(resolved, defaultLocale) ?? string.Empty,
                    row.Addresses.Resolve(resolved, defaultLocale) ?? string.Empty,
                    row.Phone.Value,
                    // `0025` D8 — programsız şubede null; dilden bağımsız.
                    row.OpeningHours?.ToDto(),
                    // `0027` D8 — "şu an" ve penceredeki özel günler; saat dilimi yanıtta yok (C3).
                    openNow,
                    // `0028` D8 — şubenin bugünkü alanı (SS2); yoksa null.
                    row.Email?.Value,
                    map is { } urls ? new PublicBranchMapDto(urls.LinkUrl) { EmbedUrl = urls.EmbedUrl } : null)
                {
                    SpecialHours = specialHours,
                };
            }),
        ];

        // D3 — D1'de olmayan kodla saklanmış satır düşer; sıra D1'inkidir, girilme sırası değil.
        List<PublicSocialLinkDto> socialLinks =
        [
            .. SocialPlatform.InOrder(gallery.SocialLinks).Select(link => new PublicSocialLinkDto(link.Platform, link.Url)),
        ];

        List<PublicGeoDto?> geo =
        [
            .. ordered.Select(row => row.Coordinate is { } point
                ? new PublicGeoDto(Scaled(point.Latitude), Scaled(point.Longitude))
                : null),
        ];

        return new ContactGalleryDto(new PublicGalleryDto(displayName, branches, socialLinks), geo);
    }

    /// <summary>`0028` D13 — <c>decimal(18,8)</c>'in sondaki sıfırları atılır: JSON sayısı <c>40.98765</c>, <c>40.98765000</c> değil.</summary>
    private static decimal Scaled(decimal value) =>
        decimal.Parse(BranchMapAddress.Number(value), System.Globalization.CultureInfo.InvariantCulture);

    private Locale Resolve(string locale)
    {
        Result<Locale> requested = Locale.Create(locale);
        return requested.IsSuccess ? requested.Value : defaultLocale;
    }
}
