using System.Security.Claims;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Endpoints.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Identity.Endpoints;

internal sealed record LoginRequest(string? LoginId, string? Password);

internal sealed record TwoFactorRequest(string? Code);

/// <summary>
/// D15 — girişin **ayrıştırıcılı** gövdesi. `status` hangi adımın geldiğini söyler;
/// `session` yalnız TAMAMLANMIŞ dalda doludur.
///
/// <para><b>Bekleyen dal kimlik TAŞIMAZ</b> (C13): elinde yalnız parola olan kişi,
/// ikinci adımı geçmeden personelin adını, rolünü ve dilini öğrenmez. Kurulum gereken
/// dalda da taşımaz — orada kapılı bir oturum zaten var ve kabuk durumu kapının
/// `403`'ünden öğrenir (D36).</para>
///
/// <para><b>Neden `4xx` değil:</b> uç 2xx dışını `ApiError`'a çeviriyor ve ekran
/// **normal bir adımı hata olarak** basardı; `401` ayrıca kabuğu giriş ekranına yollar
/// (C19/AC-27). **Bir adım, bir hata değildir.**</para>
///
/// <para><b>Neden iç içe:</b> tamamlanmış dal *bugünkü gövdeyi* olduğu gibi taşıyor;
/// alanları düzleştirip hepsini `null` yapılabilir kılmak, uçtaki tipi sözleşmenin
/// taşımadığı alanları taşıyormuş gibi gösterirdi (`docs/frontend.md` § 2).</para>
/// </summary>
internal sealed record LoginResponse(string Status, CurrentSessionResponse? Session)
{
    internal const string Completed = "completed";
    internal const string TwoFactorRequired = "two_factor_required";
    internal const string TwoFactorSetupRequired = "two_factor_setup_required";
}

/// <summary>
/// Oturumun yanıt gövdesi. Token BULUNMAZ: o yalnız HttpOnly çerezde taşınır (0006/AC-29).
///
/// <para><b>`StaffId` (0008 D1).</b> Ekranın listedeki KENDİ satırını tanıması gerekiyor:
/// "bu işlemden sonra panele erişemeyeceksiniz" uyarısı ancak böyle yazılabilir. `DisplayName`
/// tekil değil ve `LoginId` bu yanıtta hiç yok, yani başka güvenilir bir işaret kalmıyor.
/// Kimlik sunucuda zaten `ClaimTypes.NameIdentifier` olarak duruyor; yanıta konması yeni bir
/// uç değil, var olan bir yanıtın alanıdır.</para>
///
/// <para>Alan GİRİŞ yanıtında da görünüyor, yalnız `/me`'de değil: bu kayıt ikisi tarafından
/// paylaşılıyor ve uçtaki tek `CurrentSession` tipi ikisini birden tipliyor. Ayrı tutsaydık
/// tip, giriş yanıtının taşımadığı bir alanı taşıyormuş gibi gösterirdi — `docs/frontend.md`
/// § 2'nin "tip ile sözleşmenin ayrıştığı yer sessiz hata üreten yerdir" dediği durum.
/// Personelin kendi kimliğini kendisine söylemek bir sızıntı değil.</para>
/// </summary>
internal sealed record CurrentSessionResponse(
    Guid StaffId,
    string DisplayName,
    string Role,
    string PreferredLocale,
    bool MustChangePassword,
    /// <param name="TwoFactorEnrolled">
    /// D26 — ikinci adım durumu. Değer **kaynaktan** okunan claim'den gelir (D35), yani
    /// kurulumu biten personel aynı oturumda doğru durumu görür. Mockup'ın üç panelde
    /// çizdiği *"2FA açık"* rozeti veri olmadan basılamaz (R17).
    /// </param>
    bool TwoFactorEnrolled);

/// <summary>
/// Giriş ve çıkış. Bu uçlar yönetim kökünün (<see cref="AdminSurface.RoutePrefix"/>)
/// DIŞINDADIR ve bu bilinçlidir: giriş oturumsuz çalışmak zorundadır, oysa o kökün
/// altındaki her uç oturum ister (C18).
/// </summary>
internal static class SessionEndpoints
{
    internal const string RateLimitPolicy = "identity.login";

    internal static void MapSessionEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/identity/sessions", LoginAsync)
            .WithName("Login")
            .RequireRateLimiting(RateLimitPolicy)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // D5 — doğrulama ucu giriş ucunun hız sınırını PAYLAŞIR. Ayrı (ya da hiç) sınır,
        // parolayı ele geçirmiş saldırgana kod denemek için ikinci bir bütçe verirdi.
        // Politika adı aynı ve bölüm anahtarı da aynı (kaynak), yani bütçe tektir.
        builder.MapPost("/api/identity/sessions/two-factor", VerifyTwoFactorAsync)
            .WithName("VerifyTwoFactor")
            .RequireRateLimiting(RateLimitPolicy)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // D14 — "Kullanıcı değiştir" ara durumu SİLER. Oturumsuz çalışır: ara durumun
        // kendisi oturum değil (R13) ve silinmesi için kimlik doğrulaması istemek,
        // personeli silmek istediği durumun içinde kilitlerdi.
        builder.MapDelete("/api/identity/sessions/pending", DiscardPendingAsync)
            .WithName("DiscardPendingLogin")
            .Produces(StatusCodes.Status204NoContent);

        // R10 — zorunlu parola değişimi bekleyen personelin ÇIKIŞI çalışır.
        builder.MapDelete("/api/identity/sessions/current", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<Results<Ok<LoginResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        LoginAsync(
            LoginRequest request,
            HttpContext httpContext,
            Login handler,
            CancellationToken cancellationToken)
    {
        Result<LoginResult> result = await handler.HandleAsync(
            new LoginCommand(request.LoginId, request.Password, ResolveClientKey(httpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            return IdentityProblems.From(result.Error);
        }

        if (result.Value.Step is LoginStep.TwoFactorRequired)
        {
            // D4/AC-11 — oturum çerezi YAZILMAZ; yalnız kısa ömürlü ara durum çerezi.
            // Süre SATIRDAN geliyor: çerez ile satırın aynı anda bitmesi gerekiyor.
            PendingLoginCookie.Write(
                httpContext,
                result.Value.PendingToken!,
                result.Value.PendingExpiresAt!.Value);

            return TypedResults.Ok(new LoginResponse(LoginResponse.TwoFactorRequired, Session: null));
        }

        return TypedResults.Ok(WriteSession(httpContext, result.Value));
    }

    /// <summary>
    /// Girişin ikinci adımı: zamana bağlı kod ya da kurtarma kodu. Ara durum çerezini
    /// **yalnız bu uç** okur (D4).
    /// </summary>
    private static async Task<Results<Ok<LoginResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        VerifyTwoFactorAsync(
            TwoFactorRequest request,
            HttpContext httpContext,
            TwoFactorLogin handler,
            CancellationToken cancellationToken)
    {
        Result<LoginResult> result = await handler.HandleAsync(
            new TwoFactorVerifyCommand(
                PendingLoginCookie.Read(httpContext),
                request.Code,
                ResolveClientKey(httpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            // Ara durum düştüyse çerezi de temizliyoruz: ekran parola fazına dönüyor ve
            // elde kalan ölü bir çerez bir sonraki denemede yine aynı reddi üretirdi.
            if (result.Error.Code == IdentityErrors.TwoFactorPendingExpired)
            {
                PendingLoginCookie.Clear(httpContext);
            }

            return IdentityProblems.From(result.Error);
        }

        PendingLoginCookie.Clear(httpContext);

        return TypedResults.Ok(WriteSession(httpContext, result.Value));
    }

    private static async Task<NoContent> DiscardPendingAsync(
        HttpContext httpContext,
        TwoFactorLogin handler,
        CancellationToken cancellationToken)
    {
        await handler.DiscardAsync(PendingLoginCookie.Read(httpContext), cancellationToken);

        PendingLoginCookie.Clear(httpContext);

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Oturumu kuran iki dalın ortak sonu. Token yalnız çereze yazılır; gövdeye HİÇ
    /// girmez (C17, AC-29).
    ///
    /// Kurulum gereken dal da oturum çerezi alır (D13) ama gövdesi kimlik TAŞIMAZ:
    /// kabuk kurulum borcunu kapının `403`'ünden öğrenir (D36).
    /// </summary>
    private static LoginResponse WriteSession(HttpContext httpContext, LoginResult result)
    {
        SessionCookie.Write(httpContext, result.Token!, result.Session!.RefreshExpiresAt);

        return result.Step is LoginStep.TwoFactorSetupRequired
            ? new LoginResponse(LoginResponse.TwoFactorSetupRequired, Session: null)
            : new LoginResponse(
                LoginResponse.Completed,
                new CurrentSessionResponse(
                    result.Staff.Id.Value,
                    result.Staff.DisplayName,
                    StaffRoleCode.ToCode(result.Staff.Role),
                    result.Staff.PreferredLocale.Code,
                    result.Staff.MustChangePassword,
                    result.TwoFactorEnrolled));
    }

    private static async Task<NoContent> LogoutAsync(
        HttpContext httpContext,
        Logout handler,
        CancellationToken cancellationToken)
    {
        string? sessionId = httpContext.User.FindFirstValue(SessionAuthenticationHandler.SessionIdClaim);

        if (Guid.TryParse(sessionId, out Guid parsed))
        {
            await handler.HandleAsync(parsed, cancellationToken);
        }

        SessionCookie.Clear(httpContext);

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Kilitlenme sayacını bölen anahtar. Kaynak KİŞİSEL VERİDİR: yalnız sayaçta durur,
    /// loga yazılmaz (docs/conventions.md § 12).
    ///
    /// `X-Forwarded-For` burada OKUNMAZ: güvenilmeyen bir başlığa göre bölmek, sayacı
    /// istemcinin kendisinin kapatabilmesi demektir. Uygulama ters proxy arkasına
    /// konulursa gerçek adresi yazma işi boru hattının (Host) sorumluluğudur ve yalnızca
    /// açıkça listelenmiş proxy'lere güvenilerek yapılmalıdır.
    /// </summary>
    private static string ResolveClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
