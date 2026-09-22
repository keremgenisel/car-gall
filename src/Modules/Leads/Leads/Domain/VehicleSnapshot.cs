using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// docs/domain.md **L2** — talep anındaki araç bilgisinin KOPYASI. Referans değil kopya:
/// talebe yalnız bir kimlik koyup fiyatı okuma anında araçtan çekmek L2'nin tam tersidir;
/// araç satılınca veya fiyatı değişince talep geriye dönük olarak YALAN SÖYLER
/// (Bilinen tuzak 1). Bu tip bu yüzden hiçbir şey çözmez; yalnız taşır.
///
/// <see cref="ListingId"/> izlenebilirlik içindir ve bir OKUMA YOLU DEĞİLDİR: modülde o
/// kimlikle araca giden tek bir sorgu yoktur ve olmamalıdır.
///
/// Model adı **kanonik Latin addır** (R7, AC-18). Ziyaretçinin dilinde dondurulan bir ad,
/// o dili okumayan danışman için talebi okunamaz hâle getirir ve kopya sonradan
/// düzeltilemez (Bilinen tuzak 7).
///
/// Fiyat, kolonlara DÜZ yazılır (tutar + ISO 4217 kodu + minor unit üssü). `Money` tipi
/// sınırlarda kullanılır — <see cref="Create"/> ile girer, <see cref="ReadPrice"/> ile
/// çıkar; arada saklanan şey `decimal(19,4)` + kod + üstür (C6, P2).
/// </summary>
internal readonly record struct VehicleSnapshot
{
    private VehicleSnapshot(
        Guid listingId,
        string canonicalModelName,
        bool priceOnRequest,
        decimal? priceAmount,
        string? priceCurrencyCode,
        int? priceMinorUnitExponent)
    {
        ListingId = listingId;
        CanonicalModelName = canonicalModelName;
        PriceOnRequest = priceOnRequest;
        PriceAmount = priceAmount;
        PriceCurrencyCode = priceCurrencyCode;
        PriceMinorUnitExponent = priceMinorUnitExponent;
    }

    internal Guid ListingId { get; init; }

    internal string CanonicalModelName { get; init; }

    /// <summary>"Fiyat sorunuz" AYRI bir durumdur, `0` değildir (P4, C7, AC-21).</summary>
    internal bool PriceOnRequest { get; init; }

    internal decimal? PriceAmount { get; init; }

    internal string? PriceCurrencyCode { get; init; }

    /// <summary>KWD/BHD/JOD/OMR üç ondalıklıdır; üs kopyanın parçasıdır (AC-22).</summary>
    internal int? PriceMinorUnitExponent { get; init; }

    internal static Result<VehicleSnapshot> Create(
        Guid listingId,
        string? canonicalModelName,
        PriceSnapshot price)
    {
        if (string.IsNullOrWhiteSpace(canonicalModelName))
        {
            return Result.Failure<VehicleSnapshot>(new Error(LeadErrors.EmptyModelName));
        }

        if (price.IsOnRequest)
        {
            return Result.Success(
                new VehicleSnapshot(listingId, canonicalModelName.Trim(), true, null, null, null));
        }

        Money amount = price.Amount!.Value;

        return Result.Success(new VehicleSnapshot(
            listingId,
            canonicalModelName.Trim(),
            false,
            amount.Amount,
            amount.CurrencyCode,
            amount.MinorUnitExponent));
    }

    /// <summary>
    /// Metot, özellik değil: EF karmaşık tipin okunabilir özelliklerini kolona eşler;
    /// hesaplanan bir özellik ya yanlışlıkla saklanır ya da her kullanımda elle
    /// dışlanmak zorunda kalır. Kopyanın kendisi kolonlarda durur, bu yalnız okuma biçimi.
    /// </summary>
    internal PriceSnapshot ReadPrice() => PriceOnRequest
        ? PriceSnapshot.OnRequest()
        : PriceSnapshot.Of(
            Money.Create(PriceAmount!.Value, PriceCurrencyCode!, PriceMinorUnitExponent!.Value).Value);
}
