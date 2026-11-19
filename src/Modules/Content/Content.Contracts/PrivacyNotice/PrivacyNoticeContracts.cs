namespace CarGallery.Modules.Content.Contracts.PrivacyNotice;

/// <summary>
/// `0019` D14 — "bu dilde KVKK aydınlatma metninin şu an yayındaki sürümü" okumasının iki
/// başarılı sonucu. Üçüncü sonuç HATADIR ve istisna olarak gelir: `privacy` sayfası yoksa ya
/// da dil yayında görünüp sürümü yoksa kurulum bozuktur ve bu "yayında değil" değildir.
/// </summary>
public sealed record PrivacyNoticeLookup
{
    private PrivacyNoticeLookup(Guid? versionId) => VersionId = versionId;

    /// <summary>Metin o dilde yayında değil (R5).</summary>
    public static PrivacyNoticeLookup NotPublished { get; } = new((Guid?)null);

    /// <summary>Yayındaki sürümün kimliği; metin yayında değilse <c>null</c>.</summary>
    public Guid? VersionId { get; }

    /// <summary>Metin o dilde yayında mı.</summary>
    public bool IsPublished => VersionId is not null;

    /// <summary>Metin o dilde yayında; talep bu sürüme bağlanır (R3).</summary>
    public static PrivacyNoticeLookup Published(Guid versionId) => new(versionId);
}

/// <summary>
/// `0019` D7 — yayınlanmış bir sürümün tam hâli: metin, dili, kayda geçtiği an ve başlangıç
/// sürümü olup olmadığı (R4, R7). Kişisel veri taşımaz (C4).
/// </summary>
public sealed record PrivacyNoticeVersionDto(
    Guid VersionId,
    string Locale,
    string Title,
    string Body,
    DateTimeOffset RecordedAt,
    bool IsInitial);

/// <summary>
/// `0019` — KVKK aydınlatma metninin yayınlanmış sürümlerinin okuma yüzeyi. Yalnız okur:
/// sürümü düzenleyen ya da silen bir üye yoktur (C5, AC-38).
/// </summary>
public interface IPrivacyNoticeQueries
{
    /// <summary>
    /// Verilen dilde şu an yayındaki sürüm ya da <see cref="PrivacyNoticeLookup.NotPublished"/>.
    /// Tek SQL komutudur ve hiçbir şey yazmaz (C7). Okunamayan ya da tutarsız durumda istisna
    /// atar (D14) — çağıran onu "yayında değildi"ye çevirmez.
    /// </summary>
    Task<PrivacyNoticeLookup> FindPublishedAsync(string locale, CancellationToken cancellationToken);

    /// <summary>Sürümün tam hâli; böyle bir sürüm yoksa <c>null</c> (R4).</summary>
    Task<PrivacyNoticeVersionDto?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken);
}
