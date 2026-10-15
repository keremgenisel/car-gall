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

    /// <summary>
    /// D20 — **reddin iki kodu var, bir değil.** Bu kod yanlış, penceresi dolmuş ve
    /// yeniden oynatılmış kodun **ortak** karşılığıdır; ayırmak saldırgana yakaladığı
    /// kodun bir zamanlar geçerli olduğunu söylerdi. Kilitlenmiş hesabın ikinci adımı da
    /// bunu döner — `0006`'nın suskunluk kuralı (R3/AC-5) aynen sürer.
    /// </summary>
    internal const string TwoFactorInvalid = "identity.two_factor_invalid";

    /// <summary>
    /// D20 — ara durum düştü. `TwoFactorInvalid`'dan AYRI bir koddur, çünkü personelin
    /// yapacağı şey farklıdır: orada *"taze kod yaz"*, burada *"parolanı yeniden gir"*
    /// (docs/conventions.md § 5'in kendi mantığı, R10).
    /// </summary>
    internal const string TwoFactorPendingExpired = "identity.two_factor_pending_expired";

    internal const string TwoFactorNotEnrolled = "identity.two_factor_not_enrolled";
    internal const string TwoFactorAlreadyEnrolled = "identity.two_factor_already_enrolled";

    /// <summary>
    /// D13 — kurulumu olmayan hesabın KAPISI. `403`'tür, `401` değil: kimlik vardır,
    /// yalnız bu hâlde yetkisi yoktur. `401` dönseydi uç giriş ekranına yönlendirir ve
    /// personel giriş–yönlendirme döngüsüne girerdi (C19, AC-27) —
    /// <see cref="PasswordChangeRequired"/> ile birebir aynı gerekçe. Kabuk kurulum
    /// borcunu bu koddan öğrenir (D36).
    /// </summary>
    internal const string TwoFactorSetupRequired = "identity.two_factor_setup_required";

    /// <summary>D23 — zorunlu rolde kapatma yoktur; tek yol Yöneticinin sıfırlamasıdır.</summary>
    internal const string TwoFactorDisableNotAllowed = "identity.two_factor_disable_not_allowed";

    /// <summary>
    /// Şifreden çözülen değer Base32 değilse: bu bir kullanıcı hatası değil, veri
    /// bozulmasıdır (yanlış şifreleme anahtarı en olası sebep). Koda sahip tek yer
    /// `TwoFactorSecret.FromBase32`.
    /// </summary>
    internal const string CorruptTwoFactorSecret = "identity.corrupt_two_factor_secret";
}
