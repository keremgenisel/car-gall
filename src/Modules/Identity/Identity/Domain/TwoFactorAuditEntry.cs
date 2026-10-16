using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// R12/D12 — kaydın olay türü. Veritabanında **kod** (int) olarak durur, çevrilmiş adı
/// durmaz (C7, Yasak Listesi 22).
/// </summary>
internal enum TwoFactorAuditEvent
{
    /// <summary>Kurulum onaylandı ve etkinleşti.</summary>
    Enrolled = 1,

    /// <summary>Personel kendi ikinci adımını kapattı (R15/D23).</summary>
    Disabled = 2,

    /// <summary>Yönetici sıfırladı (R6/D22).</summary>
    Reset = 3,

    /// <summary>Girişte bir kurtarma kodu kullanıldı (R5).</summary>
    RecoveryCodeUsed = 4,

    /// <summary>Kurtarma kodu kümesi yenilendi (R16/D24).</summary>
    RecoveryCodesRegenerated = 5,
}

/// <summary>
/// İkinci adımın kaydı (R12/D12). Kayıt `identity` şemasında **kendi tablosunda** yaşar;
/// üç seçenek ölçülmüştü — tüketicisi olmayan bir entegrasyon olayı, yapılandırılmış log
/// ve bir kayıt tablosu — ve kazanan üçüncüsü oldu: C9 yeni migration'ı bu şemaya
/// sınırlıyor ve kayıt zaten `Identity`'nin verisidir (C1).
///
/// **Başarısız denemeler buraya girmez** (R12): onları kilitlenme sayacı zaten taşıyor ve
/// her denemeyi yazmak tabloyu kaba kuvvetin büyüteci yapardı.
///
/// `ActorStaffId` kendi işlemlerinde personelin kendisidir, sıfırlamada **Yöneticidir**:
/// *"beklenmeyen bir kurulum görünür olsun"* (D6) ancak kimin yaptığı yazılıysa anlam
/// taşır.
/// </summary>
internal sealed class TwoFactorAuditEntry : Entity<TwoFactorAuditEntryId>
{
    private TwoFactorAuditEntry(
        TwoFactorAuditEntryId id,
        StaffId staffId,
        StaffId actorStaffId,
        TwoFactorAuditEvent auditEvent,
        DateTimeOffset occurredAt)
        : base(id)
    {
        StaffId = staffId;
        ActorStaffId = actorStaffId;
        Event = auditEvent;
        OccurredAt = occurredAt;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private TwoFactorAuditEntry()
        : base(default)
    {
    }

    /// <summary>Kaydın KONUSU olan personel.</summary>
    internal StaffId StaffId { get; private set; }

    /// <summary>İşlemi YAPAN personel; sıfırlamada Yönetici, kalanlarda personelin kendisi.</summary>
    internal StaffId ActorStaffId { get; private set; }

    internal TwoFactorAuditEvent Event { get; private set; }

    internal DateTimeOffset OccurredAt { get; private set; }

    internal static TwoFactorAuditEntry Record(
        StaffId staffId,
        StaffId actorStaffId,
        TwoFactorAuditEvent auditEvent,
        DateTimeOffset occurredAt) =>
        new(TwoFactorAuditEntryId.New(), staffId, actorStaffId, auditEvent, occurredAt);
}
