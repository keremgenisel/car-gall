namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Kararlı hata kodları: `&lt;alan&gt;.&lt;durum&gt;[.&lt;ayrıntı&gt;]`, snake_case
/// (docs/conventions.md § 5). Burada yalnız KOD vardır, metin yoktur: API çevrilmiş
/// doğrulama metni döndürmez, uç kodu kendi kataloğundan çevirir (C8, Yasak Listesi 23).
/// </summary>
internal static class IdentityErrors
{
    /// <summary>
    /// R3/AC-5/AC-8 — giriş reddinin **TEK** kodu. Kimliğin kayıtlı olmadığı, parolanın
    /// yanlış olduğu, hesabın pasif olduğu ve hesabın **kilitli** olduğu ayrımı buradan
    /// geçmez. Kilitlenme için ayrı bir kod tanımlamak, "hangi e-postalar kayıtlı"
    /// sorusunu kilitlenme kanalından geri açardı (Tuzak 3 ve 7).
    /// </summary>
    internal const string InvalidCredentials = "identity.invalid_credentials";

    internal const string NoSession = "identity.no_session";
    internal const string PasswordChangeRequired = "identity.password_change_required";
    internal const string RateLimited = "identity.rate_limited";

    internal const string LoginIdTaken = "identity.login_id_taken";
    internal const string InvalidLoginId = LoginId.InvalidFormat;
    internal const string PasswordTooShort = "identity.password_too_short";
    internal const string CurrentPasswordInvalid = "identity.current_password_invalid";
    internal const string LastActiveManager = "identity.last_active_manager";
    internal const string BranchNotFound = "identity.branch_not_found";
    internal const string LocaleNotEnabled = "identity.locale_not_enabled";
    internal const string StaffNotFound = "identity.staff_not_found";
    internal const string AlreadyDeactivated = "identity.already_deactivated";
    internal const string EmptyDisplayName = "identity.empty_display_name";
}
