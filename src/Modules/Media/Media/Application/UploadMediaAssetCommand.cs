using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;

namespace CarGallery.Modules.Media.Application;

internal sealed record UploadMediaAssetResult(Guid Id, MediaProcessingStatus Status);

/// <summary>
/// R1/R2/R11 — yükleme yolu. D9'un sırası: doğrula → orijinali depoya yaz → kaydı
/// <c>Yüklendi</c> aç.
///
/// <para><b>Orijinal İSTEK İÇİNDE yazılıyor</b> ve bu D9'un kararı: işleme asenkron olduğu
/// için (D1) yanıt dönerken dosyanın dayanıklı bir yerde olması gerekir. Baytları
/// Postgres'te tutup sonra taşımak aynı dosyayı iki kez saklardı.</para>
///
/// <para><b>Reddedilen dosya varlık kaydı OLUŞTURMAZ</b> (R11): üç doğrulamanın hepsi
/// depoya yazmadan önce koşuyor. Sırası tersine dönerse kovada sahipsiz dosyalar birikir
/// ve hiçbir kayıt onlara işaret etmez.</para>
/// </summary>
internal sealed class UploadMediaAssetCommand(
    MediaDbContext context,
    IObjectStore store,
    IImageProcessor processor,
    MediaOptions options,
    IClock clock)
{
    internal async Task<Result<UploadMediaAssetResult>> ExecuteAsync(
        byte[] content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        // D10(b) — bayt sınırı İLK. En ucuz kontrol en önde: büyük bir dosyayı çözmeye
        // kalkıp sonra "zaten çok büyüktü" demek, tam olarak kaçınmak istediğimiz CPU'yu
        // harcamak demek.
        if (content.Length > options.MaximumByteSize)
        {
            return Result.Failure<UploadMediaAssetResult>(Error.With(
                MediaErrors.TooLarge, "limit", options.MaximumByteSize.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        // D10(a) — tür İÇERİKTEN. Bildirilen `Content-Type` istemcinin denetimindedir.
        int sniffLength = Math.Min(ImageFormatDetection.SniffLength, content.Length);
        DetectedImageFormat format = ImageFormatDetection.Detect(content.AsSpan(0, sniffLength));

        if (!ImageFormatDetection.IsAccepted(format))
        {
            return Result.Failure<UploadMediaAssetResult>(Error.With(
                MediaErrors.UnsupportedType, "detected", format.ToString().ToLowerInvariant()));
        }

        // D10(c) — piksel bütçesi. Bayt sınırı bunu GÖRMEZ.
        Result<ImageMeasurement> measured = processor.Measure(content);

        if (measured.IsFailure)
        {
            return Result.Failure<UploadMediaAssetResult>(measured.Error);
        }

        long pixels = (long)measured.Value.Width * measured.Value.Height;

        if (pixels > options.MaximumPixelCount)
        {
            return Result.Failure<UploadMediaAssetResult>(Error.With(
                MediaErrors.PixelBudgetExceeded, "pixels", pixels.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        // Guid v7: sıralanabilir ve uygulama tarafında üretilir (`docs/conventions.md` § 9).
        Guid id = Guid.CreateVersion7();
        string objectKey = MediaObjectKeys.Original(id, ImageFormatDetection.Extension(format));

        using (MemoryStream govde = new(content, writable: false))
        {
            await store.PutAsync(
                objectKey, govde, ImageFormatDetection.ContentType(format), cancellationToken);
        }

        MediaAsset asset = MediaAsset.Create(
            id,
            ImageFormatDetection.ContentType(format),
            objectKey,
            content.Length,
            measured.Value.Width,
            measured.Value.Height,
            clock.UtcNow);

        context.Assets.Add(asset);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new UploadMediaAssetResult(asset.Id, asset.Status));
    }
}
