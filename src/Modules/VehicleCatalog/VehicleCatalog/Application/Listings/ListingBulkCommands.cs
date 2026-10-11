using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// `0012c` **R4**/**D45** — araç başına sonuç satırı. **Şekil TEK**: başarılı satır da
/// başarısız satır da aynı alanları taşır, yalnız doluluk değişir.
///
/// <para><c>ListingStatus</c> <b>başarılı satırda da</b> geliyor (**D45**): panel listeyi
/// yeniden çekmeden satırı boyayabiliyor. <c>null</c> yalnız bulunamayan araçta — onun
/// durumu yok ve uydurulmuş bir durum uçta yanlış cümle kurdururdu.</para>
///
/// <para><c>Parameters</c> tekil ucun gerekçesinin AYNISI (**D45**): yayın reddinde
/// `reasons` ve eksik alan anahtarları burada duruyor. Düşseydi toplu uç *"yayına
/// alınamadı"* derken tekil uç aynı araç için *"medya yok, zorunlu alan eksik"* derdi.</para>
/// </summary>
internal sealed record BulkListingRow(
    Guid VehicleId,
    bool Applied,
    string? ErrorCode,
    IReadOnlyDictionary<string, string>? Parameters,
    string? ListingStatus);

/// <summary>
/// Toplu eylem. **Tekil komutların ÜZERİNE** kurulu (**C2**): burada tek bir durum kararı
/// yok, tek bir `switch (listing.Status)` yok — geçiş mantığı `0012b`'nin agregatında
/// kalıyor.
///
/// <para><b>İşlem sınırı ilan başına</b> (**C3**/**D3**): her satır kendi kilidini alıp
/// kendi `SaveChanges`'ini yapıyor. Tek transaction, 20 ilandan 3'ü geçersizse
/// personeli ya "hepsi başarısız" yalanına ya tek tek denemeye zorlardı.</para>
///
/// <para><b>Satırlar GÖNDERİLDİĞİ SIRADA ve sırayla</b> (**D47**): paralelleştirmek kilit
/// çekişmesini çoğaltır ve yanıt sırasını belirsiz kılar. Bir satırın eşzamanlılık kaybı
/// (`concurrency.lost`) o satırda kalır; isteği düşürmek D3'ün kısmi başarı kararını
/// silerdi.</para>
/// </summary>
internal sealed class ListingBulkCommands(
    VehicleCatalogDbContext context,
    ListingTransitionCommands transitions,
    ListingFeatureCommands features,
    IBranchQueries branches,
    IVehicleModelNameResolver modelNames,
    CatalogAdminPagingOptions paging)
{
    /// <summary>
    /// İstek düzeyi reddi `Result`'ın hatasında, satır düzeyi sonuçlar değerinde.
    /// Çağıran ikisini ayırmak zorunda: biri `422`, diğeri `200` + liste (**D6**).
    /// </summary>
    internal async Task<Result<IReadOnlyList<BulkListingRow>>> ExecuteAsync(
        ListingOperation operation,
        IReadOnlyList<Guid>? vehicleIds,
        CancellationToken cancellationToken)
    {
        Result<BulkSelection> secim = BulkSelection.Create(vehicleIds, paging.MaxPageSize);

        if (secim.IsFailure)
        {
            return Result.Failure<IReadOnlyList<BulkListingRow>>(secim.Error);
        }

        IReadOnlyList<Guid> kimlikler = secim.Value.VehicleIds;

        // **D46** — aynı yanıtı veren dört okuma BURADA, istek başına bir kez. Medya
        // hazırlığı bilinçle dışarıda: o kayıt başına ve kilidin altında kalıyor.
        ListingReadContext read = await ListingReadContext.LoadAsync(
            context, branches, modelNames, kimlikler, cancellationToken);

        List<(Guid VehicleId, Error? Error)> sonuclar = [];

        foreach (Guid vehicleId in kimlikler)
        {
            Result sonuc = operation.IsFeature(out bool featured)
                ? await features.ExecuteAsync(vehicleId, featured, read, cancellationToken)
                : await transitions.ExecuteAsync(vehicleId, operation.ToAction(), read, cancellationToken);

            sonuclar.Add((vehicleId, sonuc.IsFailure ? sonuc.Error : null));
        }

        return Result.Success(await SatirlarAsync(sonuclar, cancellationToken));
    }

    /// <summary>
    /// Güncel durumlar **TEK sorguda** okunuyor, satır başına değil. Doğru olmasının
    /// nedeni sıradan geliyor: her satır bir sonraki başlamadan önce commit ediliyor ve
    /// hiçbir satır başka bir satırın ilanına dokunmuyor — yani turun sonundaki durum,
    /// her satırın kendi işleminden sonraki durumudur.
    ///
    /// <para>Durumun adı `ToString()` ile üretiliyor: veritabanında çevrilmiş durum adı
    /// saklanmıyor (Yasak Listesi 22) ve kodu uç çeviriyor.</para>
    /// </summary>
    private async Task<IReadOnlyList<BulkListingRow>> SatirlarAsync(
        IReadOnlyList<(Guid VehicleId, Error? Error)> sonuclar,
        CancellationToken cancellationToken)
    {
        List<VehicleId> kimlikler = [.. sonuclar.Select(sonuc => new VehicleId(sonuc.VehicleId))];

        List<(Guid VehicleId, ListingStatus Status)> durumlar = await context.Listings
            .AsNoTracking()
            .Where(listing => kimlikler.Contains(listing.VehicleId))
            .Select(listing => new ValueTuple<Guid, ListingStatus>(listing.VehicleId.Value, listing.Status))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> adlar = durumlar.ToDictionary(
            durum => durum.VehicleId,
            durum => durum.Status.ToString());

        return
        [
            .. sonuclar.Select(sonuc => new BulkListingRow(
                sonuc.VehicleId,
                sonuc.Error is null,
                sonuc.Error?.Code,
                sonuc.Error?.Parameters,
                adlar.GetValueOrDefault(sonuc.VehicleId)))
        ];
    }
}
