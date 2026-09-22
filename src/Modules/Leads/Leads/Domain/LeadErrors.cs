using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Kararlı hata kodları: `&lt;alan&gt;.&lt;durum&gt;[.&lt;ayrıntı&gt;]`, snake_case
/// (docs/conventions.md § 5). Kod bir kez yayınlandıktan sonra ANLAMI DEĞİŞMEZ; gerekirse
/// yeni kod eklenir.
///
/// Burada yalnız KOD vardır, metin yoktur: API çevrilmiş doğrulama metni döndürmez, uç
/// kodu kendi kataloğundan çevirir (Yasak Listesi 23, C10, Bilinen tuzak 5).
/// <see cref="ForField"/> ile taşınan `field` parametresi ucun hangi alanı işaretleyeceğini
/// söyler (AC-40) — metin değil, yapısal parametredir.
/// </summary>
internal static class LeadErrors
{
    internal const string MissingName = "lead.missing_name";
    internal const string NameTooLong = "lead.name_too_long";
    internal const string MissingContactChannel = "lead.missing_contact_channel";
    internal const string InvalidPhone = "lead.invalid_phone";
    internal const string InvalidEmail = "lead.invalid_email";
    internal const string EmailTooLong = "lead.email_too_long";
    internal const string MessageTooLong = "lead.message_too_long";
    internal const string InvalidType = "lead.invalid_type";
    internal const string TypeNotAccepted = "lead.type_not_accepted";
    internal const string InvalidLocale = "lead.invalid_locale";
    internal const string ListingUnavailable = "lead.listing_unavailable";
    internal const string ListingReserved = "lead.listing_reserved";
    internal const string MissingIdempotencyKey = "lead.idempotency_key_missing";
    internal const string RateLimited = "lead.rate_limited";
    internal const string EmptyModelName = "lead.empty_model_name";

    internal static Error ForField(string code, string field) => Error.With(code, "field", field);
}
