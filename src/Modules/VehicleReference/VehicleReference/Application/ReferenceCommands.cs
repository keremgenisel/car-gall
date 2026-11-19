using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using CarGallery.Modules.VehicleReference.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.VehicleReference.Application;

/// <summary>`0021` R5 — marka ya da modelin panelden gelen adları, ham hâliyle.</summary>
internal sealed record ReferenceNameInput(string? CanonicalName, IReadOnlyDictionary<string, string?>? DisplayNames);

/// <summary>
/// `0021` — kayıt hataları. Hata tek alana bağlanır (<c>field</c> parametresi) ve metin
/// taşımaz; panel kodu çevirir (C6).
/// </summary>
internal static class ReferenceErrors
{
    internal const string BrandNotFound = "brand.not_found";
    internal const string BrandNameTaken = "brand.name_taken";
    internal const string BrandSlugTaken = "brand.slug_taken";
    internal const string ModelNotFound = "vehicle_model.not_found";
    internal const string ModelNameTaken = "vehicle_model.name_taken";
    internal const string ModelBrandImmutable = "vehicle_model.brand_immutable";
    internal const string LocaleNotEnabled = "reference.locale_not_enabled";
    internal const string DisplayNameTooLong = "reference.display_name_too_long";

    internal const string CanonicalNameField = "canonicalName";
    internal const string BrandField = "brandId";

    internal static Error Field(string code, string field) => Error.With(code, "field", field);

    /// <summary>
    /// Görünen adlar: dil etkin olmalı, değer kırpılır, boş değer "ad yok" demektir (R3 —
    /// o dilde kanonik ad gösterilir). Hata varsa ilk hata döner.
    /// </summary>
    internal static Result<Dictionary<string, string?>> DisplayNames(
        IReadOnlyDictionary<string, string?>? raw,
        ReferenceLocales locales)
    {
        Dictionary<string, string?> names = new(StringComparer.Ordinal);

        foreach ((string code, string? value) in raw ?? new Dictionary<string, string?>())
        {
            string field = $"displayNames.{code}";

            if (!locales.IsEnabled(code))
            {
                return Result.Failure<Dictionary<string, string?>>(Field(LocaleNotEnabled, field));
            }

            string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            if (trimmed is { Length: > ReferenceNames.DisplayNameMaxLength })
            {
                return Result.Failure<Dictionary<string, string?>>(Field(DisplayNameTooLong, field));
            }

            names[code] = trimmed;
        }

        return Result.Success(names);
    }

    /// <summary>
    /// Yarışta iki istek aynı adı aynı anda ön denetimden geçirebilir; ikincisini tekil indeks
    /// düşürür (C3). İndeksin adı hangi kuralın çiğnendiğini söyler.
    /// </summary>
    internal static string? UniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            ? postgres.ConstraintName
            : null;
}

/// <summary>
/// `0021` R1, R5–R7 — markanın yazma yolu. Yalnız Yönetici (D4); politika uç grubunda.
/// Eski sürümle yazma <c>concurrency.lost</c> ile reddedilir (`0020` deseni).
/// </summary>
internal sealed class BrandCommands(VehicleReferenceDbContext context, ReferenceLocales locales)
{
    internal async Task<Result<(Guid Id, int Version)>> CreateAsync(
        ReferenceNameInput input,
        CancellationToken cancellationToken)
    {
        Result<string> canonical = ReferenceNames.Canonical(input.CanonicalName, "brand", ReferenceNames.BrandCanonicalMaxLength);

        if (canonical.IsFailure)
        {
            return Result.Failure<(Guid, int)>(ReferenceErrors.Field(canonical.Error.Code, ReferenceErrors.CanonicalNameField));
        }

        Result<Dictionary<string, string?>> names = ReferenceErrors.DisplayNames(input.DisplayNames, locales);

        if (names.IsFailure)
        {
            return Result.Failure<(Guid, int)>(names.Error);
        }

        string key = ReferenceNames.Key(canonical.Value);
        string slug = ReferenceNames.Slug(canonical.Value);

        if (await context.VehicleBrands.AnyAsync(brand => brand.CanonicalKey == key, cancellationToken))
        {
            return Result.Failure<(Guid, int)>(ReferenceErrors.Field(ReferenceErrors.BrandNameTaken, ReferenceErrors.CanonicalNameField));
        }

        if (await context.VehicleBrands.AnyAsync(brand => brand.Slug == slug, cancellationToken))
        {
            return Result.Failure<(Guid, int)>(ReferenceErrors.Field(ReferenceErrors.BrandSlugTaken, ReferenceErrors.CanonicalNameField));
        }

        VehicleBrand brand = new()
        {
            Id = Guid.CreateVersion7(),
            CanonicalName = canonical.Value,
            CanonicalKey = key,
            Slug = slug,
        };

        ApplyNames(brand.Translations, names.Value, brand.Id);
        context.VehicleBrands.Add(brand);

        Error? failure = await SaveAsync(cancellationToken);

        return failure is null
            ? Result.Success((brand.Id, brand.Version))
            : Result.Failure<(Guid, int)>(failure);
    }

    /// <summary>
    /// Kanonik ad düzeltilebilir; kısaltma DEĞİŞMEZ (D10, AC-12). Görünen adlar yalnız
    /// gönderilen dillerde değişir.
    /// </summary>
    internal async Task<Result<int>> UpdateAsync(
        Guid id,
        int version,
        ReferenceNameInput input,
        CancellationToken cancellationToken)
    {
        VehicleBrand? brand = await context.VehicleBrands
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (brand is null)
        {
            return Result.Failure<int>(new Error(ReferenceErrors.BrandNotFound));
        }

        if (brand.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        Result<string> canonical = ReferenceNames.Canonical(input.CanonicalName, "brand", ReferenceNames.BrandCanonicalMaxLength);

        if (canonical.IsFailure)
        {
            return Result.Failure<int>(ReferenceErrors.Field(canonical.Error.Code, ReferenceErrors.CanonicalNameField));
        }

        Result<Dictionary<string, string?>> names = ReferenceErrors.DisplayNames(input.DisplayNames, locales);

        if (names.IsFailure)
        {
            return Result.Failure<int>(names.Error);
        }

        string key = ReferenceNames.Key(canonical.Value);

        if (await context.VehicleBrands.AnyAsync(item => item.Id != id && item.CanonicalKey == key, cancellationToken))
        {
            return Result.Failure<int>(ReferenceErrors.Field(ReferenceErrors.BrandNameTaken, ReferenceErrors.CanonicalNameField));
        }

        brand.CanonicalName = canonical.Value;
        brand.CanonicalKey = key;
        ApplyNames(brand.Translations, names.Value, brand.Id);

        return await BumpAsync(brand, cancellationToken);
    }

    /// <summary>R6 — pasif yapma ve yeniden etkinleştirme; ilanlar ve araçlar etkilenmez (D5).</summary>
    internal Task<Result<int>> SetActiveAsync(Guid id, int version, bool active, CancellationToken cancellationToken) =>
        ChangeAsync(id, version, brand => brand.IsActive = active, cancellationToken);

    /// <summary>R7 — "ana sayfada göster" işareti (D7).</summary>
    internal Task<Result<int>> SetShowOnHomeAsync(Guid id, int version, bool showOnHome, CancellationToken cancellationToken) =>
        ChangeAsync(id, version, brand => brand.ShowOnHome = showOnHome, cancellationToken);

    private async Task<Result<int>> ChangeAsync(
        Guid id,
        int version,
        Action<VehicleBrand> change,
        CancellationToken cancellationToken)
    {
        VehicleBrand? brand = await context.VehicleBrands.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (brand is null)
        {
            return Result.Failure<int>(new Error(ReferenceErrors.BrandNotFound));
        }

        if (brand.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        change(brand);

        return await BumpAsync(brand, cancellationToken);
    }

    private async Task<Result<int>> BumpAsync(VehicleBrand brand, CancellationToken cancellationToken)
    {
        brand.Version++;

        Error? failure = await SaveAsync(cancellationToken);

        return failure is null ? Result.Success(brand.Version) : Result.Failure<int>(failure);
    }

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Error(ConcurrencyErrors.Lost);
        }
        catch (DbUpdateException exception) when (ReferenceErrors.UniqueViolation(exception) is { } constraint)
        {
            return ReferenceErrors.Field(
                constraint == VehicleBrandConfiguration.SlugIndex ? ReferenceErrors.BrandSlugTaken : ReferenceErrors.BrandNameTaken,
                ReferenceErrors.CanonicalNameField);
        }
    }

    internal static void ApplyNames(List<VehicleBrandTranslation> translations, Dictionary<string, string?> names, Guid brandId)
    {
        foreach ((string locale, string? name) in names)
        {
            VehicleBrandTranslation? existing = translations.Find(item => item.Locale == locale);

            if (name is null)
            {
                if (existing is not null)
                {
                    translations.Remove(existing);
                }
            }
            else if (existing is null)
            {
                translations.Add(new VehicleBrandTranslation { BrandId = brandId, Locale = locale, Name = name });
            }
            else
            {
                existing.Name = name;
            }
        }
    }
}

/// <summary>
/// `0021` R2, R5, R6 — modelin yazma yolu. Model bir markanın ALTINA eklenir ve markası bir
/// daha değişmez (D14, AC-20): aracın markası modelden türüyor, model taşınırsa geçmiş
/// araçların markası sessizce değişirdi.
/// </summary>
internal sealed class ModelCommands(VehicleReferenceDbContext context, ReferenceLocales locales)
{
    internal async Task<Result<(Guid Id, int Version)>> CreateAsync(
        Guid brandId,
        ReferenceNameInput input,
        CancellationToken cancellationToken)
    {
        if (!await context.VehicleBrands.AnyAsync(brand => brand.Id == brandId, cancellationToken))
        {
            return Result.Failure<(Guid, int)>(new Error(ReferenceErrors.BrandNotFound));
        }

        Result<string> canonical = ReferenceNames.Canonical(input.CanonicalName, "vehicle_model", ReferenceNames.ModelCanonicalMaxLength);

        if (canonical.IsFailure)
        {
            return Result.Failure<(Guid, int)>(ReferenceErrors.Field(canonical.Error.Code, ReferenceErrors.CanonicalNameField));
        }

        Result<Dictionary<string, string?>> names = ReferenceErrors.DisplayNames(input.DisplayNames, locales);

        if (names.IsFailure)
        {
            return Result.Failure<(Guid, int)>(names.Error);
        }

        string key = ReferenceNames.Key(canonical.Value);

        if (await context.VehicleModels.AnyAsync(model => model.BrandId == brandId && model.CanonicalKey == key, cancellationToken))
        {
            return Result.Failure<(Guid, int)>(ReferenceErrors.Field(ReferenceErrors.ModelNameTaken, ReferenceErrors.CanonicalNameField));
        }

        VehicleModel model = new()
        {
            Id = Guid.CreateVersion7(),
            BrandId = brandId,
            CanonicalName = canonical.Value,
            CanonicalKey = key,
        };

        ApplyNames(model.Translations, names.Value, model.Id);
        context.VehicleModels.Add(model);

        Error? failure = await SaveAsync(cancellationToken);

        return failure is null
            ? Result.Success((model.Id, model.Version))
            : Result.Failure<(Guid, int)>(failure);
    }

    /// <summary>
    /// <paramref name="brandId"/> gönderildiyse modelin markasıyla AYNI olmak zorundadır;
    /// farklıysa istek reddedilir ve model değişmez (AC-20).
    /// </summary>
    internal async Task<Result<int>> UpdateAsync(
        Guid id,
        int version,
        Guid? brandId,
        ReferenceNameInput input,
        CancellationToken cancellationToken)
    {
        VehicleModel? model = await context.VehicleModels
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (model is null)
        {
            return Result.Failure<int>(new Error(ReferenceErrors.ModelNotFound));
        }

        if (brandId is { } istenen && istenen != model.BrandId)
        {
            return Result.Failure<int>(ReferenceErrors.Field(ReferenceErrors.ModelBrandImmutable, ReferenceErrors.BrandField));
        }

        if (model.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        Result<string> canonical = ReferenceNames.Canonical(input.CanonicalName, "vehicle_model", ReferenceNames.ModelCanonicalMaxLength);

        if (canonical.IsFailure)
        {
            return Result.Failure<int>(ReferenceErrors.Field(canonical.Error.Code, ReferenceErrors.CanonicalNameField));
        }

        Result<Dictionary<string, string?>> names = ReferenceErrors.DisplayNames(input.DisplayNames, locales);

        if (names.IsFailure)
        {
            return Result.Failure<int>(names.Error);
        }

        string key = ReferenceNames.Key(canonical.Value);

        if (await context.VehicleModels.AnyAsync(
                item => item.Id != id && item.BrandId == model.BrandId && item.CanonicalKey == key,
                cancellationToken))
        {
            return Result.Failure<int>(ReferenceErrors.Field(ReferenceErrors.ModelNameTaken, ReferenceErrors.CanonicalNameField));
        }

        model.CanonicalName = canonical.Value;
        model.CanonicalKey = key;
        ApplyNames(model.Translations, names.Value, model.Id);

        return await BumpAsync(model, cancellationToken);
    }

    /// <summary>R6 — pasif model yeni seçim olarak sunulmaz; kayıtlı araçlar değişmez (AC-21).</summary>
    internal async Task<Result<int>> SetActiveAsync(Guid id, int version, bool active, CancellationToken cancellationToken)
    {
        VehicleModel? model = await context.VehicleModels.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (model is null)
        {
            return Result.Failure<int>(new Error(ReferenceErrors.ModelNotFound));
        }

        if (model.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        model.IsActive = active;

        return await BumpAsync(model, cancellationToken);
    }

    private async Task<Result<int>> BumpAsync(VehicleModel model, CancellationToken cancellationToken)
    {
        model.Version++;

        Error? failure = await SaveAsync(cancellationToken);

        return failure is null ? Result.Success(model.Version) : Result.Failure<int>(failure);
    }

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Error(ConcurrencyErrors.Lost);
        }
        catch (DbUpdateException exception) when (ReferenceErrors.UniqueViolation(exception) is not null)
        {
            return ReferenceErrors.Field(ReferenceErrors.ModelNameTaken, ReferenceErrors.CanonicalNameField);
        }
    }

    private static void ApplyNames(List<VehicleModelTranslation> translations, Dictionary<string, string?> names, Guid modelId)
    {
        foreach ((string locale, string? name) in names)
        {
            VehicleModelTranslation? existing = translations.Find(item => item.Locale == locale);

            if (name is null)
            {
                if (existing is not null)
                {
                    translations.Remove(existing);
                }
            }
            else if (existing is null)
            {
                translations.Add(new VehicleModelTranslation { VehicleModelId = modelId, Locale = locale, Name = name });
            }
            else
            {
                existing.Name = name;
            }
        }
    }
}
