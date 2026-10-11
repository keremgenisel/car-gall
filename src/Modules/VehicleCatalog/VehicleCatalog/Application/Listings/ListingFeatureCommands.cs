using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// `0012c` **R5** — öne çıkarma ve kaldırma. Geçiş komutundan **AYRI** bir sınıf ve bu
/// ayrım kararın kendisi: öne çıkarma bir durum geçişi değil (A3 onu tanımıyor), bir
/// **sıralama** eylemidir.
///
/// <para>Aynı kilit (`ListingLock`), aynı sıra: kilitle, oku, karar ver, yansıt, kaydet.
/// Karar yine agregatta (**C2**).</para>
///
/// <para><b>Duyuru YOK</b> (**D16**): `Listing.Feature` olay yaymıyor, dolayısıyla
/// `VehicleCatalogUnitOfWork`'ün çevirisine hiçbir şey düşmüyor ve outbox'a satır
/// yazılmıyor. Bu bir ihmal değil karar — vitrin sırasının ayarı `Audit` ve `Engagement`
/// için yeni bir olgu üretmez.</para>
/// </summary>
internal sealed class ListingFeatureCommands(
    VehicleCatalogDbContext context,
    VehicleCatalogUnitOfWork unitOfWork,
    ShowcaseProjectionWriter projection,
    InvariantGuardOptions guardOptions,
    IClock clock)
{
    internal Task<Result> ExecuteAsync(Guid vehicleId, bool featured, CancellationToken cancellationToken) =>
        ExecuteAsync(vehicleId, featured, read: null, cancellationToken);

    /// <summary>
    /// <c>read</c> toplu yolun ön okuma bağlamı (**D46**). Öne çıkarma medya hazırlığı
    /// sormuyor — yayın koşulu değil — ama yansıtma şube adlarını ve public alanları
    /// istiyor ve onlar bağlamdan geliyor.
    /// </summary>
    internal Task<Result> ExecuteAsync(
        Guid vehicleId,
        bool featured,
        ListingReadContext? read,
        CancellationToken cancellationToken) =>
        InvariantGuard.ExecuteAsync(
            context,
            ListingLock.Row(vehicleId),
            excluded: Guid.Empty,
            async (_, ct) =>
            {
                Vehicle? vehicle = await context.Vehicles
                    .FirstOrDefaultAsync(item => item.Id == new VehicleId(vehicleId), ct);

                Listing? listing = vehicle is null
                    ? null
                    : await context.Listings.FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, ct);

                if (vehicle is null || listing is null)
                {
                    return Result.Failure(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId));
                }

                Result sonuc = featured ? listing.Feature() : listing.Unfeature();

                if (sonuc.IsFailure)
                {
                    return sonuc;
                }

                // **D36** — eylem zaman damgasını ilerletir. Geçişlerle aynı gerekçe
                // (`0012b` **D20**): listenin tek satırı iki agregattan besleniyor ve
                // yalnız ilana yazsaydık araç listede yerinden kıpırdamazdı.
                vehicle.Touch(clock.UtcNow);

                await projection.ProjectAsync(vehicle, listing, read, ct);
                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);
}
