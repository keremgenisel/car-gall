using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts.Sessions;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// `Dealership` bu test projesinin sınırının ötesindedir (docs/testing.md § 1) ve
/// sahtelenir: burada sınanan şey şubenin kendisi değil, `Identity`'nin geçersiz bir
/// şube kimliğini reddetmesidir.
/// </summary>
internal sealed class FakeBranchValidator : IBranchValidator
{
    internal static readonly Guid GecerliSubeId = Guid.Parse("0192c000-0000-7000-8000-000000000001");

    public Task<bool> ExistsAsync(Guid branchId, CancellationToken cancellationToken) =>
        Task.FromResult(branchId == GecerliSubeId);
}

/// <summary>
/// Süreler ve eşikler YAPILANDIRMADAN gelir (C20). Testler yapılandırmayı burada
/// sabitler ve doğruladıkları şey o değerdir, koda gömülü bir sabit değil.
/// </summary>
internal sealed class IdentityHarness(IdentityDatabase database)
{
    internal const int Esik = 5;
    internal const int AsgariParolaUzunlugu = 12;

    internal static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan ErisimSuresi = TimeSpan.FromMinutes(30);
    internal static readonly TimeSpan YenilemeSuresi = TimeSpan.FromDays(14);

    internal const string VarsayilanIstemci = "203.0.113.7";

    /// <summary>
    /// `0013` D9/D18/D19 — ikinci adımın sayıları da YAPILANDIRMADAN gelir; testler
    /// yapılandırılan değeri doğrular, koda gömülü bir sabiti değil (C20). Parametre
    /// kümesi (hane, pencere, algoritma) bunun TERSİ: o bir sözleşmedir ve `TimeBasedCode`
    /// içinde sabittir (C12/D16).
    /// </summary>
    internal const int KurtarmaKoduSayisi = 10;

    internal const string Kurulus = "Test Galeri";

    internal static readonly TimeSpan BekleyenKurulumSuresi = TimeSpan.FromMinutes(10);
    internal static readonly TimeSpan AraDurumSuresi = TimeSpan.FromMinutes(5);

    internal FixedClock Saat { get; } = new(StaffBuilder.VarsayilanAn);

    internal IdentityOptions Secenekler { get; } = new()
    {
        AccessLifetime = ErisimSuresi,
        RefreshLifetime = YenilemeSuresi,
        FailedAttemptThreshold = Esik,
        LockoutDuration = KilitSuresi,
        MinimumPasswordLength = AsgariParolaUzunlugu,
        PasswordIterations = StaffBuilder.TestIterasyon,
        RecoveryCodeCount = KurtarmaKoduSayisi,
        RecoveryCodeIterations = StaffBuilder.TestIterasyon,
        PendingEnrollmentLifetime = BekleyenKurulumSuresi,
        PendingLoginLifetime = AraDurumSuresi,
        TwoFactorIssuer = Kurulus,
    };

    internal IdentityLocaleOptions Localeler { get; } = new(
        Locale.Create("tr").Value,
        [Locale.Create("tr").Value, Locale.Create("ar").Value, Locale.Create("en").Value]);

    internal async Task<Result<LoginResult>> GirisAsync(
        string? kimlik,
        string? parola,
        string istemci = VarsayilanIstemci)
    {
        await using IdentityDbContext context = database.CreateContext();
        Login handler = new(context, Ozetleyici(), Secenekler, Saat);

        return await handler.HandleAsync(new LoginCommand(kimlik, parola, istemci), CancellationToken.None);
    }

    /// <summary>
    /// `0013` D15 — giriş artık **iki dallı** ve `Token` bekleyen dalda `null`. Oturum
    /// bekleyen testler tokeni buradan alır: dalın gerçekten bir oturum ürettiği tek bir
    /// yerde doğrulanır, çağıran yerlerde `!` ile varsayılmaz.
    ///
    /// Kurulumu olmayan Yönetici `TwoFactorSetupRequired` dalını alır ve o dal da gerçek
    /// bir oturum verir (D13) — `0006`'nın oturum kriterleri bu yüzden aynen geçerli.
    /// </summary>
    internal static string OturumTokeni(Result<LoginResult> sonuc)
    {
        Assert.True(sonuc.IsSuccess);
        Assert.NotNull(sonuc.Value.Session);

        return sonuc.Value.Token!;
    }

    internal async Task<Result<Guid>> PersonelEkleAsync(AddStaffCommand komut)
    {
        await using IdentityDbContext context = database.CreateContext();

        return await Komutlar(context).AddAsync(komut, CancellationToken.None);
    }

    /// <summary>
    /// Her çağrı KENDİ bağlamını açar: eşzamanlılık testleri iki ayrı isteği temsil eden
    /// iki ayrı bağlantıya ihtiyaç duyar. `guardOptions` yalnız kilit süresini ölçen
    /// testlerde verilir.
    /// </summary>
    internal async Task<TSonuc> KomutAsync<TSonuc>(
        Func<StaffCommands, Task<TSonuc>> islem,
        InvariantGuardOptions<IdentityDbContext>? guardOptions = null)
    {
        await using IdentityDbContext context = database.CreateContext();

        return await islem(Komutlar(context, guardOptions));
    }

    internal async Task<StaffSessionDto?> OturumCozAsync(string? token)
    {
        await using IdentityDbContext context = database.CreateContext();
        StaffSessionQueries queries = new(context, Secenekler, Saat);

        return await queries.ResolveAsync(token, CancellationToken.None);
    }

    internal async Task CikisAsync(Guid oturumId)
    {
        await using IdentityDbContext context = database.CreateContext();

        await new Logout(context, Saat).HandleAsync(oturumId, CancellationToken.None);
    }

    internal static AddStaffCommand EkleKomutu(
        string ad = "Ayşe Yılmaz",
        string kimlik = "ayse@ornek.com",
        string parola = "cok-guclu-parola",
        string rol = "manager",
        string? locale = "tr",
        Guid? subeId = null) =>
        new(ad, kimlik, parola, rol, locale, subeId);

    /// <summary>
    /// `0013` — ikinci adımın komut yüzeyi. Şifreleyici testlerde de **yapılandırmadan**
    /// gelen bir anahtarla kuruluyor (D3); `IkinciAdimTestAnahtari` onu üretiyor.
    /// </summary>
    internal async Task<TSonuc> IkinciAdimAsync<TSonuc>(Func<TwoFactorCommands, Task<TSonuc>> islem)
    {
        await using IdentityDbContext context = database.CreateContext();

        return await islem(new TwoFactorCommands(
            context,
            IdentityDatabase.CreateUnitOfWork(context, Saat),
            Ozetleyici(),
            Koruyucu,
            Secenekler,
            Saat));
    }

    /// <summary>
    /// Kurulumu baştan sona yapar ve sırrı döndürür. Testlerin saati SABİT olduğu için
    /// adım çakışması yok: D11'in tek kullanım kuralı burada ölçülebilir kalıyor
    /// (gerçek saatle koşan testlerde aynı pencerede ikinci kod reddedilir — D45).
    /// </summary>
    internal async Task<string> IkinciAdimKurAsync(Guid personelId) =>
        (await IkinciAdimKurVeKodlariAlAsync(personelId)).Sir;

    /// <summary>Kurtarma kodları da gereken testler için: küme yalnız onayda döner (D31).</summary>
    internal async Task<(string Sir, RecoveryCodeSet Kodlar)> IkinciAdimKurVeKodlariAlAsync(Guid personelId)
    {
        TwoFactorEnrollmentStarted baslangic = (await IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        Result<RecoveryCodeSet> onay = await IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, Kod(baslangic.Secret), CancellationToken.None));

        Assert.True(onay.IsSuccess);

        return (baslangic.Secret, onay.Value);
    }

    /// <summary>Sabit saatten o anın kodu; `adim` verilirse o adımın kodu.</summary>
    internal string Kod(string sirBase32, long? adim = null) => TimeBasedCode.Compute(
        TwoFactorSecret.FromBase32(sirBase32).Value.ToBytes(),
        adim ?? TimeBasedCode.StepAt(Saat.UtcNow));

    /// <summary>Girişin ikinci adımı; ara durum çerezinin taşıdığı opak değerle.</summary>
    internal async Task<Result<LoginResult>> IkinciAdimGecAsync(
        string? araDurumTokeni,
        string? kod,
        string istemci = VarsayilanIstemci)
    {
        await using IdentityDbContext context = database.CreateContext();
        TwoFactorLogin handler = new(context, Koruyucu, Secenekler, Saat);

        return await handler.HandleAsync(
            new TwoFactorVerifyCommand(araDurumTokeni, kod, istemci), CancellationToken.None);
    }

    internal async Task AraDurumSilAsync(string? araDurumTokeni)
    {
        await using IdentityDbContext context = database.CreateContext();

        await new TwoFactorLogin(context, Koruyucu, Secenekler, Saat)
            .DiscardAsync(araDurumTokeni, CancellationToken.None);
    }

    internal ISecretProtector Koruyucu { get; } = new AesGcmSecretProtector(IkinciAdimTestAnahtari.Base64);

    private Pbkdf2PasswordHasher Ozetleyici() => new(Secenekler);

    private StaffCommands Komutlar(IdentityDbContext context, InvariantGuardOptions<IdentityDbContext>? guardOptions = null) => new(
        context,
        IdentityDatabase.CreateUnitOfWork(context, Saat),
        Ozetleyici(),
        new FakeBranchValidator(),
        Localeler,
        Secenekler,
        guardOptions ?? InvariantGuardOptions<IdentityDbContext>.Default,
        Saat);
}
