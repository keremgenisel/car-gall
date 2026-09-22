using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts.Sessions;
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

    internal FixedClock Saat { get; } = new(StaffBuilder.VarsayilanAn);

    internal IdentityOptions Secenekler { get; } = new()
    {
        AccessLifetime = ErisimSuresi,
        RefreshLifetime = YenilemeSuresi,
        FailedAttemptThreshold = Esik,
        LockoutDuration = KilitSuresi,
        MinimumPasswordLength = AsgariParolaUzunlugu,
        PasswordIterations = StaffBuilder.TestIterasyon,
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

    internal async Task<Result<Guid>> PersonelEkleAsync(AddStaffCommand komut)
    {
        await using IdentityDbContext context = database.CreateContext();

        return await Komutlar(context).AddAsync(komut, CancellationToken.None);
    }

    internal async Task<TSonuc> KomutAsync<TSonuc>(Func<StaffCommands, Task<TSonuc>> islem)
    {
        await using IdentityDbContext context = database.CreateContext();

        return await islem(Komutlar(context));
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

    private Pbkdf2PasswordHasher Ozetleyici() => new(Secenekler);

    private StaffCommands Komutlar(IdentityDbContext context) => new(
        context,
        IdentityDatabase.CreateUnitOfWork(context, Saat),
        Ozetleyici(),
        new FakeBranchValidator(),
        Localeler,
        Secenekler,
        Saat);
}
