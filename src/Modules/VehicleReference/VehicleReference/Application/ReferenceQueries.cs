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

/// <summary>
/// `0021` — modelin adı iki parçadır: marka ve model, ikisi de dil başına görünen ad ya da
/// kanonik ada düşüş (R3). Tam ad bu iki parçanın birleşimidir ve vitrinin bugünkü görünümü
/// ("Toyota Corolla") böyle korunur (D12, AC-5).
/// </summary>
internal sealed class VehicleModelQueries(VehicleReferenceDbContext context) : IVehicleModelQueries
{
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, VehicleModelPartsDto> parts =
            await ResolvePartsAsync(vehicleModelIds, locale, cancellationToken);

        return parts.ToDictionary(pair => pair.Key, pair => FullName(pair.Value.BrandLabel, pair.Value.ModelLabel));
    }

    /// <summary>
    /// Çevirilere HİÇ bakmaz: kanonik adlar markanın ve modelin kendi kolonlarındadır ve çeviri
    /// satırlarını okumak burada yalnız maliyet olurdu.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await context.VehicleModels
            .AsNoTracking()
            .Where(model => vehicleModelIds.Contains(model.Id))
            .Join(
                context.VehicleBrands,
                model => model.BrandId,
                brand => brand.Id,
                (model, brand) => new { model.Id, Brand = brand.CanonicalName, Model = model.CanonicalName })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => FullName(row.Brand, row.Model));
    }

    public async Task<IReadOnlyDictionary<Guid, VehicleModelPartsDto>> ResolvePartsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return new Dictionary<Guid, VehicleModelPartsDto>();
        }

        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .Where(model => vehicleModelIds.Contains(model.Id))
            .ToListAsync(cancellationToken);

        List<Guid> brandIds = [.. models.Select(model => model.BrandId).Distinct()];

        Dictionary<Guid, VehicleBrand> brands = await context.VehicleBrands
            .AsNoTracking()
            .Include(brand => brand.Translations)
            .Where(brand => brandIds.Contains(brand.Id))
            .ToDictionaryAsync(brand => brand.Id, cancellationToken);

        return models
            .Where(model => brands.ContainsKey(model.BrandId))
            .ToDictionary(
                model => model.Id,
                model => new VehicleModelPartsDto(
                    model.Id,
                    model.BrandId,
                    BrandLabel(brands[model.BrandId], locale),
                    ModelLabel(model, locale)));
    }

    public async Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .Where(model => model.IsActive
                && context.VehicleBrands.Any(brand => brand.Id == model.BrandId && brand.IsActive))
            .OrderBy(model => model.CanonicalName)
            .ToListAsync(cancellationToken);

        return [.. models.Select(model => new VehicleModelDto(model.Id, model.BrandId, ModelLabel(model, locale)))];
    }

    public async Task<bool?> FindSelectableAsync(Guid vehicleModelId, CancellationToken cancellationToken)
    {
        var row = await context.VehicleModels
            .AsNoTracking()
            .Where(model => model.Id == vehicleModelId)
            .Join(
                context.VehicleBrands,
                model => model.BrandId,
                brand => brand.Id,
                (model, brand) => new { Selectable = model.IsActive && brand.IsActive })
            .SingleOrDefaultAsync(cancellationToken);

        return row?.Selectable;
    }

    /// <summary>
    /// Tam ad marka ve model parçasının boşlukla birleşimidir. Bu bir cümle değil, bir özel
    /// addır ve bölünmüş kayıtların her dilde eski tam adı birebir üretmesi bu kurala bağlıdır
    /// (`0021` C4: sabit eşleme her dili "marka + boşluk + model" diye böldü).
    /// </summary>
    internal static string FullName(string brand, string model) => $"{brand} {model}";

    internal static string BrandLabel(VehicleBrand brand, string locale) =>
        brand.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name ?? brand.CanonicalName;

    internal static string ModelLabel(VehicleModel model, string locale) =>
        model.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name ?? model.CanonicalName;
}

internal sealed class VehicleBrandQueries(VehicleReferenceDbContext context) : IVehicleBrandQueries
{
    public async Task<IReadOnlyList<VehicleBrandDto>> GetActiveAsync(string locale, CancellationToken cancellationToken)
    {
        List<VehicleBrand> brands = await context.VehicleBrands
            .AsNoTracking()
            .Include(brand => brand.Translations)
            .Where(brand => brand.IsActive)
            .ToListAsync(cancellationToken);

        return [.. Alphabetical(brands).Select(brand => new VehicleBrandDto(brand.Id, brand.Slug, VehicleModelQueries.BrandLabel(brand, locale)))];
    }

    public async Task<IReadOnlyList<ShowcaseBrandDto>> GetBrandsOfModelsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return [];
        }

        List<VehicleBrand> brands = await context.VehicleBrands
            .AsNoTracking()
            .Include(brand => brand.Translations)
            .Where(brand => context.VehicleModels.Any(model =>
                model.BrandId == brand.Id && vehicleModelIds.Contains(model.Id)))
            .ToListAsync(cancellationToken);

        return [.. Alphabetical(brands).Select(brand => Showcase(brand, locale))];
    }

    public async Task<IReadOnlyList<ShowcaseBrandDto>> FindBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        string locale,
        CancellationToken cancellationToken)
    {
        if (slugs.Count == 0)
        {
            return [];
        }

        // Eşleşme tam ve harfe duyarlı (D20): `Toyota` bilinmeyen değerdir.
        List<VehicleBrand> brands = await context.VehicleBrands
            .AsNoTracking()
            .Include(brand => brand.Translations)
            .Where(brand => slugs.Contains(brand.Slug))
            .ToListAsync(cancellationToken);

        return [.. Alphabetical(brands).Select(brand => Showcase(brand, locale))];
    }

    public async Task<BrandModelSetDto> GetModelSetAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken)
    {
        if (slugs.Count == 0)
        {
            return new BrandModelSetDto([], []);
        }

        List<(Guid Id, string Slug)> brands = await context.VehicleBrands
            .AsNoTracking()
            .Where(brand => slugs.Contains(brand.Slug))
            .Select(brand => new ValueTuple<Guid, string>(brand.Id, brand.Slug))
            .ToListAsync(cancellationToken);

        List<Guid> brandIds = [.. brands.Select(brand => brand.Id)];

        List<Guid> modelIds = brandIds.Count == 0
            ? []
            : await context.VehicleModels
                .AsNoTracking()
                .Where(model => brandIds.Contains(model.BrandId))
                .Select(model => model.Id)
                .ToListAsync(cancellationToken);

        return new BrandModelSetDto([.. brands.Select(brand => brand.Slug)], modelIds);
    }

    /// <summary>
    /// D7 — kanonik ada göre alfabetik. Sıralama bellekte ve sıra-duyarsız ordinal: veritabanı
    /// harmanlaması kuruluma göre değişir (Türkçe harmanlamada "I" ile "i" farklı yere düşer)
    /// ve kanonik ad zaten ASCII'dir (C3).
    /// </summary>
    internal static IEnumerable<VehicleBrand> Alphabetical(IEnumerable<VehicleBrand> brands) =>
        brands.OrderBy(brand => brand.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(brand => brand.CanonicalName, StringComparer.Ordinal);

    private static ShowcaseBrandDto Showcase(VehicleBrand brand, string locale) =>
        new(brand.Slug, VehicleModelQueries.BrandLabel(brand, locale), brand.CanonicalName, brand.ShowOnHome);
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
