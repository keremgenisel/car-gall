using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>`0020` R9 — panelin şube girdisi; değerler ham hâliyle, dil anahtarları ham kod.</summary>
internal sealed record BranchFormInput(
    IReadOnlyDictionary<string, string?>? Names,
    IReadOnlyDictionary<string, string?>? Addresses,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? TimeZone,
    string? Slug,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>
/// Şubenin yazma yolu. `0007` B5 korumasını (kalıcı kapatma) yazmıştı ve "şube yönetimi
/// spec'i geldiğinde koruma hazır olur" diyordu; `0020` o spec'tir (`0003` X1) ve uçları
/// yalnız Yönetici politikasının arkasında açar (D2).
///
/// <para>Her yazma bir sürüm taşır: okunan sürüm güncel değilse <c>concurrency.lost</c> (R16,
/// D17 — `409`). Duyurular iş verisiyle aynı <c>SaveChanges</c>'ta outbox'a yazılır (C2):
/// bütün yazmalar <see cref="DealershipUnitOfWork"/>'ten geçer.</para>
/// </summary>
internal sealed class BranchCommands(
    DealershipDbContext context,
    DealershipUnitOfWork unitOfWork,
    InvariantGuardOptions<DealershipDbContext> guardOptions,
    DealershipLocales locales)
{
    internal const string BranchNotFound = "branch.not_found";

    /// <summary>`0020` D19 — kısaltma galeri genelinde, kapatılmış şubeler dâhil tekildir.</summary>
    internal const string SlugTaken = "branch.slug_taken";

    /// <summary>`0020` R9 — yeni şube aktif açılır ve açılış duyurusu üretir (AC-21, AC-22).</summary>
    internal async Task<Result<(Guid Id, int Version)>> OpenAsync(
        BranchFormInput input,
        CancellationToken cancellationToken)
    {
        Result<BranchDraft> draft = await ParseAsync(input, null, cancellationToken);

        if (draft.IsFailure)
        {
            return Result.Failure<(Guid, int)>(draft.Error);
        }

        Result<Branch> opened = Branch.Open(draft.Value, locales.Default);

        if (opened.IsFailure)
        {
            return Result.Failure<(Guid, int)>(opened.Error);
        }

        context.Branches.Add(opened.Value);

        Result kayit = await SaveAsync(cancellationToken);

        return kayit.IsSuccess
            ? Result.Success((opened.Value.Id.Value, opened.Value.Version))
            : Result.Failure<(Guid, int)>(kayit.Error);
    }

    /// <summary>`0020` R10 — aktif ya da geçici kapalı şubenin bütün alanları (AC-34–AC-42).</summary>
    internal async Task<Result<int>> UpdateAsync(
        Guid branchId,
        int version,
        BranchFormInput input,
        CancellationToken cancellationToken)
    {
        Branch? branch = await FindAsync(branchId, cancellationToken);
        Error? hata = Precheck(branch, version);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        if (branch!.Status is BranchStatus.Closed)
        {
            // Kapatılmış şube salt okunurdur (D21); alan hatası göstermek, düzeltilebilir bir
            // şey varmış gibi davranmak olurdu.
            return Result.Failure<int>(new Error(Branch.ClosedReadOnly));
        }

        Result<BranchDraft> draft = await ParseAsync(input, branchId, cancellationToken);

        if (draft.IsFailure)
        {
            return Result.Failure<int>(draft.Error);
        }

        Result sonuc = branch.Update(draft.Value, locales.Default);

        if (sonuc.IsFailure)
        {
            return Result.Failure<int>(sonuc.Error);
        }

        Result kayit = await SaveAsync(cancellationToken);

        return kayit.IsSuccess ? Result.Success(branch.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>`0020` R11 — son aktif şubede de serbest (`0003` D3, AC-44).</summary>
    internal Task<Result<int>> CloseTemporarilyAsync(Guid branchId, int version, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, version, branch => branch.CloseTemporarily(), cancellationToken);

    /// <summary>`0020` R11 — yalnız geçici kapalıdan (AC-46, AC-54).</summary>
    internal Task<Result<int>> ReopenAsync(Guid branchId, int version, CancellationToken cancellationToken) =>
        TransitionAsync(branchId, version, branch => branch.Reopen(), cancellationToken);

    /// <summary>
    /// B5 — son aktif şube kapatılamaz. Okuma, karar ve yazma tek transaction içinde ve
    /// kilit altında; gerekçesi `InvariantGuard`'ta. Sürüm denetimi yapmaz; `0007`'nin
    /// komut düzeyindeki testleri bu imzayı kullanır.
    /// </summary>
    internal async Task<Result> CloseAsync(Guid branchId, CancellationToken cancellationToken) =>
        await CloseAsync(branchId, null, cancellationToken);

    /// <summary>
    /// `0020` R12, R13 — panelin kalıcı kapatması. Sürüm KİLİTTEN SONRA okunan agregata karşı
    /// denetlenir: önce okunsaydı bekleyen istek kilit açıldığında bayat bir kopyayla karar
    /// verirdi. Başarıda yeni sürüm döner.
    /// </summary>
    internal async Task<Result<int>> CloseAsync(Guid branchId, int? version, CancellationToken cancellationToken)
    {
        int yeniSurum = 0;

        Result sonuc = await InvariantGuard.ExecuteAsync(
            context,
            ActiveBranches,
            branchId,
            async (otherActiveBranchCount, ct) =>
            {
                // Agregat KİLİTTEN SONRA okunuyor: önce okunsaydı, bekleyen istek kilit
                // açıldığında bayat bir kopyayla karar verirdi ve `Closed` kontrolü
                // sessizce atlanırdı.
                Branch? branch = await context.Branches
                    .SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), ct);

                if (branch is null)
                {
                    return Result.Failure(new Error(BranchNotFound));
                }

                if (version is not null && branch.Version != version)
                {
                    return Result.Failure(new Error(ConcurrencyErrors.Lost));
                }

                Result result = branch.Close(otherActiveBranchCount);

                if (result.IsFailure)
                {
                    return result;
                }

                Result kayit = await SaveAsync(ct);

                yeniSurum = branch.Version;
                return kayit;
            },
            guardOptions,
            cancellationToken);

        return sonuc.IsSuccess ? Result.Success(yeniSurum) : Result.Failure<int>(sonuc.Error);
    }

    private async Task<Result<int>> TransitionAsync(
        Guid branchId,
        int version,
        Func<Branch, Result> transition,
        CancellationToken cancellationToken)
    {
        Branch? branch = await FindAsync(branchId, cancellationToken);
        Error? hata = Precheck(branch, version);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        Result sonuc = transition(branch!);

        if (sonuc.IsFailure)
        {
            return Result.Failure<int>(sonuc.Error);
        }

        Result kayit = await SaveAsync(cancellationToken);

        return kayit.IsSuccess ? Result.Success(branch!.Version) : Result.Failure<int>(kayit.Error);
    }

    private Task<Branch?> FindAsync(Guid branchId, CancellationToken cancellationToken) =>
        context.Branches.SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), cancellationToken);

    private static Error? Precheck(Branch? branch, int version)
    {
        if (branch is null)
        {
            return new Error(BranchNotFound);
        }

        return branch.Version == version ? null : new Error(ConcurrencyErrors.Lost);
    }

    /// <summary>
    /// Dil süzgeci, alan kuralları ve kısaltma tekilliği. Tekillik kaydetmeden ÖNCE
    /// sorulur; yarışta kaydetme anındaki indeks ihlali de aynı alan koduna çevrilir
    /// (<see cref="SaveAsync"/>).
    /// </summary>
    private async Task<Result<BranchDraft>> ParseAsync(
        BranchFormInput input,
        Guid? self,
        CancellationToken cancellationToken)
    {
        FieldErrors dilHatalari = new();
        Dictionary<Locale, string?> adlar = locales.Filter(input.Names, "name", dilHatalari);
        Dictionary<Locale, string?> adresler = locales.Filter(input.Addresses, "address", dilHatalari);

        if (dilHatalari.Any)
        {
            return Result.Failure<BranchDraft>(dilHatalari.ToError());
        }

        Result<BranchDraft> draft = BranchDraft.Parse(
            new BranchInput(
                adlar,
                adresler,
                input.Phone,
                input.WhatsApp,
                input.Email,
                input.TimeZone,
                input.Slug,
                input.Latitude,
                input.Longitude),
            locales.Default);

        if (draft.IsFailure)
        {
            return draft;
        }

        BranchSlug slug = draft.Value.Slug;
        IQueryable<Branch> ayniKisaltma = context.Branches.AsNoTracking().Where(item => item.Slug == slug);

        if (self is not null)
        {
            // AC-40 — şubenin kendi kısaltması tekillik hatası değildir.
            BranchId kendisi = new(self.Value);
            ayniKisaltma = ayniKisaltma.Where(item => item.Id != kendisi);
        }

        bool alinmis = await ayniKisaltma.AnyAsync(cancellationToken);

        return alinmis
            ? Result.Failure<BranchDraft>(FieldErrors.Single("slug", SlugTaken))
            : draft;
    }

    private async Task<Result> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error(ConcurrencyErrors.Lost));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // D19 — bu tabloda tek tekil indeks kısaltmanınki; ihlal sunucu hatası değil
            // alan hatasıdır (C5).
            return Result.Failure(FieldErrors.Single("slug", SlugTaken));
        }
    }

    /// <summary>
    /// B5'in "aktif" tanımı: yalnızca <see cref="BranchStatus.Active"/>. `TemporarilyClosed`
    /// sayılmaz — şubenin KENDİ durumu geçici de olsa kapalıdır. (`Identity`'deki "geçici
    /// kilitli Yönetici aktif sayılır" kuralı farklı bir şeydir: orada kilit ayrı bir
    /// tabloda yaşayan, hesabın durumuna dokunmayan geçici bir engeldir.)
    ///
    /// Yüklem `const`'tur ve değer parametredir; dizeye gömülmez (bkz. `InvariantLock`).
    /// </summary>
    private static readonly InvariantLock ActiveBranches = new(
        DealershipDbContext.Schema,
        "branches",
        "status = {0}",
        [(int)BranchStatus.Active]);
}
