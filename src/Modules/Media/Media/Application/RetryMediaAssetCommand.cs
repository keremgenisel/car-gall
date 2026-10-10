using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// AC-14 — tekrar denemenin AÇIK komutu. `başarısız` bir varlığı `yüklendi`'ye döndürür ve
/// **aynı kimliği korur**; sıradaki işleme turu onu yeniden eline alır.
///
/// <para><b>Otomatik bir yol bilinçli olarak YOK</b> (AC-15): `0009`'un kaçan hatası
/// otomatik tekrarın kuyruğu sessizce tıkamasıydı. Komut çağrılmadan hiçbir varlık
/// `başarısız`dan çıkmaz.</para>
///
/// <para>Ekranı `0012` basacak (X8); bu spec komutu ve ucunu sunar.</para>
/// </summary>
internal sealed class RetryMediaAssetCommand(MediaDbContext context, IClock clock)
{
    internal async Task<Result> ExecuteAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        MediaAsset? asset = await context.Assets
            .FirstOrDefaultAsync(value => value.Id == mediaAssetId, cancellationToken);

        if (asset is null)
        {
            return Result.Failure(new Error(MediaErrors.NotFound));
        }

        Result sonuc = asset.Retry(clock.UtcNow);

        if (sonuc.IsFailure)
        {
            // Durum geçişi reddedildi (ör. varlık `hazır` ya da halen `işleniyor`).
            // Domain'in kodu yerine ucun okuyabileceği daha dar bir kod dönüyor: çağıran
            // "bu varlık tekrar denenemez" bilgisini istiyor, geçişin adını değil.
            return Result.Failure(Error.With(
                MediaErrors.NotRetryable, "status", asset.Status.ToString().ToLowerInvariant()));
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
