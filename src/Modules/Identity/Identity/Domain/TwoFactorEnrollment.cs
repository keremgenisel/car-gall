using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// İkinci adım kurulumu (docs/domain.md — **İkinci Adım** / `TwoFactorEnrollment`).
/// Personel başına **tek** satır: kurulum ya bekliyordur ya onaylanmıştır.
///
/// Bekleyen kurulumun **ömrü vardır** ve yeniden başlatmak **yeni bir sır** üretir
/// (D18). İkisi de *"sır bir kez gösterilir"* kuralının parçasıdır: aynı sır ikinci kez
/// gösterilebildiği anda o kural yalandır, süresiz bekleyen bir sır ise uyuyan **ikinci
/// bir kimlik bilgisidir**.
///
/// Satır sırrı **şifreli** taşır (D3); şifrelemeyi `ISecretProtector` yapar ve anahtar
/// dağıtım yapılandırmasından gelir. Agregat şifrelemeyi bilmez — eline hazır gelen
/// metni saklar; aksi hâlde alan katmanı bir altyapı bağımlılığı kazanırdı.
/// </summary>
internal sealed class TwoFactorEnrollment : Entity<StaffId>
{
    private TwoFactorEnrollment(
        StaffId staffId,
        string secretCipher,
        DateTimeOffset createdAt,
        DateTimeOffset pendingExpiresAt)
        : base(staffId)
    {
        SecretCipher = secretCipher;
        CreatedAt = createdAt;
        PendingExpiresAt = pendingExpiresAt;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private TwoFactorEnrollment()
        : base(default) => SecretCipher = string.Empty;

    /// <summary>Şifrelenmiş doğrulama sırrı. Açık metin hiçbir kolonda durmaz (D3).</summary>
    internal string SecretCipher { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Bekleyen kurulumun son geçerlilik anı. Onaylandıktan sonra anlamı yoktur ve
    /// okunmaz; ayrı bir kolona taşımak satırı ikiye bölmeden aynı bilgiyi verirdi.
    /// </summary>
    internal DateTimeOffset PendingExpiresAt { get; private set; }

    internal DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// D11 — kabul edilmiş son adım. Toleransın açtığı 90 saniyelik pencerede aynı kodun
    /// ikinci kez geçmesini bu sayaç engelliyor (`TimeBasedCode.TryMatch`).
    /// </summary>
    internal long LastAcceptedStep { get; private set; }

    internal bool IsConfirmed => ConfirmedAt is not null;

    internal static TwoFactorEnrollment Start(
        StaffId staffId,
        string secretCipher,
        DateTimeOffset now,
        TimeSpan pendingLifetime) =>
        new(staffId, secretCipher, now, now.Add(pendingLifetime));

    /// <summary>
    /// Kurulum yeniden başlatıldığında satır YENİ sırla baştan kurulur; eski sır düşer
    /// (D18/AC-6). Onaylanmış bir kurulum buradan geçmez — çağıran `409` döndürür (AC-9).
    /// </summary>
    internal void Restart(string secretCipher, DateTimeOffset now, TimeSpan pendingLifetime)
    {
        SecretCipher = secretCipher;
        CreatedAt = now;
        PendingExpiresAt = now.Add(pendingLifetime);
        ConfirmedAt = null;
        LastAcceptedStep = 0;
    }

    internal bool IsPendingExpiredAt(DateTimeOffset now) => !IsConfirmed && now >= PendingExpiresAt;

    /// <summary>
    /// Doğrulanmamış kurulum ancak bir kodla etkinleşir (R3/AC-3, AC-4). Onay anında
    /// kabul edilen adım de saklanır: onay kodunun kendisi de yeniden oynatılamaz.
    /// </summary>
    internal Result Confirm(long acceptedStep, DateTimeOffset now)
    {
        if (IsConfirmed)
        {
            return Result.Failure(new Error(IdentityErrors.TwoFactorAlreadyEnrolled));
        }

        ConfirmedAt = now;
        LastAcceptedStep = acceptedStep;

        return Result.Success();
    }

    internal void RegisterAcceptedStep(long acceptedStep) => LastAcceptedStep = acceptedStep;
}
