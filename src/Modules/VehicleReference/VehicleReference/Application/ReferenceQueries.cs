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

    public Task<bool> ExistsAsync(string slug, CancellationToken cancellationToken) =>
        context.BodyTypes.AsNoTracking().AnyAsync(type => type.Slug == slug, cancellationToken);
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

    /// <summary>
    /// Sıralama KANONİK ada göre, etikete göre değil: etiket locale'e göre değişir ve
    /// personelin dili değiştiğinde listenin sırası da değişirdi. Kanonik ad dilden
    /// bağımsızdır (docs/domain.md, Kanonik Ad).
    /// </summary>
    public async Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .OrderBy(model => model.CanonicalName)
            .ToListAsync(cancellationToken);

        return
        [
            .. models.Select(model => new VehicleModelDto(
                model.Id,
                model.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                    ?? model.CanonicalName))
        ];
    }

    public Task<bool> ExistsAsync(Guid vehicleModelId, CancellationToken cancellationToken) =>
        context.VehicleModels.AsNoTracking().AnyAsync(model => model.Id == vehicleModelId, cancellationToken);
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

    public async Task<IReadOnlyList<ReferenceTermDto>> GetByKindAsync(
        string kind,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(kind, out ReferenceTermKind tur))
        {
            return [];
        }

        List<ReferenceTerm> terimler = await context.ReferenceTerms
            .AsNoTracking()
            .Include(term => term.Translations)
            .Where(term => term.Kind == tur)
            .OrderBy(term => term.CanonicalName)
            .ToListAsync(cancellationToken);

        return
        [
            .. terimler.Select(term => new ReferenceTermDto(
                kind,
                term.Slug,
                term.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                    ?? term.CanonicalName))
        ];
    }

    /// <summary>
    /// Tek sorguda bütün çiftleri çeker ve EKSİK olanları döner. Çift başına sorgu atmak
    /// altı alan için altı gidiş gelişti; doğrulama kayıt yolunun içinde ve orada hız
    /// doğrudan personelin beklediği süredir.
    /// </summary>
    public async Task<IReadOnlyList<(string Kind, string Slug)>> FindMissingAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        CancellationToken cancellationToken)
    {
        if (terms.Count == 0)
        {
            return [];
        }

        HashSet<string> sluglar = [.. terms.Select(term => term.Slug)];

        List<(ReferenceTermKind Kind, string Slug)> mevcut = await context.ReferenceTerms
            .AsNoTracking()
            .Where(term => sluglar.Contains(term.Slug))
            .Select(term => new ValueTuple<ReferenceTermKind, string>(term.Kind, term.Slug))
            .ToListAsync(cancellationToken);

        HashSet<(string, string)> bulunan = [.. mevcut.Select(item => (item.Kind.ToString(), item.Slug))];

        return [.. terms.Where(term => !bulunan.Contains((term.Kind, term.Slug)))];
    }
}
