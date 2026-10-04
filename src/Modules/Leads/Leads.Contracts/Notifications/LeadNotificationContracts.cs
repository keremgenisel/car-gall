namespace CarGallery.Modules.Leads.Contracts.Notifications;

/// <summary>
/// 0009 D1 — bildirim gövdesini yazabilmek için gereken DAR okuma yüzeyi.
///
/// <para><b>Neden olay değil sorgu.</b> <c>LeadCaptured</c> kişisel alan taşımaz ve
/// taşımamalıdır: outbox satırı yayıncının şemasında <c>jsonb</c> olarak KALICIDIR ve bir
/// kez girdi mi KVKK silme talebi olay geçmişini de kapsar. Bu yüzden tüketici talebi
/// <c>LeadId</c> ile okur ve kişisel veriyi olaydan değil KAYNAKTAN alır — olayın kendi
/// doc comment'i bunu zaten öngörmüştü.</para>
///
/// <para><b>Neden bu kadar dar</b> (D7). Her alanın bir gerekçesi var; gerekçesi olmayan
/// alan burada YOKTUR:</para>
/// <list type="bullet">
/// <item><c>Email</c> — ziyaretçi karşılığının tek kanalı (R3). <b>Null olabilir:</b> 0005
/// R4 telefonu tek iletişim kanalı olarak kabul ediyor ve o hâlde karşılık gönderilmez,
/// "kanal yok" olarak kaydedilir (R6, AC-11).</item>
/// <item><c>SubmittedLocale</c> — karşılık talebin bırakıldığı dilde gider (L1, R3, AC-8).</item>
/// <item><c>CanonicalModelName</c> — bildirimin okunabilir olması için (R2). Kanonik Latin
/// addır; ziyaretçinin dilinde dondurulmuş bir ad, o dili okumayan çalışan için talebi
/// okunamaz yapardı.</item>
/// <item><c>BranchId</c> — galeri bildiriminin hedefini ve şube adını çözmek için; ikisi de
/// <c>Dealership</c>'ten gelir (D2).</item>
/// <item><c>ListingId</c> — detay bağlantısının slug'ını çözmek için; <c>VehicleCatalog</c>'tan
/// gelir (D5).</item>
/// </list>
///
/// <para><b>Ad ve telefon YOKTUR.</b> Ad hiçbir kriterde kullanılmıyor; telefon ise SMS
/// kanalı kapsam dışı olduğu için (X1) hiç gerekmiyor. İkisini de buraya koymak, kişisel
/// veriyi ihtiyaç olmadan bir modül sınırının ötesine taşımak olurdu (C5).</para>
/// </summary>
public sealed record LeadNotificationDto(
    Guid LeadId,
    string? Email,
    string SubmittedLocale,
    string CanonicalModelName,
    Guid BranchId,
    Guid ListingId);

public interface ILeadNotificationQueries
{
    /// <summary>
    /// Var olmayan talep için <c>null</c> döner. Çağıran bunu bir hata değil, işlenecek bir
    /// şey kalmadığı biçiminde okur: olay geldiği hâlde talep yoksa (silinmiş veya
    /// anonimleştirilmiş) gönderilecek bir mesaj da yoktur.
    /// </summary>
    Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken);
}
