using System.Security.Claims;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

/// <summary>
/// R7/D13 — **zorunlu rolde kurulumu olmayan personelin yalnızca kendi parolasını
/// değiştirme, kendi ikinci adım kurulumu ve çıkış istekleri çalışır; diğer her yönetim
/// isteği reddedilir.**
///
/// <para><b>Neden kapılı bir oturum veriliyor:</b> D4 *"ikinci adım geçilmeden oturum
/// çerezi yazılmaz"* diyor, ama kurulumu olmayan personelin geçebileceği bir ikinci adım
/// YOK. O hâlde parola doğru → oturum yok → ne parola değiştirilebilir ne kurulum
/// yapılabilir ve R7'nin *"önce kurar"*ı hiçbir yoldan yapılamazdı. Kurulumu olmayan
/// hesapta parola bugün **zaten tek faktör**; kapılı oturum mevcut duruma göre hiçbir şeyi
/// zayıflatmıyor. D4'ün reddi **kurulumu olan** hesap için aynen duruyor.</para>
///
/// <para>İzin verilen yollar <b>açıkça sayılır</b>; tersini saymak (yasaklananları
/// listelemek) bir sonraki panel spec'i yeni bir uç eklediğinde onu sessizce açık
/// bırakırdı — <see cref="PasswordChangeGate"/>'in kendi gerekçesi. **Çıkış burada
/// sayılmaz** çünkü buraya hiç uğramaz: `/api/identity/sessions/current` yönetim
/// kökünün DIŞINDADIR.</para>
///
/// <para><b>`/me` bilerek listede değil</b> (D36): kabuk kurulum borcunu bu kapının
/// döndüğü `403`'ün KODUNDAN öğrenir — `identity.password_change_required` ile birebir
/// aynı desen. Listeye okuma ucu eklemek, *"izin verilenler açıkça sayılır"* gerekçesini
/// her yeni ekranda yeniden pazarlık konusu yapardı.</para>
///
/// Karşılık `403`'tür, `401` değil: kimlik VARDIR, yalnız bu hâlde yetkisi yoktur. `401`
/// dönseydi uç giriş ekranına yönlendirir ve personel giriş–yönlendirme döngüsüne
/// girerdi (C19, AC-27).
/// </summary>
internal sealed class TwoFactorSetupGate(RequestDelegate next)
{
    internal const string OwnTwoFactorPath = $"{AdminSurface.RoutePrefix}/me/two-factor";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsBlocked(context))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = StatusCodes.Status403Forbidden,
            ["errorCode"] = IdentityErrors.TwoFactorSetupRequired,
        });
    }

    private static bool IsBlocked(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AdminSurface.RoutePrefix))
        {
            return false;
        }

        if (!StaffRoleCode.TryParse(context.User.FindFirstValue(ClaimTypes.Role), out StaffRole role)
            || !TwoFactorPolicy.IsRequiredFor(role))
        {
            return false;
        }

        if (context.User.FindFirstValue(SessionAuthenticationHandler.TwoFactorEnrolledClaim) == "true")
        {
            return false;
        }

        return !context.Request.Path.StartsWithSegments(PasswordChangeGate.OwnPasswordPath)
            && !context.Request.Path.StartsWithSegments(OwnTwoFactorPath);
    }
}
