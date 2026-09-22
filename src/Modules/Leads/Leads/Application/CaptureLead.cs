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
    string? IdempotencyKey,
    /// <summary>
    /// C17'nin sayacını bölen OPAK anahtar (bugün ziyaretçinin IP'si). Application onu
    /// yalnız bir dize olarak görür; nereden geldiği uç katmanın bilgisidir.
    /// </summary>
    string ClientKey);

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
    ILeadCaptureQuota quota,
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

        // Kolon `character varying(100)`; aşan değer doğrulanmadan geçerse PostgreSQL
        // patlar ve ziyaretçi 500 alıp talebini kaybeder. Sınır kolonun ÖNÜNDE duruyor.
        if (command.IdempotencyKey.Trim().Length > Lead.MaximumIdempotencyKeyLength)
        {
            return Result.Failure<Guid>(new Error(LeadErrors.IdempotencyKeyTooLong));
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

        // Fiyatı ÇÖZÜLEMEYEN ilan da talebe kaynak olamaz. "Fiyat sorunuz" bu değildir:
        // o, P4'te tanımlı bir iş durumudur ve buraya `PriceSnapshot.OnRequest` olarak
        // gelir. Çözülemeyen fiyatı "fiyat sorunuz"a düşürmek, talebe yanlış bir gerçeği
        // KALICI olarak yazmak olurdu (L2) — model adıyla aynı hizada davranıyoruz.
        if (listing.Price is null)
        {
            return Result.Failure<Guid>(new Error(LeadErrors.ListingUnavailable));
        }

        string? canonicalModelName =
            await modelNames.FindAsync(listing.VehicleModelId, cancellationToken);

        Result<VehicleSnapshot> snapshot = VehicleSnapshot.Create(
            listing.ListingId,
            canonicalModelName,
            listing.Price.Value);

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

        // AC-32 — bütçe BURADA, yazmanın hemen öncesinde tüketilir. Yukarıdaki her ret
        // (geçersiz tip, geçersiz telefon, çözülemeyen ilan, rezerve ilan) sayaca HİÇ
        // dokunmadan döndü: kriter "5 talep kaydedildikten sonra" diyor, "5 denemeden
        // sonra" değil.
        if (!quota.TryAcquire(command.ClientKey))
        {
            return Result.Failure<Guid>(new Error(LeadErrors.RateLimited));
        }

        context.Leads.Add(lead.Value);

        bool captured = false;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            captured = true;
        }
        catch (DbUpdateException exception) when (LeadPersistence.IsIdempotencyKeyConflict(exception))
        {
            // Eşzamanlı ikinci istek yukarıdaki okumayı geçmiş ama tekil indekse takılmış.
            // Kayıt oluşmadı; ilkinin kimliği döner (AC-35).
            context.ChangeTracker.Clear();

            Lead winner = await context.Leads
                .AsNoTracking()
                .SingleAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);

            return Result.Success(winner.Id.Value);
        }
        finally
        {
            // Kayıt oluşmadıysa bütçe harcanmamıştır — çakışan ikinci istek de,
            // beklenmeyen bir hata da ziyaretçinin hakkını yemez.
            if (!captured)
            {
                quota.Release(command.ClientKey);
            }
        }

        return Result.Success(lead.Value.Id.Value);
    }

}
