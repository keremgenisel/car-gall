namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Kanonik Latin model adı `VehicleReference`'a aittir (docs/architecture.md § 3).
/// `Leads` onu KENDİ tek atlamasıyla alır — Catalog üzerinden istemek
/// `Leads → VehicleCatalog → VehicleReference` zinciri olurdu (Yasak Listesi 13).
///
/// <c>null</c> = model çözülemedi. Çağıran bunu talebi reddetmek için kullanır: adsız bir
/// anlık görüntü, sonradan doldurulamayacak bir boşluktur (R7).
/// </summary>
internal interface ICanonicalModelNameSource
{
    Task<string?> FindAsync(Guid vehicleModelId, CancellationToken cancellationToken);
}
