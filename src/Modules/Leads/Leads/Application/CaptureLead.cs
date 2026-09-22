using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Ziyaretçinin bıraktığı talebin tel üzerindeki hâli. Tip ve locale KOD olarak gelir,
/// çevrilmiş adıyla değil (Yasak Listesi 22).
/// </summary>
internal sealed record CaptureLeadCommand(
    string? ListingSlug,
    string? LeadType,
    string? Locale,
    string? FullName,
    string? Phone,
    string? Email,
    string? Message,
    bool ConsentGiven,
    string? IdempotencyKey);

/// <summary>
/// R1–R17'nin tek uygulama yolu. Sıra rastgele değil: önce ziyaretçinin kendi girdisi
/// (ucuz, tamamı bellekte), sonra ilanın çözülmesi (veritabanı), en sonda yazma. Ters
/// sırada çalışan bir doğrulama, geçersiz bir formu her denemede iki sorguya mal ederdi.
///
/// İlan çözülemiyorsa <b>tek</b> hata kodu döner: "hiç yok" ile "vitrinde görünmüyor"
/// arasında ayrım yapan bir dal bu metotta YOKTUR (R9, AC-14).
/// </summary>
internal sealed class CaptureLead(
    LeadsDbContext context,
    LeadsUnitOfWork unitOfWork,
    IListingSnapshotSource listings,
    ICanonicalModelNameSource modelNames,
    LeadLocaleOptions locales,
    IClock clock)
{
    internal async Task<Result<Guid>> HandleAsync(
        CaptureLeadCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Result.Failure<Guid>(new Error(LeadErrors.MissingIdempotencyKey));
        }

        if (!LeadTypeCode.TryParse(command.LeadType, out LeadType type))
        {
            return Result.Failure<Guid>(LeadErrors.ForField(LeadErrors.InvalidType, "leadType"));
        }

        // R3/AC-10 — takas TANINIR ama kabul edilmez; ziyaretçi "böyle bir tip yok"
        // demeyen ayrı bir karşılık alır.
        if (!LeadTypePolicy.IsAccepted(type))
        {
            return Result.Failure<Guid>(LeadErrors.ForField(LeadErrors.TypeNotAccepted, "leadType"));
        }

        if (!locales.TryResolve(command.Locale, out Locale locale))
        {
            return Result.Failure<Guid>(LeadErrors.ForField(LeadErrors.InvalidLocale, "locale"));
        }

        Result<ContactDetails> contact = ContactDetails.Create(
            command.FullName,
            command.Phone,
            command.Email,
            command.Message);

        if (contact.IsFailure)
        {
            return Result.Failure<Guid>(contact.Error);
        }

        // R17/AC-35 — aynı anahtarla gelen ikinci istek YENİ KAYIT OLUŞTURMAZ. Var olanı
        // döndürmek, uçtaki "tekrar dene"nin ikinci bir talep yaratmasını engeller
        // (Bilinen tuzak 8). Yarışı bu okuma değil, tekil indeks kapatır — aşağıda.
        string idempotencyKey = command.IdempotencyKey.Trim();

        Lead? existing = await context.Leads
            .AsNoTracking()
            .SingleOrDefaultAsync(lead => lead.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            return Result.Success(existing.Id.Value);
        }

        ListingSnapshot? listing = await listings.FindAsync(command.ListingSlug ?? string.Empty, cancellationToken);

        if (listing is null)
        {
            return Result.Failure<Guid>(new Error(LeadErrors.ListingUnavailable));
        }

        // R2/AC-12 — `Rezerve` ilan vitrinde GÖRÜNÜR (R6), bu yüzden ayrı bir karşılık
        // vermek bilgi sızdırmaz: ziyaretçi zaten rozeti görüyor.
        if (listing.IsReserved)
        {
            return Result.Failure<Guid>(new Error(LeadErrors.ListingReserved));
        }

        string? canonicalModelName =
            await modelNames.FindAsync(listing.VehicleModelId, cancellationToken);

        Result<VehicleSnapshot> snapshot = VehicleSnapshot.Create(
            listing.ListingId,
            canonicalModelName,
            listing.Price);

        if (snapshot.IsFailure)
        {
            // Adı çözülemeyen ilan talebe kaynak olamaz: adsız bir anlık görüntü
            // sonradan doldurulamayacak bir boşluktur (R7). Ziyaretçi açısından bu,
            // ilanın çözülememesiyle aynı durumdur.
            return Result.Failure<Guid>(new Error(LeadErrors.ListingUnavailable));
        }

        Result<Lead> lead = Lead.Capture(
            type,
            contact.Value,
            snapshot.Value,
            listing.BranchId,
            locale,
            command.ConsentGiven,
            idempotencyKey,
            clock.UtcNow);

        if (lead.IsFailure)
        {
            return Result.Failure<Guid>(lead.Error);
        }

        context.Leads.Add(lead.Value);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIdempotencyKeyConflict(exception))
        {
            // Eşzamanlı ikinci istek yukarıdaki okumayı geçmiş ama tekil indekse takılmış.
            // Kayıt oluşmadı; ilkinin kimliği döner (AC-35).
            context.ChangeTracker.Clear();

            Lead winner = await context.Leads
                .AsNoTracking()
                .SingleAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);

            return Result.Success(winner.Id.Value);
        }

        return Result.Success(lead.Value.Id.Value);
    }

    private static bool IsIdempotencyKeyConflict(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("ux_leads_idempotency_key", StringComparison.Ordinal) == true;
}
