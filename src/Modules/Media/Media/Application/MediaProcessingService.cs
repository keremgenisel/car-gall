using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// İşleme hattının bir turu (D1, D14). R3, R4, R5, R7, R8 burada buluşuyor.
///
/// <para><b>Tur varlığı durum geçişiyle SAHİPLENİR ve bu kendi transaction'ındadır</b>
/// (D14/AC-12): sahiplenme kaydedilmeden iş başlamaz, bu yüzden ikinci bir tur aynı varlığı
/// artık <c>Yüklendi</c> görmez. Ayrı bir kilit mekanizması gerekmiyor — R6'nın durumu
/// kilidin kendisi.</para>
///
/// <para><b>Duyuru iş verisiyle AYNI `SaveChanges`'te yazılıyor</b> (C4/AC-23): ayrı
/// düşerlerse ya duyurulmayan bir hazır varlık ya da olmayan bir varlığın duyurusu kalır.</para>
/// </summary>
internal sealed class MediaProcessingService(
    MediaDbContext context,
    IObjectStore store,
    IImageProcessor processor,
    MediaUrlResolver urls,
    OutboxWriter<MediaDbContext> outbox,
    MediaOptions options,
    IClock clock,
    ILogger<MediaProcessingService> logger)
{
    /// <summary>
    /// Bir turda en fazla kaç varlık işlenir. `OutboxDispatchOptions.BatchSize` ile aynı
    /// gerekçe: turun BİR SINIRI olmalı.
    ///
    /// <para><b>Bu sınır bir sondayla bulundu.</b> Sahiplenme kilidini kasten bozup turu
    /// koşturduğumda döngü hiç bitmedi: sorgu her seferinde aynı varlığı veriyordu ve
    /// `while` koşulu yalnız iptale bakıyordu. Kilit bugün o döngüyü kapatıyor — ama
    /// döngünün kapanması kilidin doğruluğuna BAĞLI olmamalı. Hattın kendi sınırı var.</para>
    ///
    /// <para>Görüntü işleme CPU-yoğun: yirmi görsel bir turda zaten saniyeler sürüyor ve
    /// kalanlar beş saniye sonraki turda alınıyor.</para>
    /// </summary>
    private const int BatchSize = 20;

    /// <summary>İşlenen varlık sayısı. Sıfır, yapılacak iş olmadığı anlamına gelir.</summary>
    internal async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        int islenen = 0;

        while (islenen < BatchSize && !cancellationToken.IsCancellationRequested)
        {
            MediaAsset? asset = await SahiplenAsync(cancellationToken);

            if (asset is null)
            {
                break;
            }

            await IsleAsync(asset, cancellationToken);
            islenen++;
        }

        return islenen;
    }

    /// <summary>
    /// D14 — en eski bekleyen varlığı alır ve <c>İşleniyor</c>'a geçirir. Geçiş kendi
    /// `SaveChanges`'inde kalıcı olur; işin kendisi ondan sonra başlar.
    /// </summary>
    private async Task<MediaAsset?> SahiplenAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset simdi = clock.UtcNow;

        MediaAsset? aday = await context.Assets
            .Where(asset => asset.Status == MediaProcessingStatus.Uploaded
                && asset.NextAttemptAt <= simdi)
            .OrderBy(asset => asset.NextAttemptAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (aday is null)
        {
            return null;
        }

        Result sahiplenme = aday.BeginProcessing(clock.UtcNow);

        if (sahiplenme.IsFailure)
        {
            return null;
        }

        await context.SaveChangesAsync(cancellationToken);

        return aday;
    }

    private async Task IsleAsync(MediaAsset asset, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RenderedDerivative>> uretim = await UretAsync(asset, cancellationToken);

        if (uretim.IsFailure)
        {
            await BasarisizAsync(asset, uretim.Error.Code, cancellationToken);
            return;
        }

        List<MediaDerivative> turevler = [];

        foreach (RenderedDerivative uretilen in uretim.Value)
        {
            MediaVariant? variant = MediaVariant.Find(uretilen.VariantCode);

            if (variant is null)
            {
                await BasarisizAsync(asset, MediaErrors.Undecodable, cancellationToken);
                return;
            }

            string anahtar = MediaObjectKeys.Derivative(asset.Id, variant);

            try
            {
                using MemoryStream govde = new(uretilen.Content, writable: false);
                await store.PutAsync(anahtar, govde, "image/webp", cancellationToken);
            }
#pragma warning disable CA1031 // Depolama hatası BEKLENEN bir tur hatasıdır; varlık yeniden denenir.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Türev yazılamadı: {AssetId} {Variant}", asset.Id, variant.Code);
                await BasarisizAsync(asset, MediaErrors.StorageUnavailable, cancellationToken);
                return;
            }

            turevler.Add(new MediaDerivative
            {
                VariantCode = variant.Code,
                Width = uretilen.Width,
                Height = uretilen.Height,
                ObjectKey = anahtar,
            });
        }

        // D11 — duyurunun `Url`'i GÖSTERİM türevidir (800 px), en büyüğü değil.
        //
        // Kontrol `MarkReady`'den ÖNCE ve bu bir sıra hatasının bedeliydi: sonraya
        // koyduğumda varlık zaten `Hazır` olduğu için `BasarisizAsync`'in durum geçişi
        // reddediliyordu ve varlık HAZIR ama DUYURUSUZ kalıyordu — R7 sessizce
        // karşılanmaz olurdu.
        MediaDerivative? gosterim = turevler
            .SingleOrDefault(turev => turev.VariantCode == MediaVariant.Display.Code);

        if (gosterim is null)
        {
            logger.LogError("Gösterim türevi üretilmedi: {AssetId}", asset.Id);
            await BasarisizAsync(asset, MediaErrors.Undecodable, cancellationToken);
            return;
        }

        Result hazir = asset.MarkReady(turevler, clock.UtcNow);

        if (hazir.IsFailure)
        {
            logger.LogError("Varlık hazır işaretlenemedi: {AssetId} {Error}", asset.Id, hazir.Error.Code);
            return;
        }

        outbox.Write(new MediaAssetReady(
            Guid.CreateVersion7(),
            clock.UtcNow,
            asset.Id,
            urls.Resolve(gosterim.ObjectKey).ToString()));

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<IReadOnlyList<RenderedDerivative>>> UretAsync(
        MediaAsset asset,
        CancellationToken cancellationToken)
    {
        byte[]? kaynak = await IndirAsync(asset.OriginalObjectKey, cancellationToken);

        if (kaynak is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.StorageUnavailable));
        }

        // AC-18 — filigran kaynağı erişilemezse varlık BAŞARISIZ olur. Filigransız türev
        // üretmek, galeri sahibinin satın aldığı tek garantiyi sessizce iptal etmekti ve
        // sessizliği fotoğraflar rakip sitede göründüğünde fark edilirdi.
        byte[]? filigran = await IndirAsync(options.WatermarkObjectKey, cancellationToken);

        if (filigran is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.WatermarkUnavailable));
        }

        return processor.Render(kaynak, filigran);
    }

    private async Task<byte[]?> IndirAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await using Stream? akis = await store.OpenReadAsync(objectKey, cancellationToken);

            if (akis is null)
            {
                return null;
            }

            using MemoryStream tampon = new();
            await akis.CopyToAsync(tampon, cancellationToken);

            return tampon.ToArray();
        }
#pragma warning disable CA1031 // Depolama hatası BEKLENEN bir tur hatasıdır.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogError(exception, "Nesne okunamadı: {ObjectKey}", objectKey);
            return null;
        }
    }

    /// <summary>
    /// Deneme hakkı kaldıysa varlık <c>Yüklendi</c>'ye döner (M1); tükendiyse
    /// <c>Başarısız</c> olur ve <c>MediaAssetFailed</c> yayılır (R8/AC-25).
    /// </summary>
    private async Task BasarisizAsync(MediaAsset asset, string reason, CancellationToken cancellationToken)
    {
        if (asset.HasAttemptsLeft(options.MaximumAttempts))
        {
            Result yeniden = asset.Reschedule(reason, clock.UtcNow, options.RetryBaseDelay);

            if (yeniden.IsFailure)
            {
                logger.LogError("Varlık yeniden kuyruğa alınamadı: {AssetId}", asset.Id);
                return;
            }

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        Result basarisiz = asset.MarkFailed(reason, clock.UtcNow);

        if (basarisiz.IsFailure)
        {
            logger.LogError("Varlık başarısız işaretlenemedi: {AssetId}", asset.Id);
            return;
        }

        outbox.Write(new MediaAssetFailed(Guid.CreateVersion7(), clock.UtcNow, asset.Id, reason));

        await context.SaveChangesAsync(cancellationToken);
    }
}
