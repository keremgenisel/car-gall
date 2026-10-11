using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// `0012a` D12/AC-2…9 — aracın sıralı medya listesini yerine koyar.
///
/// <para><b>Tek idempotent işlem:</b> bağlama (R1), sıralama (R2) ve çıkarma (R3) aynı
/// isteğin içinde. Kapak **sıranın sonucu** (D2); ayrı bir "kapak yap" eylemi, D2'nin
/// reddettiği ikinci gerçek kaynağı geri getirirdi.</para>
///
/// <para><b>Çıkarmak SİLMEZ</b> (R3, AC-7): bu komut yalnız `VehicleCatalog`'un listesine
/// dokunuyor; varlık `Media`'da olduğu gibi kalıyor ve başka bir araca bağlanabilir
/// (D15). **F1**'in aynı ilkesi — kapatmak/çıkarmak silmek değildir.</para>
///
/// <para><b>Eşzamanlılık koruması YOK</b> (D13) ve bu bilinçli: `UpdateVehicleCommand` da
/// taşımıyor ve aynı agregatın iki yazma yolundan yalnız birini korumak tutarsız olurdu.
/// Bedel: eşzamanlı iki düzenlemede son yazan kazanır.</para>
/// </summary>
internal sealed class UpdateVehicleMediaCommand(
    VehicleCatalogDbContext context,
    IMediaAssetQueries mediaAssets,
    ShowcaseProjectionWriter projection,
    IClock clock)
{
    internal async Task<Result> ExecuteAsync(
        Guid vehicleId,
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediaAssetIds);

        Vehicle? vehicle = await context.Vehicles
            .FirstOrDefaultAsync(value => value.Id == new VehicleId(vehicleId), cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId));
        }

        // AC-9 — var olmayan bir varlık bağlanamaz. Kontrol `Application` katmanında
        // çünkü varlığın var olup olmadığı `Media`'nın bilgisi ve `Domain` onu bilemez.
        //
        // Toplu okuma: kimlik başına bir çağrı, dört fotoğraflı bir araçta dört istek
        // demekti (D10).
        if (mediaAssetIds.Count > 0)
        {
            IReadOnlyList<MediaAssetDto> bulunan =
                await mediaAssets.FindManyAsync(mediaAssetIds, cancellationToken);

            HashSet<Guid> varOlan = [.. bulunan.Select(dto => dto.Id)];

            // `Cast<Guid?>` ŞART. `Guid` bir DEĞER tipi: `FirstOrDefault` eşleşme
            // bulamadığında `null` değil `Guid.Empty` döner ve "eksik yok" ile "eksik
            // olan Guid.Empty" aynı değere iner. Önceki hâl bunu `eksik != Guid.Empty`
            // ile kapatmaya çalışıyordu ve gerçekten eksik olan sıfır kimliği de
            // eliyordu: `"mediaAssetIds": ["00000000-…"]` gövdesi `204` alıyor ve var
            // olmayan bir kimlik yazma modeline giriyordu (dış bulgu, ölçüldü).
            Guid? eksik = mediaAssetIds
                .Where(kimlik => !varOlan.Contains(kimlik))
                .Cast<Guid?>()
                .FirstOrDefault();

            if (eksik is not null)
            {
                return Result.Failure(Error.With(
                    Vehicle.MediaAssetNotFound, "mediaAssetId", eksik.Value.ToString()));
            }
        }

        // D14 — DURUM ön koşulu yok: `işleniyor` ve `başarısız` varlıklar da bağlanabilir.
        Result sonuc = vehicle.ReplaceMediaAssets(mediaAssetIds);

        if (sonuc.IsFailure)
        {
            return sonuc;
        }

        vehicle.Touch(clock.UtcNow);

        // 0012b D41 — kapak sıralı listenin İLK elemanı; sıra değişince vitrindeki kapak
        // değişiyor. R15'in saydığı yazma yolları arasında yoktu ve dışarıda bırakmak,
        // fotoğrafı yeniden sıralayan personelin vitrinde ESKİ kapağı görmesi demekti.
        await projection.ProjectAsync(vehicle, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
