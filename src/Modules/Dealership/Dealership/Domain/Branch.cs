using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain.Events;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// Galerinin fiziksel lokasyonu (docs/domain.md). `Dealership` bu verinin TEK yazma
/// sahibidir (C1); başka hiçbir modül onu yazmaz.
/// </summary>
internal sealed class Branch : AggregateRoot<BranchId>
{
    internal const string DefaultLocaleNameRequired = "branch.default_locale_name_required";
    internal const string DefaultLocaleAddressRequired = "branch.default_locale_address_required";
    internal const string InvalidTransition = "branch.invalid_status_transition";
    internal const string LastActiveBranch = "branch.last_active_branch";

    private readonly List<Guid> mediaAssetIds = [];

    private Branch(
        BranchId id,
        LocalizedText names,
        LocalizedText addresses,
        PhoneNumber phone,
        TimeZoneId timeZone,
        BranchSlug slug)
        : base(id)
    {
        Names = names;
        Addresses = addresses;
        Phone = phone;
        TimeZone = timeZone;
        Slug = slug;
        Status = BranchStatus.Active;
    }

    internal LocalizedText Names { get; private set; }

    internal LocalizedText Addresses { get; private set; }

    /// <summary>Opsiyonel (R2, AC-7).</summary>
    internal Coordinate? Coordinate { get; private set; }

    internal PhoneNumber Phone { get; private set; }

    internal PhoneNumber? WhatsApp { get; private set; }

    internal EmailAddress? Email { get; private set; }

    internal TimeZoneId TimeZone { get; private set; }

    internal BranchStatus Status { get; private set; }

    internal BranchSlug Slug { get; private set; }

    /// <summary>D7 — yalnızca REFERANS; görselin kendisi `Media` spec'ine aittir.</summary>
    internal IReadOnlyList<Guid> MediaAssetIds => mediaAssetIds;

    /// <summary>
    /// Varsayılan locale'deki ad ve adres ZORUNLUDUR (R2, AC-6). R5 okuma yolunun hiçbir
    /// koşulda boş ad döndürmemesini bu garantiye dayandırıyor.
    /// </summary>
    internal static Result<Branch> Open(
        LocalizedText names,
        LocalizedText addresses,
        PhoneNumber phone,
        TimeZoneId timeZone,
        BranchSlug slug,
        Locale defaultLocale)
    {
        if (!names.HasValueFor(defaultLocale))
        {
            return Result.Failure<Branch>(new Error(DefaultLocaleNameRequired));
        }

        if (!addresses.HasValueFor(defaultLocale))
        {
            return Result.Failure<Branch>(new Error(DefaultLocaleAddressRequired));
        }

        Branch branch = new(BranchId.New(), names, addresses, phone, timeZone, slug);
        branch.Raise(new BranchWasOpened(branch.Id));

        return Result.Success(branch);
    }

    internal void SetCoordinate(Coordinate coordinate) => Coordinate = coordinate;

    internal void SetWhatsApp(PhoneNumber number) => WhatsApp = number;

    internal void SetEmail(EmailAddress email) => Email = email;

    internal void AttachMediaAsset(Guid mediaAssetId)
    {
        if (!mediaAssetIds.Contains(mediaAssetId))
        {
            mediaAssetIds.Add(mediaAssetId);
        }
    }

    /// <summary>R8/AC-21 — duyuruyu YALNIZCA bu doğurur.</summary>
    internal Result ChangeDisplayNames(LocalizedText names, Locale defaultLocale)
    {
        if (!names.HasValueFor(defaultLocale))
        {
            return Result.Failure(new Error(DefaultLocaleNameRequired));
        }

        Names = names;
        Raise(new BranchDisplayNameWasChanged(Id));

        return Result.Success();
    }

    /// <summary>AC-22 — bunların hiçbiri duyuru üretmez.</summary>
    internal Result ChangeAddresses(LocalizedText addresses, Locale defaultLocale)
    {
        if (!addresses.HasValueFor(defaultLocale))
        {
            return Result.Failure(new Error(DefaultLocaleAddressRequired));
        }

        Addresses = addresses;
        return Result.Success();
    }

    internal void ChangePhone(PhoneNumber phone) => Phone = phone;

    internal void ChangeTimeZone(TimeZoneId timeZone) => TimeZone = timeZone;

    internal void ChangeSlug(BranchSlug slug) => Slug = slug;

    /// <summary>
    /// AC-18 — tek aktif şube olsa bile reddedilmez. `Geçici kapalı` bir ÇALIŞMA SAATİ
    /// gerçeğidir, bir lokasyon gerçeği değil; A8'i tetiklemez (D3).
    /// </summary>
    internal Result CloseTemporarily()
    {
        if (Status is not BranchStatus.Active)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = BranchStatus.TemporarilyClosed;
        return Result.Success();
    }

    internal Result Reopen()
    {
        // AC-19 — `Kapatılmış` terminaldir; yalnız `Geçici kapalı` geri dönebilir.
        if (Status is not BranchStatus.TemporarilyClosed)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = BranchStatus.Active;
        return Result.Success();
    }

    /// <summary>
    /// B5/AC-16 — son AKTİF şube kapatılamaz. Sayı dışarıdan verilir: bir şube kaç kardeşi
    /// olduğunu bilemez, ama invariant'ın agregatın dışına kaçması da gerekmez.
    /// </summary>
    internal Result Close(int otherActiveBranchCount)
    {
        if (Status is BranchStatus.Closed)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        if (Status is BranchStatus.Active && otherActiveBranchCount == 0)
        {
            return Result.Failure(new Error(LastActiveBranch));
        }

        Status = BranchStatus.Closed;
        Raise(new BranchWasClosed(Id));

        return Result.Success();
    }
}
