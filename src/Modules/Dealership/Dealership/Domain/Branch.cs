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

    /// <summary>`0020` R10 · D21 — kapatılmış şube salt okunurdur.</summary>
    internal const string ClosedReadOnly = "branch.closed_read_only";

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

    /// <summary>
    /// `0025` **D4** · **D15** — haftalık çalışma programı; <c>null</c> = girilmemiş. Boş program
    /// (yedi boş gün) saklanmaz, <c>null</c> olur. Değişikliği duyuru üretmez (C7, D11).
    /// </summary>
    internal OpeningHours? OpeningHours { get; private set; }

    /// <summary>
    /// `0027` D2 · D18 — özel günler; <c>null</c> = kayıt yok. Saklanan kayıtlar geçmiş olabilir: okumalar
    /// bugüne göre süzer (D4), silinmeleri yalnız <c>specialHours</c> taşıyan bir kaydetmededir (B10).
    /// </summary>
    internal SpecialHours? SpecialHours { get; private set; }

    /// <summary>D7 — yalnızca REFERANS; görselin kendisi `Media` spec'ine aittir.</summary>
    internal IReadOnlyList<Guid> MediaAssetIds => mediaAssetIds;

    /// <summary>
    /// `0020` R16 · D17 — iyimser eşzamanlılık. Alan düzenlemesi DE durum eylemleri de bir
    /// artırır: düzenleme formu açıkken şube geçici kapatılırsa formun kaydı reddedilir (AC-62).
    /// </summary>
    internal int Version { get; private set; } = 1;

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

    /// <summary>`0020` R9 — açma: zorunlu alanlar `Open`'dan, opsiyoneller ardından.</summary>
    internal static Result<Branch> Open(BranchDraft draft, Locale defaultLocale)
    {
        Result<Branch> opened = Open(draft.Names, draft.Addresses, draft.Phone, draft.TimeZone, draft.Slug, defaultLocale);

        if (opened.IsSuccess)
        {
            opened.Value.ApplyOptionals(draft);
        }

        return opened;
    }

    internal void SetCoordinate(Coordinate coordinate) => Coordinate = coordinate;

    internal void SetWhatsApp(PhoneNumber number) => WhatsApp = number;

    internal void SetEmail(EmailAddress email) => Email = email;

    /// <summary>`0020` D9 — opsiyonel alanlar boşaltılabilir (AC-39).</summary>
    internal void ClearCoordinate() => Coordinate = null;

    /// <summary>`0020` D9.</summary>
    internal void ClearWhatsApp() => WhatsApp = null;

    /// <summary>`0020` D9.</summary>
    internal void ClearEmail() => Email = null;

    /// <summary>
    /// `0020` R10 — panelin "kaydet"i: R9'daki bütün alanlar tek seferde. Durum alanına
    /// DOKUNMAZ (Bilinen tuzak 2); durum yalnız üç eylemden değişir.
    ///
    /// <para><b>Ad yalnız değiştiyse</b> <see cref="ChangeDisplayNames"/> çağrılır (Bilinen
    /// tuzak 1): adı aynı kalan bir kaydetme duyuru üretmez (AC-37), herhangi bir dildeki
    /// ad değişikliği tam olarak bir duyuru üretir — ad ve adres birlikte değişse de
    /// (AC-35, AC-36, D20).</para>
    /// </summary>
    internal Result Update(BranchDraft draft, Locale defaultLocale)
    {
        if (Status is BranchStatus.Closed)
        {
            return Result.Failure(new Error(ClosedReadOnly));
        }

        if (!SameTexts(Names, draft.Names))
        {
            Result names = ChangeDisplayNames(draft.Names, defaultLocale);

            if (names.IsFailure)
            {
                return names;
            }
        }

        Result addresses = ChangeAddresses(draft.Addresses, defaultLocale);

        if (addresses.IsFailure)
        {
            return addresses;
        }

        ChangePhone(draft.Phone);
        ChangeTimeZone(draft.TimeZone);
        ChangeSlug(draft.Slug);
        ApplyOptionals(draft);
        Version++;

        return Result.Success();
    }

    private void ApplyOptionals(BranchDraft draft)
    {
        // `0025` P1 · P4 — taslakta `null`: program DEĞİŞMEZ (alan yok ya da açık `null`, D11).
        // Yedi boş gün: siler (D4). Açmada `null` program yok demektir.
        if (draft.OpeningHours is { } openingHours)
        {
            OpeningHours = openingHours.IsEmpty ? null : openingHours;
        }

        // `0027` D13 — taslakta `null`: kayıtlar değişmez; liste (geçmişi düşmüş) yerine geçer.
        if (draft.SpecialHours is { } specialHours)
        {
            SpecialHours = specialHours.Days.Count == 0 ? null : specialHours;
        }

        if (draft.WhatsApp is { } whatsApp)
        {
            SetWhatsApp(whatsApp);
        }
        else
        {
            ClearWhatsApp();
        }

        if (draft.Email is { } email)
        {
            SetEmail(email);
        }
        else
        {
            ClearEmail();
        }

        if (draft.Coordinate is { } coordinate)
        {
            SetCoordinate(coordinate);
        }
        else
        {
            ClearCoordinate();
        }
    }

    private static bool SameTexts(LocalizedText left, LocalizedText right)
    {
        Dictionary<string, string> a = left.ToDictionary();
        Dictionary<string, string> b = right.ToDictionary();

        return a.Count == b.Count
            && a.All(pair => b.TryGetValue(pair.Key, out string? value) && string.Equals(value, pair.Value, StringComparison.Ordinal));
    }

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
        Version++;
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
        Version++;
        return Result.Success();
    }

    /// <summary>
    /// B5/AC-16 — son AKTİF şube kapatılamaz. Sayı dışarıdan verilir: bir şube kaç kardeşi
    /// olduğunu bilemez, ama invariant'ın agregatın dışına kaçması da gerekmez.
    ///
    /// <para>`0020` **D13** — kural TEK koşulludur: galeride BAŞKA aktif şube yoksa kapatılan
    /// şubenin durumu ne olursa olsun reddedilir. Eskiden yalnız "aktif ve başka aktif yok"
    /// reddediliyordu; son aktif şube önce geçici kapatılıp sonra kalıcı kapatılınca galerinin
    /// bütün şubeleri `Kapatılmış` olabiliyordu (AC-51).</para>
    /// </summary>
    internal Result Close(int otherActiveBranchCount)
    {
        if (Status is BranchStatus.Closed)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        if (otherActiveBranchCount == 0)
        {
            return Result.Failure(new Error(LastActiveBranch));
        }

        Status = BranchStatus.Closed;
        Version++;
        Raise(new BranchWasClosed(Id));

        return Result.Success();
    }
}
