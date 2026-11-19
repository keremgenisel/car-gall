using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `VehicleCatalog`'un ilan özeti okuması, bu test projesinin sınırının ötesindedir ve
/// sahtelenir (docs/testing.md § 1). Sahte, sunucunun GERÇEK biçimini taşır:
/// <see cref="ListingSummaryDto"/> ve "yalnız Yayında olanlar" sözleşmesi (`ListingSummaryQueries`).
/// Rezerve, satılmış, arşivlenmiş ve yayından kaldırılmış ilanın sözleşmedeki karşılığı aynıdır:
/// sonuçta YOKTUR.
/// </summary>
internal sealed class SahteIlanOzeti : IListingSummaryQueries
{
    private readonly Dictionary<Guid, ListingSummaryDto> yayinda = [];

    internal int CagriSayisi { get; private set; }

    internal ListingSummaryDto Yayinla(
        Guid ilanId,
        DateTimeOffset ilkYayin,
        decimal? fiyat = 1_250_000m,
        string paraBirimi = "TRY",
        int us = 2,
        Guid? modelId = null,
        Guid? subeId = null,
        int yil = 2019,
        int km = 125_000,
        string? slug = null)
    {
        ListingSummaryDto ozet = new(
            ilanId,
            slug ?? $"toyota-corolla-{yil}-{ilanId.ToString("N")[..6]}",
            modelId ?? SahteAdlar.Corolla,
            yil,
            km,
            fiyat is null ? ShowcasePriceDto.OnRequestPrice() : new ShowcasePriceDto(false, fiyat, paraBirimi, us),
            subeId ?? SahteAdlar.Kadikoy,
            ilkYayin);

        yayinda[ilanId] = ozet;

        return ozet;
    }

    /// <summary>Satıldı, Arşiv, Rezerve ya da Taslak: sözleşmenin dilinde hepsi "Yayında değil".</summary>
    internal void YayindanCikar(Guid ilanId) => yayinda.Remove(ilanId);

    internal void FiyatDegistir(Guid ilanId, decimal fiyat) =>
        yayinda[ilanId] = yayinda[ilanId] with { Price = new ShowcasePriceDto(false, fiyat, yayinda[ilanId].Price.CurrencyCode, yayinda[ilanId].Price.MinorUnitExponent) };

    public Task<IReadOnlyList<ListingSummaryDto>> FindPublishedAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken)
    {
        CagriSayisi++;

        return Task.FromResult<IReadOnlyList<ListingSummaryDto>>(
            [.. listingIds.Distinct().Where(yayinda.ContainsKey).Select(id => yayinda[id])]);
    }
}

/// <summary>Sahte kimlikler: model ve şube.</summary>
internal static class SahteAdlar
{
    internal static readonly Guid Corolla = Guid.Parse("0192c000-0000-7000-8000-0000000a0001");

    internal static readonly Guid Kadikoy = Guid.Parse("0192c000-0000-7000-8000-0000000b0001");
}
