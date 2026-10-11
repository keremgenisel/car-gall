using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Events;

/// <summary>
/// `VehicleCatalog`'un sekiz entegrasyon olayı (`0012b` **D11**). Adlar
/// `docs/architecture.md` § 1'deki adlarla **birebir** aynıdır ve hepsi `Vehicle*`
/// öneklidir — önek § 1'in 2026-10-11 düzeltmesiyle kararlı hâle geldi.
///
/// <para><b>Gövde beş alan taşır ve YENİ DURUMU ayrı bir alan olarak TAŞIMAZ</b>
/// (**D30**): durumu olayın <b>tipi</b> söyler. Ayrı bir `status` alanı, tip ile alanın
/// ayrışabileceği bir ikinci gerçek kaynağı açardı — `VehicleSold` taşıyan bir olayın
/// gövdesinde `"published"` yazabilirdi ve hangisinin doğru olduğu tanımsız kalırdı.</para>
///
/// <para><b>Adres gövdede</b> çünkü sitemap tüketicisi (`docs/architecture.md` § 3 SEO)
/// onu olaydan almak zorunda; sonradan eklemek sürüm uyumluluğu sorunudur. <b>Fiyat
/// gövdede DEĞİL</b>: § 2.B değişken veriyi kaynaktan okutuyor ve `Notifications`'ın aynı
/// dersi kayıtlı — olay gövdesinde taşınan veri, kaynak değiştikten sonra outbox'ta
/// yaşamaya devam eder.</para>
///
/// <para><b>`PublishedAt` ilk yayın zamanıdır</b> (**D31**): yeniden yayınlanan ilan yine
/// duyurulur ve tüketici "bu ilk yayın değil" diyebilmek için o zamanı görmek zorundadır.
/// Yeniden yayında değer DONAR — `0002`'nin vitrin sıralaması ona dayanıyor.</para>
/// </summary>
public sealed record VehiclePublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug,
    DateTimeOffset PublishedAt) : IIntegrationEvent;

/// <summary>İlan yayından kaldırıldı ve `Taslak` durumuna döndü (A3).</summary>
public sealed record VehicleUnpublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>İlan rezerve edildi; vitrinde rozetiyle kalmaya devam eder (domain R6).</summary>
public sealed record VehicleReserved(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>Rezervasyon kaldırıldı; ilan `Yayında` durumuna döndü (**D17**).</summary>
public sealed record VehicleReturnedToShowcase(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>Araç satıldı. **A4**: bu durum terminaldir.</summary>
public sealed record VehicleSold(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>İlan arşivlendi; aracın arşiv bayrağı aynı işlemde yazıldı (**D9**).</summary>
public sealed record VehicleArchived(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>
/// Aracın fiyatı değişti. **Tutar gövdede YOK** (**D30**): tüketici güncel fiyatı
/// `VehicleCatalog`'un okuma yüzeyinden alır — tek atlama, § 2.A'nın saydığı meşru okuma.
/// Tetikleyicisi araç **düzenleme** komutudur (C7'nin adıyla yazılı istisnası).
/// </summary>
public sealed record VehiclePriceChanged(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;

/// <summary>
/// Araç başka bir şubeye taşındı (**A7**). Tetikleyicisi araç düzenleme komutudur; transfer
/// AKIŞI kapsam dışı (X7) — bu spec yalnız duyuruyu ekler.
/// </summary>
public sealed record VehicleRelocated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VehicleId,
    Guid ListingId,
    string Slug) : IIntegrationEvent;
