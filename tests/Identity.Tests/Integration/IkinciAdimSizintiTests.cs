using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// C2'nin iki yarısı: sır ve kurtarma kodu **loga** girmez, **açık metin olarak
/// saklanmaz**. İkisi ayrı yüzeydir ve ayrı ölçülür — `0006`'nın parola için verdiği
/// kararın (C6, AC-51) ikinci adım karşılığı.
///
/// Bu, gözle bakılarak doğrulanacak bir şey değil: EF Core parametreli sorguları
/// loglayabilir ve bir gün biri tanılama için hassas veri loglamasını açarsa sır doğrudan
/// log dosyasına düşer. Testler loglama AÇIK ve YAKALANIYORken koşuyor.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimSizintiTests(IdentityDatabase database)
{
    private const string Kimlik = "yonetici@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0013/AC-47: Doğrulama sırrı ve kurtarma kodları **hiçbir log kaydında** görünmez")]
    public async Task Gorunmez_LogKayitlarinda()
    {
        await database.ResetAsync();

        YakalayanLogger logger = new();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(personelId);

        // Sırrın ve kodun geçtiği yolların HEPSİ loglanan bir bağlamda koşuyor: kurulum
        // okuması, giriş ve ikinci adım doğrulaması.
        await using IdentityDbContext context = LogluContext(logger);

        await context.TwoFactorEnrollments.AsNoTracking().ToListAsync();
        await context.RecoveryCodes.AsNoTracking().ToListAsync();

        TwoFactorLogin ikinciAdim = new(context, harness.Koruyucu, harness.Secenekler, harness.Saat);
        await ikinciAdim.HandleAsync(
            new TwoFactorVerifyCommand("olmayan-ara-durum", kodlar.Codes[0], IdentityHarness.VarsayilanIstemci),
            CancellationToken.None);

        Assert.NotEmpty(logger.Kayitlar);

        // Önce KOŞULUN kurulduğu ölçülüyor: en az bir parametreli komut loglandı ve
        // parametre değerleri maskelenmedi. Bu iddia olmadan `EnableSensitiveDataLogging`
        // sessizce kaldırılabilir ve test, aradığı şeyi arayamadığı hâlde yeşil kalır —
        // kartın kapanış turunda ölçülen dış bulgu #6 tam buydu.
        Assert.Contains(
            logger.Kayitlar,
            kayit => kayit.Contains("Parameters=[@", StringComparison.Ordinal)
                && !kayit.Contains("='?'", StringComparison.Ordinal));

        Assert.DoesNotContain(logger.Kayitlar, kayit => kayit.Contains(sir, StringComparison.Ordinal));
        Assert.DoesNotContain(
            logger.Kayitlar,
            kayit => kayit.Contains(kodlar.Codes[0], StringComparison.OrdinalIgnoreCase));

        // Normalize edilmiş hâli de aranıyor: kod tireler atılarak özetleniyor ve bir
        // sızıntı tam o biçimde olurdu.
        Assert.DoesNotContain(
            logger.Kayitlar,
            kayit => kayit.Contains(
                RecoveryCodeText.Normalize(kodlar.Codes[0]), StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "0013/AC-48: Doğrulama sırrı veritabanında **açık metin durmaz**")]
    public async Task DurmazAcikMetin_Veritabaninda()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        string sir = await harness.IkinciAdimKurAsync(personelId);

        await using IdentityDbContext context = database.CreateContext();

        // Kolon HAM hâliyle okunuyor: EF dönüşümünden geçen bir okuma, şifrelemenin
        // varlığını değil yokluğunu gizleyebilirdi.
        List<string> satirlar = await context.Database
            .SqlQuery<string>($"SELECT secret_cipher AS \"Value\" FROM identity.two_factor_enrollments")
            .ToListAsync();

        string satir = Assert.Single(satirlar);

        Assert.DoesNotContain(sir, satir, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("aes-256-gcm$", satir, StringComparison.Ordinal);

        // Şifreleme gerçekten geri döndürülebilir: aynı anahtarla çözülen değer sırrın
        // kendisi (C2a). Aksi hâlde "şifreli görünen" bozuk bir kolon da yeşil kalırdı.
        Assert.Equal(sir, harness.Koruyucu.Unprotect(satir).Value);
    }

    [Fact(DisplayName = "0013/AC-50: Kurtarma kodları veritabanında **tek yönlü özetlenmiş** durur")]
    public async Task Ozetlenmis_KurtarmaKodlari()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        RecoveryCodeSet kodlar = (await harness.IkinciAdimKurVeKodlariAlAsync(personelId)).Kodlar;

        await using IdentityDbContext context = database.CreateContext();
        List<string> satirlar = await context.Database
            .SqlQuery<string>($"SELECT code_hash AS \"Value\" FROM identity.recovery_codes")
            .ToListAsync();

        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, satirlar.Count);

        foreach (string kod in kodlar.Codes)
        {
            Assert.DoesNotContain(satirlar, satir => satir.Contains(kod, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                satirlar,
                satir => satir.Contains(RecoveryCodeText.Normalize(kod), StringComparison.OrdinalIgnoreCase));
        }

        // Özet PAROLA özetinin biçimini taşıyor (C2b, `0006` C6 deseni) ve kendi
        // maliyetini içinde saklıyor.
        Assert.All(satirlar, satir => Assert.StartsWith("pbkdf2-sha256$", satir, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0013/AC-51: Doğrulama sırrı ve kurtarma kodu hiçbir hata yanıtında bulunmaz")]
    public async Task Tasimaz_HataGovdesi_SirVeKod()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(personelId);

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        // Üç ayrı red: yanlış kod, tükenmiş ara durum ve zaten kurulmuş hesap.
        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        List<Error> redler =
        [
            (await harness.IkinciAdimGecAsync(araDurum, "000000")).Error,
            (await harness.IkinciAdimGecAsync("olmayan-ara-durum", kodlar.Codes[0])).Error,
            (await harness.IkinciAdimAsync(
                komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Error,
        ];

        foreach (Error red in redler)
        {
            string govde = red.Code + string.Join(
                ';', red.Parameters?.Select(parametre => $"{parametre.Key}={parametre.Value}") ?? []);

            Assert.DoesNotContain(sir, govde, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(kodlar.Codes[0], govde, StringComparison.OrdinalIgnoreCase);
        }
    }

    private IdentityDbContext LogluContext(YakalayanLogger logger)
    {
        DbContextOptionsBuilder<IdentityDbContext> builder = new();

        builder
            .UseNpgsql(
                database.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseLoggerFactory(LoggerFactory.Create(logging =>
                logging.AddProvider(logger).SetMinimumLevel(LogLevel.Debug)))
            // Hassas veri loglaması AÇIK: bu testin ölçtüğü senaryo tam olarak "biri bir
            // gün tanılama için bunu açarsa sır loga düşer mi" sorusudur ve kapalıyken EF
            // parametreleri `'?'` olarak maskeler, yani test sızıntıyı ARAYAMAZ. Alttaki
            // `ConfigureWarnings` yalnız bu satır yüzünden doğan uyarıyı bastırır —
            // ikisi birlikte anlamlıdır, biri olmadan diğeri ölü satırdır.
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning));

        return new IdentityDbContext(builder.Options);
    }
}
