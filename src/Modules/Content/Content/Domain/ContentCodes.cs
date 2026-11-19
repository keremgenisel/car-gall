namespace CarGallery.Modules.Content.Domain;

/// <summary>
/// `0017` C13/D6 — dört sistem sayfasının KİMLİĞİ ve altlıktaki SIRASI kodda sabittir.
/// Kimlik slug'dan bağımsızdır: altlık ve talep formu sayfayı kimlikle bulur.
/// </summary>
internal static class SystemPageKeys
{
    internal const string About = "about";
    internal const string Privacy = "privacy";
    internal const string Cookies = "cookies";
    internal const string Faq = "faq";

    /// <summary>R21 — altlığın "Kurumsal" kolonundaki sıra: Hakkımızda, SSS, KVKK, Çerez.</summary>
    internal static IReadOnlyList<string> FooterOrder { get; } = [About, Faq, Privacy, Cookies];
}

/// <summary>`0017` R5 — dil başına yayın durumu. Veritabanında KOD olarak saklanır (C3).</summary>
internal enum PublicationStatus
{
    /// <summary><c>draft</c> — ziyaretçiye görünmez.</summary>
    Draft = 1,

    /// <summary><c>published</c> — o dilin adresinde görünür.</summary>
    Published = 2,
}

/// <summary>Yayın durumunun saklanan kodu (C3, AC-131).</summary>
internal static class PublicationStatusCodes
{
    internal const string Draft = "draft";
    internal const string Published = "published";

    /// <summary>Durum → kod.</summary>
    internal static string ToCode(PublicationStatus status) =>
        status == PublicationStatus.Published ? Published : Draft;

    /// <summary>Kod → durum; tanınmayan kod bozuk veridir.</summary>
    internal static PublicationStatus FromCode(string code) => code switch
    {
        Published => PublicationStatus.Published,
        Draft => PublicationStatus.Draft,
        _ => throw new InvalidOperationException($"Tanınmayan yayın durumu kodu: '{code}'."),
    };
}

/// <summary>
/// `0017` C5 — hata kodları. Yanıt çevrilmiş cümle taşımaz; uç kodu kendi kataloğundan
/// çevirir (AC-120).
/// </summary>
internal static class ContentErrors
{
    /// <summary>Bir veya daha fazla alan reddedildi; alanlar ve kodları parametrelerde.</summary>
    internal const string ValidationFailed = "content.validation_failed";
    internal const string FieldRequired = "content.field_required";
    internal const string TitleTooLong = "content.title_too_long";
    internal const string BodyTooLong = "content.body_too_long";
    internal const string QuestionTooLong = "content.question_too_long";
    internal const string AnswerTooLong = "content.answer_too_long";
    internal const string SlugInvalid = "content.slug_invalid";
    internal const string SlugReserved = "content.slug_reserved";
    internal const string SlugTaken = "content.slug_taken";
    internal const string LinkSchemeNotAllowed = "content.link_scheme_not_allowed";
    internal const string LocaleNotEnabled = "content.locale_not_enabled";
    internal const string PageNotFound = "content.page_not_found";
    internal const string FaqEntryNotFound = "content.faq_entry_not_found";
    internal const string SystemPageNotDeletable = "content.system_page_not_deletable";
    internal const string DescriptionTooLong = "content.description_too_long";
}
