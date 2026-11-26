using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0020` R1 — panelin galeri profili girdisi; değerler ham hâliyle. `0024` **D4** · P1 —
/// <c>SocialLinks</c> <c>null</c> ise (gövdede alan yok ya da açık <c>null</c>) kayıtlı hesaplar
/// değişmez; harita kümeyi bütünüyle yerine koyar.
/// </summary>
internal sealed record GalleryProfileInput(
    string? CommercialName,
    string? TaxNumber,
    string? TradeRegistryNumber,
    IReadOnlyDictionary<string, string?>? DisplayNames,
    IReadOnlyDictionary<string, string?>? SocialLinks = null);

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

        locales.KeepDisabled(adlar, gallery.DisplayNames);

        Result sonuc = gallery.UpdateProfile(
            input.CommercialName,
            input.TaxNumber,
            input.TradeRegistryNumber,
            adlar,
            locales.Default,
            input.SocialLinks);

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

    /// <summary>`0026` D8 · D9 — logo ataması; eski sürüm `409` (AC-12), sürüm bir artar (AC-11).</summary>
    internal Task<Result<int>> AssignLogoAsync(int version, Guid mediaAssetId, CancellationToken cancellationToken) =>
        mediaAssetId == Guid.Empty
            ? Task.FromResult(Result.Failure<int>(FieldErrors.Single("mediaAssetId", Gallery.LogoMediaAssetRequired)))
            : LogoAsync(version, gallery => gallery.AssignLogo(mediaAssetId), cancellationToken);

    /// <summary>`0026` D8 · D9 — logo kaldırma; bağ silinir, varlık durur (AC-13).</summary>
    internal Task<Result<int>> RemoveLogoAsync(int version, CancellationToken cancellationToken) =>
        LogoAsync(version, gallery => gallery.RemoveLogo(), cancellationToken);

    private async Task<Result<int>> LogoAsync(int version, Action<Gallery> change, CancellationToken cancellationToken)
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

        change(gallery);

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
