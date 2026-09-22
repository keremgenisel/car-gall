namespace CarGallery.Modules.Identity.Contracts;

/// <summary>
/// Yönetim yüzeyinin sınırı (C18). Burada sabit olarak yayınlanmasının nedeni ileriye
/// dönüktür: talep paneli, araç paneli ve içerik paneli kendi spec'leriyle geldiğinde
/// uçlarını **aynı köke** asacaklar ve aynı korumaya girecekler. Kök adı her modülde
/// yeniden yazılsaydı, bir sonraki panel kendi kökünü açar ve C18'in rota tarayan testi
/// onu hiç görmezdi.
/// </summary>
public static class AdminSurface
{
    /// <summary>
    /// Altındaki **her** endpoint oturum gerektirir ve bu, tek tek uçları sayan bir
    /// testle değil, rotaları TARAYAN bir host testiyle doğrulanır (C18, AC-23).
    /// </summary>
    public const string RoutePrefix = "/api/admin";

    /// <summary>Oturum gerektiren her yönetim ucu bu politikayı taşır.</summary>
    public const string AuthenticatedPolicy = "admin.authenticated";

    /// <summary>Yalnız Yönetici rolünün yapabildiği işlemler (R9, AC-25, AC-26, AC-28).</summary>
    public const string ManagerPolicy = "admin.manager";

    /// <summary>Kimlik doğrulama şemasının adı.</summary>
    public const string AuthenticationScheme = "cargallery.session";

    /// <summary>Oturum çerezinin adı. Uç bu çerezi OKUYAMAZ (HttpOnly) — C17, AC-29.</summary>
    public const string SessionCookieName = "cargallery.session";
}
