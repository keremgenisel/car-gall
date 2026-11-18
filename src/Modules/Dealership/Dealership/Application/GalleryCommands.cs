using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>`0020` R1 — panelin galeri profili girdisi; değerler ham hâliyle.</summary>
internal sealed record GalleryProfileInput(
    string? CommercialName,
    string? TaxNumber,
    string? TradeRegistryNumber,
    IReadOnlyDictionary<string, string?>? DisplayNames);

/// <summary>
/// `0020` R1–R3a, R16 — galeri profilinin yazma yolu. Eski sürümle kaydetme
/// <c>concurrency.lost</c> ile reddedilir (D7, D17 — `409`); okunan sürüm güncel olsa da
/// aynı sürümden paralel bir kaydetme önce yazdıysa sürüm kolonu ikincisini düşürür.
/// </summary>
internal sealed class GalleryCommands(DealershipDbContext context, DealershipLocales locales)
{
    internal async Task<Result<int>> UpdateProfileAsync(
        int version,
        GalleryProfileInput input,
        CancellationToken cancellationToken)
    {
        Gallery? gallery = await context.Galleries.SingleOrDefaultAsync(cancellationToken);

        if (gallery is null)
        {
            return Result.Failure<int>(new Error(Gallery.NotFound));
        }

        if (gallery.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        FieldErrors dilHatalari = new();
        Dictionary<Locale, string?> adlar = locales.Filter(input.DisplayNames, "displayName", dilHatalari);

        if (dilHatalari.Any)
        {
            return Result.Failure<int>(dilHatalari.ToError());
        }

        Result sonuc = gallery.UpdateProfile(
            input.CommercialName,
            input.TaxNumber,
            input.TradeRegistryNumber,
            adlar,
            locales.Default);

        if (sonuc.IsFailure)
        {
            return Result.Failure<int>(sonuc.Error);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        return Result.Success(gallery.Version);
    }
}
