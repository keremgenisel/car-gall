using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleReference.Application;

internal sealed class BodyTypeQueries(VehicleReferenceDbContext context) : IBodyTypeQueries
{
    public async Task<IReadOnlyList<BodyTypeDto>> GetAllAsync(string locale, CancellationToken cancellationToken)
    {
        List<BodyType> types = await context.BodyTypes
            .AsNoTracking()
            .Include(type => type.Translations)
            .OrderBy(type => type.Slug)
            .ToListAsync(cancellationToken);

        return
        [
            .. types.Select(type => new BodyTypeDto(
                type.Slug,
                // Referans veride çeviri zorunludur; eksikse kanonik Latin ada düşülür
                // ve ham çeviri anahtarı ASLA gösterilmez (AC-29).
                type.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                    ?? type.CanonicalName))
        ];
    }
}

internal sealed class VehicleModelQueries(VehicleReferenceDbContext context) : IVehicleModelQueries
{
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .Where(model => vehicleModelIds.Contains(model.Id))
            .ToListAsync(cancellationToken);

        return models.ToDictionary(
            model => model.Id,
            model => model.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                ?? model.CanonicalName);
    }

    /// <summary>
    /// Çevirilere HİÇ bakmaz. `Include` da yapmaz: kanonik ad modelin kendi kolonundadır
    /// ve çeviri satırlarını okumak burada yalnız maliyet olurdu.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Where(model => vehicleModelIds.Contains(model.Id))
            .ToListAsync(cancellationToken);

        return models.ToDictionary(model => model.Id, model => model.CanonicalName);
    }
}

internal sealed class ReferenceTermQueries(VehicleReferenceDbContext context) : IReferenceTermQueries
{
    public async Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveLabelsAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        string locale,
        CancellationToken cancellationToken)
    {
        if (terms.Count == 0)
        {
            return new Dictionary<(string, string), string>();
        }

        HashSet<string> sluglar = [.. terms.Select(term => term.Slug)];

        List<ReferenceTerm> kayitlar = await context.ReferenceTerms
            .AsNoTracking()
            .Include(term => term.Translations)
            .Where(term => sluglar.Contains(term.Slug))
            .ToListAsync(cancellationToken);

        Dictionary<(string, string), string> sonuc = [];

        foreach (ReferenceTerm kayit in kayitlar)
        {
            string kind = kayit.Kind.ToString();
            if (!terms.Contains((kind, kayit.Slug)))
            {
                continue;
            }

            // Çevirisi olmayan locale'de kanonik Latin ada düşülür; ham anahtar dönmez.
            sonuc[(kind, kayit.Slug)] =
                kayit.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                ?? kayit.CanonicalName;
        }

        return sonuc;
    }
}
