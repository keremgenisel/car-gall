namespace CarGallery.Modules.Dealership.Contracts.Gallery;

/// <summary>
/// `0014` **D15** — galerinin ziyaretçiye dönük okuması bir İZİN LİSTESİDİR: yanıt yalnız
/// galerinin adını ve aktif şubelerin adını, adresini ve telefonunu taşır. Vergi ve sicil
/// numarası, sosyal bağlantılar, logo kimliği; şubenin e-postası, WhatsApp'ı, koordinatı,
/// slug'ı ve durumu burada YOKTUR — null olarak da yoktur (AC-14). Yasak listesi sonradan
/// eklenen bir alanı yakalamaz; izin listesi yakalar.
/// </summary>
public sealed record PublicGalleryDto(string CommercialName, IReadOnlyList<PublicBranchDto> Branches);

/// <summary>Telefon E.164 biçimindedir; uç onu bir <c>tel:</c> bağlantısına çevirir (AC-45).</summary>
public sealed record PublicBranchDto(string Name, string Address, string Phone);
