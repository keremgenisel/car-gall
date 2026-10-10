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

        // Filigran TUR BAŞINA bir kez indiriliyor, varlık başına değil. Önceki hâli
        // `UretAsync` içindeydi ve 20'lik bir partide aynı nesneyi 20 kez çekiyordu.
        //
        // Süreç içi önbellek bilinçli olarak YOK: D4 logo değiştiğinde yapılandırmanın
        // güncellendiğini söylüyor ve kalıcı bir önbellek o değişimi bir yeniden başlatmaya
        // kadar görmezdi.
        //
        // `null` ise tur yine koşuyor: AC-18 gereği sahiplenilen her varlık
        // `media.watermark_unavailable` ile BAŞARISIZ işaretlenmeli — sessizce bekleyen bir
        // varlık, R8'in önlemeye çalıştığı şeyin ta kendisi.
        byte[]? filigran = await IndirAsync(options.WatermarkObjectKey, cancellationToken);

        while (islenen < BatchSize && !cancellationToken.IsCancellationRequested)
        {
            MediaAsset? asset = await SahiplenAsync(cancellationToken);

            if (asset is null)
            {
                break;
            }

            await IsleAsync(asset, filigran, cancellationToken);
            islenen++;
        }

        return islenen;
    }

    /// <summary>
    /// D14 — turun varlığı SAHİPLENMESİ. Geçiş eşzamanlılık belirteci altında kaydedilir;
    /// aynı satırı başka bir tur çoktan sahiplendiyse <c>SaveChanges</c> düşer ve bu tur
    /// SIRADAKİ varlığa geçer.
    ///
    /// <para><b>Kilit bir sondayla eklendi.</b> Önceki hâli çıplak bir <c>SELECT</c> +
    /// <c>UPDATE</c> çiftiydi ve D14'ün *"geçişi işin başına koymak R6'nın durumunu aynı
    /// zamanda bir kilit yapıyor"* iddiası iki turun okuması kesiştiğinde tutmuyordu:
    /// ikisi de sahiplendi, <c>attempt_count</c> kaybolan güncellemeyle <b>1</b>'de kaldı
    /// (oysa o sayaç AC-11'in ölçütü) ve gerçekten paralel koşturulduğunda ikisi de tam
    /// işi yaptı — çözme, üç türev, filigran, S3'e yazma — sonra ikincisi
    /// <c>PK_asset_derivatives</c> çakışmasına düştü. Tek Host örneğiyle görünmez; ikinci
    /// örnek açıldığı gün belirtisi "bazı fotoğraflar geç hazır oluyor" olurdu.</para>
    ///
    /// <para><b>Neden satır kilidi değil:</b> `0007` R3 satır kilidini yalnız
    /// `BuildingBlocks/Concurrency/` altına koyuyor ve bir mimari test (`0007`/AC-9)
    /// modüllerde kilit sözdizimini tarıyor. İlk düzeltmemde ham SQL yazdım ve o test beni
    /// kırdı — haklıydı: ortak desen tek yerde yaşar. `InvariantGuard` de bu iş için uygun
    /// değil: o "satır kümesini kilitle, SAY, karar ver" içindir; buradaki ihtiyaç bir
    /// kuyruktan bir satır kapmak. Eşzamanlılık belirteci aynı garantiyi ortak deseni
    /// delmeden veriyor.</para>
    ///
    /// <para>Çakışan tur <b>durmuyor, atlıyor</b>: döngü bir sonraki adayı deniyor ve
    /// partinin geri kalanı bir sonraki tike ertelenmiyor.</para>
    /// </summary>
    private async Task<MediaAsset?> SahiplenAsync(CancellationToken cancellationToken)
    {
        // Deneme sayısı parti boyuyla sınırlı: her çakışma bir adayı tüketiyor ve
        // sınırsız bir döngü, kuyruk kalabalıkken turu burada asılı bırakırdı.
        for (int deneme = 0; deneme < BatchSize; deneme++)
        {
            DateTimeOffset simdi = clock.UtcNow;

            // Sıra YÜKLEME anına göre: geri çekilme aralığı kalktığı için (AC-15)
            // "sırası gelmiş" diye ayrı bir zaman alanı yok — ilk gelen ilk işlenir.
            MediaAsset? aday = await context.Assets
                .Where(asset => asset.Status == MediaProcessingStatus.Uploaded)
                .OrderBy(asset => asset.UploadedAt)
                .ThenBy(asset => asset.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (aday is null)
            {
                return null;
            }

            Result sahiplenme = aday.BeginProcessing(simdi);

            if (sahiplenme.IsFailure)
            {
                return null;
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);

                return aday;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Başka bir tur bu satırı çoktan sahiplendi. İstisna akış kontrolü DEĞİL:
                // sağlayıcının sinyali burada, sınırda, "sıradakine geç" kararına
                // çevriliyor (`docs/conventions.md` § 5 — `InvariantGuard`'ın aynı deseni).
                //
                // Takipçi temizlenmeli: aksi hâlde aynı `DbContext` bir sonraki turda o
                // bayat nesneyi yeniden yazmaya kalkar.
                context.ChangeTracker.Clear();
            }
        }

        return null;
    }

    private async Task IsleAsync(
        MediaAsset asset,
        byte[]? watermark,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RenderedDerivative>> uretim =
            await UretAsync(asset, watermark, cancellationToken);

        if (uretim.IsFailure)
        {
            await BasarisizAsync(asset, ReasonFor(uretim.Error.Code), cancellationToken);
            return;
        }

        List<MediaDerivative> turevler = [];

        foreach (RenderedDerivative uretilen in uretim.Value)
        {
            MediaVariant? variant = MediaVariant.Find(uretilen.VariantCode);

            if (variant is null)
            {
                await BasarisizAsync(asset, MediaFailureReason.Undecodable, cancellationToken);
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
                await BasarisizAsync(asset, MediaFailureReason.StorageUnavailable, cancellationToken);
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

        // AC-29 — duyuru türev kümesini taşıyor. Gösterim türevinin varlığı yine
        // kontrol ediliyor: `VehicleCatalog` kapak görselini onun adıyla alıyor ve küme
        // onsuz gelirse alıcı sessizce hiçbir şey yazmazdı.
        //
        // Kontrol `MarkReady`'den ÖNCE ve bu bir sıra hatasının bedeliydi: sonraya
        // koyduğumda varlık zaten `hazır` olduğu için `BasarisizAsync`'in durum geçişi
        // reddediliyordu ve varlık HAZIR ama DUYURUSUZ kalıyordu — R7 sessizce
        // karşılanmaz olurdu.
        if (!turevler.Exists(turev => turev.VariantCode == MediaVariant.Display.Code))
        {
            logger.LogError("Gösterim türevi üretilmedi: {AssetId}", asset.Id);
            await BasarisizAsync(asset, MediaFailureReason.Undecodable, cancellationToken);
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
            [.. turevler.Select(turev => new MediaDerivativePayload(
                turev.VariantCode,
                urls.Resolve(turev.ObjectKey).ToString(),
                turev.Width,
                turev.Height))]));

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<IReadOnlyList<RenderedDerivative>>> UretAsync(
        MediaAsset asset,
        byte[]? watermark,
        CancellationToken cancellationToken)
    {
        // AC-18 — filigran kaynağı erişilemezse varlık BAŞARISIZ olur. Filigransız türev
        // üretmek, galeri sahibinin satın aldığı tek garantiyi sessizce iptal etmekti ve
        // sessizliği fotoğraflar rakip sitede göründüğünde fark edilirdi.
        //
        // Kontrol KAYNAĞI indirmeden önce: filigran yoksa orijinali çekmenin anlamı yok
        // ve 20'lik bir partide bu 20 gereksiz S3 GET demekti.
        if (watermark is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.WatermarkUnavailable));
        }

        byte[]? kaynak = await IndirAsync(asset.OriginalObjectKey, cancellationToken);

        if (kaynak is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.StorageUnavailable));
        }

        return processor.Render(kaynak, watermark);
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
    /// R8/AC-13 — tur başarısız oldu: varlık <c>başarısız</c> işaretlenir ve
    /// <c>MediaAssetFailed</c> yayılır (AC-32).
    ///
    /// <para><b>Otomatik yeniden kuyruğa alma YOK</b> (AC-15). Önceki hâli deneme bütçesi
    /// tükenene kadar varlığı <c>yüklendi</c>'ye döndürüyordu; M1 o geçişi hiç saymıyor.
    /// Tekrar deneme artık <c>RetryMediaAssetCommand</c> ile, AÇIK bir komutla.</para>
    /// </summary>
    private async Task BasarisizAsync(
        MediaAsset asset,
        MediaFailureReason reason,
        CancellationToken cancellationToken)
    {
        Result basarisiz = asset.MarkFailed(reason, clock.UtcNow);

        if (basarisiz.IsFailure)
        {
            logger.LogError("Varlık başarısız işaretlenemedi: {AssetId}", asset.Id);
            return;
        }

        outbox.Write(new MediaAssetFailed(Guid.CreateVersion7(), clock.UtcNow, asset.Id, reason));

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// AC-33 — işlemcinin hata KODUNU duyurunun kapalı sebep kümesine eşler. Eşlenmeyen
    /// bir kod `Undecodable`'a düşüyor ve bu bilinçli: duyurunun kümesi KAPALI, yani
    /// işlemciye yeni bir hata kodu eklemek sözleşmeyi sessizce büyütemez.
    /// </summary>
    private static MediaFailureReason ReasonFor(string errorCode) => errorCode switch
    {
        MediaErrors.WatermarkUnavailable => MediaFailureReason.WatermarkUnavailable,
        MediaErrors.StorageUnavailable => MediaFailureReason.StorageUnavailable,
        _ => MediaFailureReason.Undecodable,
    };
}
