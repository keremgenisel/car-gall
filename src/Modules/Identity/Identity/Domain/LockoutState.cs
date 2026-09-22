using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// R6/AC-6/AC-8/AC-9 — başarısız giriş sayacı.
///
/// Sayaç **hesap ve kaynak birlikte** anahtarlanır. Yalnız hesaba bağlı bir kilit,
/// e-postasını bilen birinin o kişiyi kasten dışarıda tutmasına izin verirdi — son
/// Yönetici dâhil. Farklı bir kaynaktan gelen doğru parola, o kaynak kilitli değilse
/// kabul edilir (AC-9).
///
/// Anahtar **kayıtlı olmayan** kimlikler için de tutulur (R3, AC-8): sayaç yalnız var
/// olan hesaplar için işletilseydi, kilitlenme cevabının gelip gelmemesi hangi
/// e-postaların kayıtlı olduğunu tam olarak sızdırırdı (Tuzak 7). Bu yüzden bu tip
/// `Staff`'ın parçası DEĞİL, kendi başına bir kayıttır.
///
/// Kaynak anahtarı KİŞİSEL VERİDİR ve yalnız bu sayaçta durur; loga yazılmaz
/// (docs/conventions.md § 12).
/// </summary>
internal sealed class LockoutState
{
    private LockoutState(string normalizedLoginId, string clientKey)
    {
        NormalizedLoginId = normalizedLoginId;
        ClientKey = clientKey;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private LockoutState()
        : this(string.Empty, string.Empty)
    {
    }

    internal string NormalizedLoginId { get; private set; }

    internal string ClientKey { get; private set; }

    internal int FailedAttempts { get; private set; }

    internal DateTimeOffset? LockedUntil { get; private set; }

    internal static LockoutState For(LoginId loginId, string clientKey) =>
        new(loginId.Normalized, clientKey);

    /// <summary>
    /// Ayrıştırılamayan bir giriş kimliği için de sayaç gerekir (R3): aksi hâlde "biçimi
    /// bozuk" girişler sayaca hiç dokunmaz ve sonsuz deneme kanalı açılırdı.
    /// </summary>
    internal static LockoutState ForKey(string normalizedKey, string clientKey) =>
        new(normalizedKey, clientKey);

    internal bool IsLockedAt(DateTimeOffset now) => LockedUntil is not null && now < LockedUntil;

    /// <summary>
    /// Eşik ve süre yapılandırmadan gelir; koda gömülü sabit yoktur (C20). Kriterler
    /// **yapılandırılan** değeri doğrular.
    /// </summary>
    internal void RegisterFailure(DateTimeOffset now, int threshold, TimeSpan lockoutDuration)
    {
        // Kilit süresi dolmuşsa sayaç sıfırdan başlar: eski bir kilitten arta kalan sayı,
        // dönen personeli tek yanlış denemede yeniden kilitlerdi.
        if (LockedUntil is not null && now >= LockedUntil)
        {
            FailedAttempts = 0;
            LockedUntil = null;
        }

        FailedAttempts++;

        if (FailedAttempts >= threshold)
        {
            LockedUntil = now.Add(lockoutDuration);
        }
    }

    /// <summary>Başarılı giriş sayacı sıfırlar; kilit izi bırakmaz.</summary>
    internal void Clear()
    {
        FailedAttempts = 0;
        LockedUntil = null;
    }

    internal Result EnsureNotLocked(DateTimeOffset now) => IsLockedAt(now)
        ? Result.Failure(new Error(IdentityErrors.InvalidCredentials))
        : Result.Success();
}
