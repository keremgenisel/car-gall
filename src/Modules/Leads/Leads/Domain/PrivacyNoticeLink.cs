namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// `0019` D2 — talebin KVKK aydınlatma metniyle bağının üç durumu. Saklanan değer KODDUR
/// (P7): bugün bu bağı bir teknik kişi sorguyla okuyacak (D7) ve sayı değil ad okumalı.
/// </summary>
internal enum PrivacyNoticeStatus
{
    /// <summary><c>not_recorded</c> — bu kural gelmeden bırakılmış talep (R6). Uygulama bu değeri YAZMAZ.</summary>
    NotRecorded = 1,

    /// <summary><c>not_published</c> — talep anında metin o dilde yayında değildi (R5).</summary>
    NotPublished = 2,

    /// <summary><c>published</c> — talep anında yayındaki sürüme bağlı (R3).</summary>
    Published = 3,
}

/// <summary>Durumun saklanan kodu.</summary>
internal static class PrivacyNoticeStatusCodes
{
    internal const string NotRecorded = "not_recorded";
    internal const string NotPublished = "not_published";
    internal const string Published = "published";

    /// <summary>Durum → kod; tanımsız değer bir programlama hatasıdır ve sessizce yazılmaz.</summary>
    internal static string ToCode(PrivacyNoticeStatus status) => status switch
    {
        PrivacyNoticeStatus.NotRecorded => NotRecorded,
        PrivacyNoticeStatus.NotPublished => NotPublished,
        PrivacyNoticeStatus.Published => Published,
        _ => throw new InvalidOperationException($"Tanımsız aydınlatma metni durumu: {(int)status}."),
    };

    /// <summary>Kod → durum; tanınmayan kod bozuk veridir.</summary>
    internal static PrivacyNoticeStatus FromCode(string code) => code switch
    {
        NotRecorded => PrivacyNoticeStatus.NotRecorded,
        NotPublished => PrivacyNoticeStatus.NotPublished,
        Published => PrivacyNoticeStatus.Published,
        _ => throw new InvalidOperationException($"Tanınmayan aydınlatma metni durumu kodu: '{code}'."),
    };
}

/// <summary>
/// `0019` P8 — talep anında kurulan bağ. Yalnız iki fabrikası vardır: "kayıt tutulmuyordu"
/// uygulama kodundan KURULAMAZ, yalnız bu kuraldan önceki satırdan okunur (D15, AC-23).
/// </summary>
internal readonly record struct PrivacyNoticeLink
{
    private PrivacyNoticeLink(PrivacyNoticeStatus status, Guid? versionId)
    {
        Status = status;
        VersionId = versionId;
    }

    /// <summary>Durum.</summary>
    internal PrivacyNoticeStatus Status { get; }

    /// <summary>Sürüm kimliği; yalnız <see cref="PrivacyNoticeStatus.Published"/>'da dolu (D27).</summary>
    internal Guid? VersionId { get; }

    /// <summary>Metin talebin dilinde yayında değildi; başka dilin metnine düşülmez (D2).</summary>
    internal static PrivacyNoticeLink NotPublished() => new(PrivacyNoticeStatus.NotPublished, null);

    /// <summary>Talep anında yayındaki sürüm.</summary>
    internal static PrivacyNoticeLink Published(Guid versionId) => new(PrivacyNoticeStatus.Published, versionId);
}
