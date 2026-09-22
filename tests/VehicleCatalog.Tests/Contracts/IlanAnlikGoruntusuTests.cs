using System.Reflection;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;

namespace CarGallery.Modules.VehicleCatalog.Tests.Contracts;

/// <summary>
/// Sözleşme testi (docs/testing.md § 1). 0005'in DoD'si şunu istiyor: eklenen okuma
/// yüzeyi YALNIZCA talep anlık görüntüsü için gereken alanları taşır, gizli alan
/// sızdırmaz. Bu test o cümlenin kapısıdır — yüzeye sessizce eklenen bir alan burada
/// kırılır.
/// </summary>
public sealed class IlanAnlikGoruntusuTests
{
    private static readonly string[] BeklenenAlanlar =
    [
        nameof(ListingSnapshotDto.ListingId),
        nameof(ListingSnapshotDto.IsReserved),
        nameof(ListingSnapshotDto.VehicleModelId),
        nameof(ListingSnapshotDto.Price),
        nameof(ListingSnapshotDto.BranchId),
    ];

    /// <summary>F4 — şase, plaka ve iç notlar public yanıtta HİÇ bulunmaz.</summary>
    private static readonly string[] YasakParcalar = ["vin", "chassis", "plate", "plaka", "note", "internal"];

    [Fact(DisplayName = "AC-17: Talep, ilgilenilen aracın o andaki model adını ve fiyatını kendi içinde taşır")]
    public void TasirYalnizBeklenenAlanlari_IlanAnlikGoruntusu()
    {
        string[] alanlar =
        [
            .. typeof(ListingSnapshotDto)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != "EqualityContract")
                .Select(property => property.Name)
                .Order()
        ];

        Assert.Equal([.. BeklenenAlanlar.Order()], alanlar);
    }

    [Fact(DisplayName = "AC-17: Talep, ilgilenilen aracın o andaki model adını ve fiyatını kendi içinde taşır")]
    public void SizdirmazGizliAlan_IlanAnlikGoruntusu()
    {
        foreach (PropertyInfo property in typeof(ListingSnapshotDto).GetProperties())
        {
            foreach (string parca in YasakParcalar)
            {
                Assert.False(
                    property.Name.Contains(parca, StringComparison.OrdinalIgnoreCase),
                    $"Gizli alan public yüzeye konamaz (F3, F4): '{property.Name}'.");
            }
        }
    }
}
