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

        return new ShowcaseListingDetailDto(
            row.Slug,
            row.IsReserved,
            row.CoverMediaUrl,
            modelNames.TryGetValue(row.VehicleModelId, out string? name) ? name : string.Empty,
            row.ModelYear,
            row.MileageKilometers,
            ToPrice(row),
            ResolveLocalized(row.BranchNames, resolved),
            await BuildAttributesAsync(row, resolved, cancellationToken));
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
