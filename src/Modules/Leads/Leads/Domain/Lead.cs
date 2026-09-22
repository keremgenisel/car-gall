using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain.Events;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Ziyaretçinin bıraktığı ilgi kaydı — sistemin ikinci çekirdek kavramı (docs/domain.md).
/// `Leads` bu verinin TEK yazma sahibidir (C1); aracın güncel fiyatına sahip değildir,
/// yalnız kendi kopyasına sahiptir.
///
/// Talep düştüğü anda üç şeyi KENDİ İÇİNDE taşır ve bunlar sonradan değişmez: hangi dilde
/// bırakıldığı (**L1**), o andaki araç bilgisi (**L2**) ve iletişim izni (**L7**).
/// Bu üçünün ortak özelliği sonradan telafi edilememeleridir — bu yüzden hiçbiri
/// opsiyonel, hiçbiri sonradan doldurulabilir değildir.
/// </summary>
internal sealed class Lead : AggregateRoot<LeadId>
{
    private Lead(
        LeadId id,
        LeadType type,
        ContactDetails contact,
        VehicleSnapshot vehicle,
        Guid branchId,
        Locale submittedLocale,
        bool consentGiven,
        string idempotencyKey,
        DateTimeOffset capturedAt)
        : base(id)
    {
        Type = type;
        Contact = contact;
        Vehicle = vehicle;
        BranchId = branchId;
        SubmittedLocale = submittedLocale;
        ConsentGiven = consentGiven;
        IdempotencyKey = idempotencyKey;
        CapturedAt = capturedAt;
        Status = LeadStatus.New;
    }

    internal LeadType Type { get; private set; }

    /// <summary>**L4** — her talep `Yeni` başlar; geçişler atama akışıyla gelir (X2).</summary>
    internal LeadStatus Status { get; private set; }

    /// <summary>KİŞİSEL alanlar (R13, **L6**). Boşaltılabilir; kayıt boşaltılamaz.</summary>
    internal ContactDetails Contact { get; private set; }

    /// <summary>İSTATİSTİKSEL alan: talep anındaki araç gerçeği (**L2**).</summary>
    internal VehicleSnapshot Vehicle { get; private set; }

    /// <summary>
    /// **L3** — talep, ilgilenilen aracın şubesine bağlanır. Modüller arası referans
    /// SOFT'tur: foreign key kısıtı yoktur (Yasak Listesi 2, docs/conventions.md § 9).
    /// </summary>
    internal Guid BranchId { get; private set; }

    /// <summary>**L1** — bırakıldığı dil. Bugün kaydedilmezse yarın geriye dönük üretilemez.</summary>
    internal Locale SubmittedLocale { get; private set; }

    /// <summary>**L7** — izin bir KAYITTIR; verilmemiş olması da kaydedilir (AC-25).</summary>
    internal bool ConsentGiven { get; private set; }

    /// <summary>
    /// R17 — aynı anahtarla gelen ikinci istek yeni kayıt üretmez. Ağ hatası isteğin
    /// sunucuya ulaşmasından SONRA olduysa hız sınırı bunu yakalamaz: iki istek eşiğin
    /// çok altındadır (Bilinen tuzak 8).
    /// </summary>
    internal string IdempotencyKey { get; private set; }

    internal DateTimeOffset CapturedAt { get; private set; }

    /// <summary>
    /// R12/AC-8 — oluşan talep `Yeni` durumundadır. Takas TANINIR ama bu spec'te KABUL
    /// EDİLMEZ (R3, AC-10, Karar D3): ayrımı burada yapmak, tanınmayan bir tiple tanınan
    /// ama kabul edilmeyen bir tipin ziyaretçiye farklı karşılık vermesini mümkün kılar.
    /// </summary>
    internal static Result<Lead> Capture(
        LeadType type,
        ContactDetails contact,
        VehicleSnapshot vehicle,
        Guid branchId,
        Locale submittedLocale,
        bool consentGiven,
        string idempotencyKey,
        DateTimeOffset capturedAt)
    {
        if (!LeadTypePolicy.IsAccepted(type))
        {
            return Result.Failure<Lead>(LeadErrors.ForField(LeadErrors.TypeNotAccepted, "leadType"));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<Lead>(new Error(LeadErrors.MissingIdempotencyKey));
        }

        Lead lead = new(
            LeadId.New(),
            type,
            contact,
            vehicle,
            branchId,
            submittedLocale,
            consentGiven,
            idempotencyKey.Trim(),
            capturedAt);

        lead.Raise(new LeadWasCaptured(lead.Id));

        return Result.Success(lead);
    }

    /// <summary>
    /// R13/AC-29, docs/domain.md **L6** — kişisel alanlar boşaltılır; KAYIT SİLİNMEZ ve
    /// istatistiksel alanları (tip, şube, locale, tarih, anlık görüntü) bozulmadan kalır.
    ///
    /// Bu eylemi çalıştıracak bir yönetim yüzeyi bu spec'te YOKTUR (X4). Yetenek yine de
    /// burada, çünkü R13 verinin anonimleştirilebilir BİÇİMDE durmasını istiyor ve
    /// AC-29 bunu "boşaltıldığında" diye ölçüyor — ölçülebilmesi için yapılabilir olması
    /// gerekir.
    /// </summary>
    internal void Anonymize() => Contact = Contact.Erase();

    internal bool IsAnonymized() => Contact.IsErased();
}
