using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

/// <summary>
/// 0011/AC-36 — duyurunun alıcısı. `IIntegrationEventHandler<MediaAssetReady>`'i uygular ve
/// DI'ya kayıtlıdır; dağıtıcı onu buradan bulur.
///
/// <para><b>Önceki hâli KABLOLANAMAZ durumdaydı</b> ve `0011`'in CLARIFY turu bunu ölçtü:
/// imzası <c>HandleAsync(MediaAssetReady, Guid vehicleId, CancellationToken)</c> idi —
/// dağıtıcının istediği sözleşmeyi uygulamıyor, DI'ya kayıtlı değil ve tek çağıranı
/// <c>vehicleId</c>'yi elle veren bir test. O değer olayın gövdesinde yok ve R10 gereği
/// `Media`'da da yok. `0003`'ün kaçan hatası birebir buydu: <i>"test edilmiş bir alıcı,
/// yayıncısı olmadan hiçbir şey kanıtlamaz."</i></para>
///
/// <para><b>`vehicleId` parametresi kalktı</b> ve yerine bir şey gelmedi: <b>kapak varlık
/// kimliği tek başına ilanı belirliyor.</b> Medya sahipsizdir (R10); hangi aracın hangi
/// görsele sahip olduğu bu modülün verisidir ve okuma modeli onu <c>cover_asset_id</c>
/// olarak zaten tutuyor.</para>
///
/// <para><b>Idempotent</b> (C4): teslim en-az-bir-kez. `BranchDeactivatedHandler`'ın aynı
/// deseni — inbox kaydı işle AYNI `SaveChanges` içinde.</para>
/// </summary>
internal sealed class MediaAssetReadyHandler(VehicleCatalogDbContext context, IClock clock)
    : IdempotentIntegrationEventHandler<MediaAssetReady, VehicleCatalogDbContext>(context, clock)
{
    /// <summary>
    /// 0011/AC-29 — duyuru türev kümesini taşıyor ve kapak görseli **gösterim** türevidir.
    /// Etiket adıyla seçiliyor: kümedeki sırayla seçmek, üretim sırası değiştiği gün
    /// kapağın sessizce 400 piksellik türev olması demekti.
    /// </summary>
    private const string DisplayVariant = "display";

    protected override async Task HandleCoreAsync(
        MediaAssetReady integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // `0016b` D25 — galeri yuvaları: varlığın bulunduğu HER yuva hazır olur ve türevlerin
        // gerçek boyutlarını alır (D18). Yuva yoksa (varlık hiçbir vitrin ilanında değil)
        // güncelleme hiçbir satıra dokunmaz. İlan yeniden yayımlanmaz (AC-3).
        List<ShowcaseMediaVariant> turevler =
        [
            .. integrationEvent.Derivatives.Select(turev => new ShowcaseMediaVariant(turev.Variant, turev.Url, turev.Width, turev.Height)),
        ];

        await Context.ShowcaseListingMedia
            .Where(yuva => yuva.MediaAssetId == integrationEvent.MediaAssetId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(yuva => yuva.Status, ShowcaseMediaStatus.Ready)
                    .SetProperty(yuva => yuva.Variants, turevler),
                cancellationToken);

        MediaDerivativePayload? gosterim = integrationEvent.Derivatives
            .FirstOrDefault(turev => turev.Variant == DisplayVariant);

        if (gosterim is null)
        {
            // Küme gösterim türevi taşımıyor: yazacak bir şey yok. Sessiz geçmek doğru —
            // alıcının başarısızlığı yayıncının işlemini iptal etmemeli
            // (`docs/architecture.md` § 2.B) ve `Media` bu durumu kendi tarafında
            // `başarısız` olarak zaten işaretliyor.
            return;
        }

        // YALNIZCA kapak varlığı yazılır. Gelen her olayda üzerine yazmak, üç görselli bir
        // araçta en son hazır olan görseli kapak yapardı — medya işleme süreleri farklı ve
        // teslimat sıra garantisi vermiyor (docs/architecture.md § 2.C). Kapak, sıralı
        // listenin ilk elemanıdır (docs/domain.md) ve bu karar domain'e aittir, olay
        // sırasına değil.
        await Context.ShowcaseListings
            .Where(listing => listing.CoverAssetId == integrationEvent.MediaAssetId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(listing => listing.CoverMediaUrl, gosterim.Url),
                cancellationToken);
    }
}
