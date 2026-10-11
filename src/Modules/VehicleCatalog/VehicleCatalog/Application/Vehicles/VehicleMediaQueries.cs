using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// `0012a`/AC-24 — aracın sıralı medya listesini `Media`'nın durum, `alt` ve türev
/// bilgisiyle **sunucuda** birleştirir.
///
/// <para><b>Birleştirme neden burada:</b> sıra `VehicleCatalog`'un verisidir (**C2**,
/// **M2**) ve durum/`alt`/türev `Media`'nın. `docs/architecture.md` § 2.A'nın saydığı
/// meşru senkron okuma tam bu: <i>"`VehicleCatalog → Media` (görseller hazır mı, URL
/// çöz)"</i> — salt okuma, aynı istek içinde gerekli, **tek atlama** (**C3**).</para>
///
/// <para><b>Neden varlık başına değil:</b> panel üç-dört fotoğraflı bir aracı tek ekranda
/// gösteriyor; <c>FindAsync</c>'i varlık başına çağırmak N+1 istek üretir ve
/// sıralama/birleştirme kuralını tarayıcıya taşır (**D10**).</para>
/// </summary>
internal sealed class VehicleMediaQueries(
    VehicleCatalogDbContext context,
    IMediaAssetQueries mediaAssets) : IVehicleMediaQueries
{
    public async Task<VehicleMediaDto?> FindAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        Vehicle? vehicle = await context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == new VehicleId(vehicleId), cancellationToken);

        if (vehicle is null)
        {
            return null;
        }

        IReadOnlyList<Guid> sirali = vehicle.MediaAssetIds;

        if (sirali.Count == 0)
        {
            return new VehicleMediaDto(vehicleId, CoverMediaAssetId: null, Items: []);
        }

        IReadOnlyList<MediaAssetDto> varliklar =
            await mediaAssets.FindManyAsync(sirali, cancellationToken);

        Dictionary<Guid, MediaAssetDto> kimlige = varliklar.ToDictionary(dto => dto.Id);

        List<VehicleMediaItemDto> ogeler = [];

        foreach (Guid kimlik in sirali)
        {
            // `Media`'da bulunamayan bir kimlik ATLANIYOR, uydurulmuyor. Böyle bir durum
            // D15'in açık bıraktığı paylaşımdan ya da elle silinmiş bir kayıttan doğabilir;
            // yanıtta durumsuz bir satır göstermek, ekranda ne olduğu belirsiz bir kutu
            // demekti.
            if (!kimlige.TryGetValue(kimlik, out MediaAssetDto? varlik))
            {
                continue;
            }

            ogeler.Add(new VehicleMediaItemDto(
                varlik.Id,
                varlik.Status.ToString().ToLowerInvariant(),
                varlik.FailureReason?.ToString().ToLowerInvariant(),
                varlik.AltText,
                [.. varlik.Derivatives.Select(turev => new VehicleMediaDerivativeDto(
                    turev.Variant, turev.Width, turev.Height, turev.Url))]));
        }

        // Kapak TÜRETİLMİŞ: sıralı listenin ilk elemanı (D2). `Media`'da bulunamayan bir
        // ilk eleman atlandığı için kapak da ÖĞELERİN ilki — yanıtın kendi içinde tutarlı
        // olması, ekranın kapak diye gösterecek bir satır bulmasını garanti ediyor.
        return new VehicleMediaDto(
            vehicleId,
            ogeler.Count > 0 ? ogeler[0].MediaAssetId : null,
            ogeler);
    }
}
