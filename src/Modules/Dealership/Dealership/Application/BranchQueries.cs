using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// C3 — `Dealership` başka modüle senkron çağrı YAPMAZ. Bu sorgu yalnızca kendi verisini
/// okur; ziyaretçi locale'i parametre olarak gelir, başka bir modülden sorulmaz.
/// </summary>
internal sealed class BranchQueries(DealershipDbContext context, Locale defaultLocale)
    : IBranchQueries, IBranchContactQueries
{
    /// <summary>
    /// 0009 D2 — galeri bildiriminin hedefi. E-posta null olabilir (0003 zorunlu kılmıyor);
    /// kurulum adresine düşme kararı çağıranın, burası yalnız olguyu söyler.
    /// </summary>
    public async Task<BranchNotificationContactDto?> FindContactAsync(
        Guid branchId,
        string locale,
        CancellationToken cancellationToken)
    {
        // Projeksiyon SORGUYA yazılıyor, belleğe değil: DTO yalnız ad ve e-posta taşıyor,
        // oysa bütün agregat çekilseydi adres, koordinat, telefon, WhatsApp, saat dilimi ve
        // slug da süreç belleğine inerdi. Kardeş sorgu `LeadNotificationQueries` bunu zaten
        // böyle yapıyor; iki yüzeyin zıt davranması gerekçesiz bir tutarsızlıktı.
        var row = await context.Branches
            .AsNoTracking()
            .Where(item => item.Id == new BranchId(branchId))
            .Select(item => new { item.Id, item.Names, item.Email })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        Result<Locale> requested = Locale.Create(locale);
        Locale resolved = requested.IsSuccess ? requested.Value : defaultLocale;

        // `Names` jsonb kolonu; çözüm yine bellekte ama yalnız O kolon üzerinde.
        string name = row.Names.Resolve(resolved, defaultLocale) ?? string.Empty;

        return new BranchNotificationContactDto(row.Id.Value, name, row.Email?.Value);
    }

    /// <summary>
    /// `0012b` **D40** — adın BÜTÜN dilleri. Varsayılan locale'e düşme YOK ve bu bilinçli:
    /// çağıran bir haritayı kopyalıyor, bir metin göstermiyor. Eksik bir dili varsayılanla
    /// doldurmak, o dilde çeviri varmış gibi görünen bir satır üretirdi ve `0010b`'nin
    /// "eksik dil" göstergesi onu bir daha hiç yakalayamazdı.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>?> FindNamesAsync(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var row = await context.Branches
            .AsNoTracking()
            .Where(item => item.Id == new BranchId(branchId))
            .Select(item => new { item.Names })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : row.Names.ToDictionary().ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<BranchSummaryDto>> ListAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        Result<Locale> requested = Locale.Create(locale);
        Locale resolved = requested.IsSuccess ? requested.Value : defaultLocale;

        List<Branch> branches = await context.Branches
            .AsNoTracking()
            .OrderBy(branch => branch.Id)
            .ToListAsync(cancellationToken);

        return
        [
            .. branches.Select(branch => new BranchSummaryDto(
                branch.Id.Value,
                branch.Names.Resolve(resolved, defaultLocale) ?? string.Empty,
                // C11 — çevrilmiş durum adı değil KOD; çeviri uçta yapılır.
                branch.Status.ToString()))
        ];
    }

    public async Task<BranchSummaryDto?> FindAsync(
        Guid branchId,
        string locale,
        CancellationToken cancellationToken)
    {
        Branch? branch = await context.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), cancellationToken);

        if (branch is null)
        {
            return null;
        }

        Result<Locale> requested = Locale.Create(locale);
        Locale resolved = requested.IsSuccess ? requested.Value : defaultLocale;

        // Varsayılan locale'deki ad her zaman vardır (R2/AC-6 onu zorunlu kılıyor),
        // dolayısıyla bu yol hiçbir koşulda boş ad döndürmez.
        string name = branch.Names.Resolve(resolved, defaultLocale) ?? string.Empty;

        return new BranchSummaryDto(branch.Id.Value, name, branch.Status.ToString());
    }
}
