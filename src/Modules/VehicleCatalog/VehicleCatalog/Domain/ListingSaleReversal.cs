using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// `0023` **D7** — bir satış geri almasının kaydı: ilan, geri alan personel, an, gerekçe ve
/// dönülen durum (**R6**). Sahibin kararı (**D1**) satış geçmişinin silinmemesi, üstüne
/// "geri alındı" yazılmasıydı; bu satır o yazının kendisidir.
///
/// <para><b>Kayıt silinmez ve düzenlenmez</b> ve invariant kodun ŞEKLİNDEDİR, bir
/// kontrolde değil (fiyat geçmişinin deseni, **A6**): bu tipte hiçbir yazan metot yok,
/// hiçbir uçta düzenleme ya da silme yolu yok (**AC-21**).</para>
///
/// <para><b>Personelin adı kayıt anındaki hâliyle kopyalanır.</b> Kayıt bir olgudur:
/// "kim yaptı" sorusunun cevabı o anki addır. Adı sonradan `Identity`'den çözmek, kaydın
/// okunmasını başka bir modülün o anki verisine bağlardı ve pasifleştirilmiş bir
/// personelin kaydı boş görünürdü.</para>
///
/// <para><b>Gerekçe kişisel veri taşıyabilir</b> (**C5**): bu satırın dışında hiçbir
/// loga yazılmaz ve hiçbir duyurunun gövdesine girmez.</para>
/// </summary>
internal sealed class ListingSaleReversal : Entity<SaleReversalId>
{
    /// <summary>**D4** — "x" gibi bir gerekçe kaydı anlamsız kılar.</summary>
    internal const int MinimumReasonLength = 10;

    /// <summary>**D4** — üst sınır kaydı ve ekranı korur.</summary>
    internal const int MaximumReasonLength = 500;

    /// <summary>Kopyalanan personel adının sınırı; `Identity`'nin kendi sınırıyla aynı (200).</summary>
    internal const int MaximumStaffNameLength = 200;

    /// <summary>Gerekçe boş ya da yalnız boşluk (**AC-16**).</summary>
    internal const string ReasonRequired = "listing.sale_reversal.reason_required";

    internal const string ReasonTooShort = "listing.sale_reversal.reason_too_short";

    internal const string ReasonTooLong = "listing.sale_reversal.reason_too_long";

    private ListingSaleReversal(
        SaleReversalId id,
        ListingId listingId,
        VehicleId vehicleId,
        Guid staffId,
        string staffDisplayName,
        string reason,
        ListingStatus restoredStatus,
        DateTimeOffset revertedAt)
        : base(id)
    {
        ListingId = listingId;
        VehicleId = vehicleId;
        StaffId = staffId;
        StaffDisplayName = staffDisplayName;
        Reason = reason;
        RestoredStatus = restoredStatus;
        RevertedAt = revertedAt;
    }

    /// <summary>YALNIZCA EF içindir; iş kodundan çağrılmaz.</summary>
    private ListingSaleReversal()
        : base(default)
    {
        StaffDisplayName = string.Empty;
        Reason = string.Empty;
    }

    internal ListingId ListingId { get; private set; }

    internal VehicleId VehicleId { get; private set; }

    /// <summary>Geri alan personelin kimliği — `Identity`'nin kimliği, çıplak `Guid` (§ 9).</summary>
    internal Guid StaffId { get; private set; }

    internal string StaffDisplayName { get; private set; }

    internal string Reason { get; private set; }

    internal ListingStatus RestoredStatus { get; private set; }

    internal DateTimeOffset RevertedAt { get; private set; }

    /// <summary>
    /// Gerekçenin kuralı TEK yerde (**D4**). Baştaki ve sondaki boşluk kırpılır ve sınır
    /// kırpılmış metne uygulanır: on boşluk "on karakterlik gerekçe" değildir (**AC-16**).
    /// </summary>
    internal static Result<string> NormalizeReason(string? reason)
    {
        string kirpilmis = (reason ?? string.Empty).Trim();

        if (kirpilmis.Length == 0)
        {
            return Result.Failure<string>(new Error(ReasonRequired));
        }

        if (kirpilmis.Length < MinimumReasonLength)
        {
            return Result.Failure<string>(new Error(ReasonTooShort));
        }

        return kirpilmis.Length > MaximumReasonLength
            ? Result.Failure<string>(new Error(ReasonTooLong))
            : Result.Success(kirpilmis);
    }

    /// <summary>
    /// Kaydı kurar. Gerekçe <see cref="NormalizeReason"/>'dan geçmiş olmalı; burada YENİDEN
    /// denetleniyor çünkü kayıt kuralı delen bir gerekçeyle hiç var olmamalı.
    /// </summary>
    internal static Result<ListingSaleReversal> Record(
        Listing listing,
        Guid staffId,
        string staffDisplayName,
        string reason,
        DateTimeOffset revertedAt)
    {
        ArgumentNullException.ThrowIfNull(listing);

        Result<string> gerekce = NormalizeReason(reason);

        if (gerekce.IsFailure)
        {
            return Result.Failure<ListingSaleReversal>(gerekce.Error);
        }

        string ad = (staffDisplayName ?? string.Empty).Trim();

        return Result.Success(new ListingSaleReversal(
            SaleReversalId.New(),
            listing.Id,
            listing.VehicleId,
            staffId,
            ad.Length > MaximumStaffNameLength ? ad[..MaximumStaffNameLength] : ad,
            gerekce.Value,
            listing.Status,
            revertedAt));
    }
}
