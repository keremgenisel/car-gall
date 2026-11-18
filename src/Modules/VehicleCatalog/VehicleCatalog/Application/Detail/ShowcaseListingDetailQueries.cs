using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Detail;

internal sealed class ShowcaseListingDetailQueries(
    VehicleCatalogDbContext context,
    IVehicleModelNameResolver modelNameResolver,
    IReferenceLabelResolver labelResolver,
    ShowcaseLocaleOptions localeOptions) : IShowcaseListingDetailQueries
{
    public async Task<ShowcaseListingDetailDto?> FindAsync(
        string slug,
        string locale,
        CancellationToken cancellationToken)
    {
        // E7 — TEK SORGU. Vitrin okuma modeli zaten yalnız görünür ilanları taşıyor
        // (0001'de böyle kuruldu): gizli ilanın satırı yok. Böylece "gizli" ile "hiç yok"
        // aynı kod yolundan geçer ve aralarında ölçülebilir bir süre farkı doğmaz (C13).
        // Önce bul–sonra görünür mü bak biçimindeki iki adımlı bir uygulama, var olan
        // gizli ilanı ölçülebilir biçimde yavaşlatır ve varlığını sızdırırdı.
        if (!ListingSlug.TryParse(slug, out ListingSlug parsed))
        {
            return null;
        }

        ShowcaseListing? row = await context.ShowcaseListings
            .AsNoTracking()
            .Include(listing => listing.OptionalValues)
            .SingleOrDefaultAsync(listing => listing.Slug == parsed.Value, cancellationToken);

        if (row is null)
        {
            return null;
        }

        Locale resolved = localeOptions.ResolveOrDefault(locale);

        IReadOnlyDictionary<Guid, string> modelNames =
            await modelNameResolver.ResolveAsync([row.VehicleModelId], resolved, cancellationToken);

        // `0016b` D14/D25 — galeri okuma modelinin kendi tablosundan; `Media`'ya gidilmez.
        List<ShowcaseListingMedia> yuvalar = await context.ShowcaseListingMedia
            .AsNoTracking()
            .Where(item => item.ListingId == row.ListingId && item.Status == ShowcaseMediaStatus.Ready)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);

        return new ShowcaseListingDetailDto(
            row.Slug,
            row.IsReserved,
            [.. yuvalar.Select(ToImage).OfType<ShowcaseImageDto>()],
            modelNames.TryGetValue(row.VehicleModelId, out string? name) ? name : string.Empty,
            row.ModelYear,
            row.MileageKilometers,
            ToPrice(row),
            ResolveLocalized(row.BranchNames, resolved),
            await BuildAttributesAsync(row, resolved, cancellationToken),
            await PaintChangeMapAsync(row, cancellationToken));
    }

    /// <summary>
    /// `0016b` C1 — harita yalnız alan ŞU AN açık ve public iken döner. Yansıtma zaten
    /// yalnız public alanı yazıyor; okuma tarafındaki ikinci eleme, alan ayarı değişip
    /// satır henüz yeniden yansıtılmamışken haritanın sızmasını kapatır. Değersizse
    /// <c>null</c> ve anahtar yazılmaz (D9).
    /// </summary>
    private async Task<IReadOnlyList<ShowcasePaintChangeDto>?> PaintChangeMapAsync(
        ShowcaseListing row,
        CancellationToken cancellationToken)
    {
        if (row.PaintChangeMap is not { Count: > 0 } harita)
        {
            return null;
        }

        bool acikVePublic = await context.VehicleFieldSettings
            .AsNoTracking()
            .AnyAsync(
                setting => setting.FieldKey == VehicleFieldKeys.PaintChangeMap
                    && setting.FormState != FieldFormState.Closed
                    && setting.PublicVisibility == FieldPublicVisibility.Visible,
                cancellationToken);

        return acikVePublic
            ? [.. harita.Select(item => new ShowcasePaintChangeDto(item.Part, item.State))]
            : null;
    }

    /// <summary>
    /// Hazır yuvanın üç türevi (D18). `display` yoksa görsel gösterilemez ve düşer; diğer
    /// iki türevden biri eksikse yerine `display` geçer — küçük kaynakta `Media` üç türevi de
    /// üretir, bu yalnız bozuk bir kümeye karşı korumadır.
    /// </summary>
    private static ShowcaseImageDto? ToImage(ShowcaseListingMedia yuva)
    {
        ShowcaseImageVariantDto? Turev(string ad) => yuva.Variants
            .Where(item => item.Variant == ad)
            .Select(item => new ShowcaseImageVariantDto(item.Url, item.Width, item.Height))
            .FirstOrDefault();

        ShowcaseImageVariantDto? display = Turev("display");

        return display is null
            ? null
            : new ShowcaseImageDto(Turev("thumbnail") ?? display, display, Turev("large") ?? display);
    }

    private async Task<IReadOnlyList<ShowcaseAttributeDto>> BuildAttributesAsync(
        ShowcaseListing row,
        Locale locale,
        CancellationToken cancellationToken)
    {
        (string Kind, string Slug)[] terms =
        [
            ("BodyType", row.BodyTypeSlug),
            ("FuelType", row.FuelTypeSlug),
            ("TransmissionType", row.TransmissionTypeSlug),
            ("DrivetrainType", row.DrivetrainTypeSlug),
            ("TrimLevel", row.TrimLevelSlug),
        ];

        IReadOnlyDictionary<(string, string), string> labels =
            await labelResolver.ResolveAsync(terms, locale, cancellationToken);

        List<ShowcaseAttributeDto> attributes = [];

        foreach ((string kind, string slug) in terms)
        {
            if (labels.TryGetValue((kind, slug), out string? label))
            {
                attributes.Add(new ShowcaseAttributeDto(ToKey(kind), label, label));
            }
        }

        // Renk taksonomi değil, serbest metin: etiketi çeviri kataloğundan, değeri veriden.
        string color = ResolveLocalized(row.ColorNames, locale);
        if (color.Length > 0)
        {
            attributes.Add(new ShowcaseAttributeDto("color", color, color));
        }

        // Yalnız okuma modelinde BULUNAN opsiyonel alanlar; kapalı/gizli olanlar zaten
        // buraya hiç yazılmadı (C4).
        foreach (ShowcaseListingOptionalValue value in row.OptionalValues.OrderBy(item => item.FieldKey))
        {
            string text = ResolveLocalized(value.Values, locale);
            if (text.Length > 0)
            {
                attributes.Add(new ShowcaseAttributeDto(value.FieldKey, text, text));
            }
        }

        return attributes;
    }

    private static string ToKey(string kind) => kind switch
    {
        "BodyType" => "body_type",
        "FuelType" => "fuel_type",
        "TransmissionType" => "transmission_type",
        "DrivetrainType" => "drivetrain_type",
        "TrimLevel" => "trim_level",
        _ => kind,
    };

    private static ShowcasePriceDto ToPrice(ShowcaseListing row) => row.PriceOnRequest
        ? ShowcasePriceDto.OnRequestPrice()
        : new ShowcasePriceDto(false, row.PriceAmount, row.PriceCurrencyCode, row.PriceMinorUnitExponent);

    private string ResolveLocalized(Dictionary<string, string> values, Locale locale)
    {
        if (values.TryGetValue(locale.Code, out string? value))
        {
            return value;
        }

        return values.TryGetValue(localeOptions.Default.Code, out string? fallback) ? fallback : string.Empty;
    }
}
