using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Identity.Tests.Integration;

[Collection(IdentityDatabaseCollection.Name)]
public sealed class PersonelYonetimiTests(IdentityDatabase database)
{
    [Fact(DisplayName = "0006/AC-31: Yönetici bir personel eklediğinde personel listede görünür ve verilen kimlikle giriş yapabilir")]
    public async Task Listelenir_VeGirisYapabilir_EklenenPersonel()
    {
        IdentityHarness harness = await KurAsync();

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            ad: "Mehmet Demir", kimlik: "mehmet@ornek.com", rol: "sales_advisor"));

        IReadOnlyList<StaffListItem> liste =
            await harness.KomutAsync(komutlar => komutlar.ListAsync(CancellationToken.None));

        Assert.Contains(liste, item => item.LoginId == "mehmet@ornek.com" && item.Role == "sales_advisor");
        Assert.True((await harness.GirisAsync("mehmet@ornek.com", "cok-guclu-parola")).IsSuccess);
    }

    /// <summary>
    /// Tekillik NORMALİZE kolon üzerindeki indekste yaşar. Gerçek PostgreSQL olmadan bu
    /// kriter kanıtlanamaz: in-memory bir sağlayıcı indeksi taklit eder (docs/testing.md § 1).
    /// </summary>
    [Fact(DisplayName = "0006/AC-12: `Ali@x.com` kayıtlıyken `ali@x.com` ile ikinci bir personel eklenemez")]
    public async Task Eklenemez_AyniKimlikFarkliYazimla()
    {
        IdentityHarness harness = await KurAsync();
        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "Ali@x.com", rol: "sales_advisor"));

        Result<Guid> ikinci = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "ali@x.com", rol: "sales_advisor"));

        Assert.True(ikinci.IsFailure);
        Assert.Equal(IdentityErrors.LoginIdTaken, ikinci.Error.Code);

        await using IdentityDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Staff.CountAsync(staff => staff.NormalizedLoginId == "ali@x.com"));
    }

    [Theory(DisplayName = "0006/AC-14: Geçerli biçimde olmayan bir e-posta ile personel eklenemez")]
    [InlineData("ayse")]
    [InlineData("@ornek.com")]
    [InlineData("ayse@ornek")]
    [InlineData("")]
    public async Task Eklenemez_GecersizEposta(string kimlik)
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: kimlik));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidLoginId, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-36: Yapılandırılan asgari uzunluktan kısa bir parola kabul edilmez")]
    public async Task Eklenemez_KisaParolayla()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "kisa@ornek.com",
            parola: new string('k', IdentityHarness.AsgariParolaUzunlugu - 1)));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.PasswordTooShort, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-36: Yapılandırılan asgari uzunluktan kısa bir parola kabul edilmez")]
    public async Task Eklenir_SinirdakiParolayla()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "sinir@ornek.com",
            parola: new string('k', IdentityHarness.AsgariParolaUzunlugu)));

        Assert.True(sonuc.IsSuccess);
    }

    [Fact(DisplayName = "0006/AC-38: Personel bir şubeye bağlanarak eklenebilir; var olmayan bir şube kimliğiyle eklenemez")]
    public async Task Eklenir_GecerliSubeyle()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "subeli@ornek.com", subeId: FakeBranchValidator.GecerliSubeId));

        Assert.True(sonuc.IsSuccess);

        await using IdentityDbContext context = database.CreateContext();
        Staff personel = await context.Staff.SingleAsync(staff => staff.Id == new StaffId(sonuc.Value));
        Assert.Equal(FakeBranchValidator.GecerliSubeId, personel.BranchId);
    }

    [Fact(DisplayName = "0006/AC-38: Personel bir şubeye bağlanarak eklenebilir; var olmayan bir şube kimliğiyle eklenemez")]
    public async Task Eklenemez_VarOlmayanSubeyle()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "hayali@ornek.com", subeId: Guid.CreateVersion7()));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.BranchNotFound, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-39: Şubesiz personel eklenebilir; şube zorunlu değildir")]
    public async Task Eklenir_Subesiz()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "subesiz@ornek.com", subeId: null));

        Assert.True(sonuc.IsSuccess);
    }

    [Theory(DisplayName = "0006/AC-40: Etkin locale listesinde olmayan bir tercih edilen dille personel eklenemez")]
    [InlineData("de")]
    [InlineData("TR")]
    [InlineData("zzzz")]
    public async Task Eklenemez_EtkinOlmayanDille(string locale)
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "dil@ornek.com", locale: locale));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LocaleNotEnabled, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-41: Tercih edilen dil verilmeden eklenen personele kurulum varsayılan dili atanır")]
    public async Task Atanir_VarsayilanDil_Verilmediginde()
    {
        IdentityHarness harness = await KurAsync();

        Result<Guid> sonuc = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "varsayilan@ornek.com", locale: null));

        Assert.True(sonuc.IsSuccess);

        await using IdentityDbContext context = database.CreateContext();
        Staff personel = await context.Staff.SingleAsync(staff => staff.Id == new StaffId(sonuc.Value));
        Assert.Equal("tr", personel.PreferredLocale.Code);
    }

    [Fact(DisplayName = "0006/AC-34: Personel kendi parolasını mevcut parolasını vererek değiştirir; mevcut parola yanlış verildiğinde değişiklik reddedilir")]
    public async Task Degistirir_KendiParolasini()
    {
        IdentityHarness harness = await KurAsync();
        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        Result sonuc = await harness.KomutAsync(komutlar => komutlar.ChangeOwnPasswordAsync(
            yoneticiId, "cok-guclu-parola", "yepyeni-parola-123", CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
        Assert.True((await harness.GirisAsync("yonetici@ornek.com", "yepyeni-parola-123")).IsSuccess);
    }

    [Fact(DisplayName = "0006/AC-34: Personel kendi parolasını mevcut parolasını vererek değiştirir; mevcut parola yanlış verildiğinde değişiklik reddedilir")]
    public async Task Reddedilir_MevcutParolaYanlissa()
    {
        IdentityHarness harness = await KurAsync();
        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        Result sonuc = await harness.KomutAsync(komutlar => komutlar.ChangeOwnPasswordAsync(
            yoneticiId, "yanlis-parola-123", "yepyeni-parola-123", CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.CurrentPasswordInvalid, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-35: Yönetici bir personelin parolasını sıfırlar; personel sıfırlanan parolayla giriş yaptığında kendisinden parola değiştirmesi istenir")]
    public async Task Sifirlar_VeZorunluDegisimIsaretler()
    {
        IdentityHarness harness = await KurAsync();
        Guid personelId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "danisman@ornek.com", rol: "sales_advisor"))).Value;

        await harness.KomutAsync(komutlar => komutlar.ChangeOwnPasswordAsync(
            personelId, "cok-guclu-parola", "ilk-degisiklik-12", CancellationToken.None));

        Result sifirlama = await harness.KomutAsync(komutlar => komutlar.ResetPasswordAsync(
            personelId, "yonetici-verdi-12", CancellationToken.None));

        Assert.True(sifirlama.IsSuccess);

        Result<LoginResult> giris = await harness.GirisAsync("danisman@ornek.com", "yonetici-verdi-12");

        Assert.True(giris.IsSuccess);
        Assert.True((await harness.OturumCozAsync(giris.Value.Token))!.MustChangePassword);
    }

    [Fact(DisplayName = "0006/AC-50: Personel kaydının döndüğü hiçbir yanıt parolayı veya parola özetini içermez")]
    public async Task IcermezParola_PersonelListesi()
    {
        IdentityHarness harness = await KurAsync();

        IReadOnlyList<StaffListItem> liste =
            await harness.KomutAsync(komutlar => komutlar.ListAsync(CancellationToken.None));

        string[] alanlar = [.. typeof(StaffListItem).GetProperties().Select(property => property.Name)];

        Assert.DoesNotContain(alanlar, ad => ad.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(alanlar, ad => ad.Contains("hash", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(liste);
    }

    /// <summary>
    /// AC-50'nin ikinci yarısı: özet yanıtta olmamakla kalmaz, veritabanından HİÇ
    /// ÇIKMAZ. Projeksiyon bellekte yapılsaydı yanıt yine doğru olurdu — bu yüzden
    /// alan adlarına bakan test bunu göremezdi; ölçülmesi gereken şey ÜRETİLEN SQL.
    ///
    /// C6 parola malzemesini mümkün olan en dar yerde tutmayı istiyor ve liste sorgusu
    /// için o yer "hiç" — özete ihtiyaç duymuyor.
    /// </summary>
    [Fact(DisplayName = "0006/AC-50: Personel listesi sorgusu parola özeti kolonunu veritabanından hiç çekmez")]
    public async Task CekmezOzetKolonunu_PersonelListesiSorgusu()
    {
        IdentityHarness harness = await KurAsync();

        YakalayanLogger logger = new();

        await using IdentityDbContext context = LogluContext(logger);

        StaffCommands komutlar = new(
            context,
            IdentityDatabase.CreateUnitOfWork(context, harness.Saat),
            new Pbkdf2PasswordHasher(harness.Secenekler),
            new FakeBranchValidator(),
            harness.Localeler,
            harness.Secenekler,
            harness.Saat);

        IReadOnlyList<StaffListItem> liste = await komutlar.ListAsync(CancellationToken.None);

        // Boş liste üzerinde her iddia bedava doğru olurdu.
        Assert.NotEmpty(liste);
        Assert.NotEmpty(logger.Kayitlar);

        // Kolon ÜRETİLEN SQL'de geçmiyor: `SELECT` listesine hiç girmedi.
        Assert.DoesNotContain(
            logger.Kayitlar,
            kayit => kayit.Contains("password_hash", StringComparison.OrdinalIgnoreCase));

        // Liste yine de gerçekten dolduruluyor — sorgu boşa çıkmış değil.
        Assert.All(liste, item => Assert.False(string.IsNullOrWhiteSpace(item.LoginId)));
    }

    private IdentityDbContext LogluContext(YakalayanLogger logger)
    {
        DbContextOptionsBuilder<IdentityDbContext> builder = new();

        builder
            .UseNpgsql(
                database.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseLoggerFactory(LoggerFactory.Create(logging =>
                logging.AddProvider(logger).SetMinimumLevel(LogLevel.Debug)));

        return new IdentityDbContext(builder.Options);
    }

    [Fact(DisplayName = "0006/AC-42: Galerideki son aktif Yönetici pasifleştirilemez; girişim reddedilir")]
    public async Task Reddedilir_SonAktifYoneticiyiPasiflestirme()
    {
        IdentityHarness harness = await KurAsync();
        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        Result sonuc = await harness.KomutAsync(k => k.DeactivateAsync(yoneticiId, CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-44: İki aktif Yöneticiden biri pasifleştirildiğinde işlem başarılı olur ve geriye bir aktif Yönetici kalır")]
    public async Task Basarili_IkiYoneticidenBiri()
    {
        IdentityHarness harness = await KurAsync();
        Guid ikinci = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"))).Value;

        Result sonuc = await harness.KomutAsync(k => k.DeactivateAsync(ikinci, CancellationToken.None));

        Assert.True(sonuc.IsSuccess);

        await using IdentityDbContext context = database.CreateContext();
        int aktifYonetici = await context.Staff.CountAsync(
            staff => staff.Role == StaffRole.Manager && staff.Status == StaffStatus.Active);

        Assert.Equal(1, aktifYonetici);
    }

    [Fact(DisplayName = "0006/AC-43: Galerideki son aktif Yöneticinin rolü başka bir role düşürülemez; girişim reddedilir")]
    public async Task Reddedilir_SonAktifYoneticininRolunuDusurme()
    {
        IdentityHarness harness = await KurAsync();
        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        Result sonuc = await harness.KomutAsync(
            k => k.ChangeRoleAsync(yoneticiId, "sales_advisor", CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, sonuc.Error.Code);
    }

    /// <summary>
    /// R12 — "aktif" durumu aktif olmak demektir. Kilitli Yönetici sayıya GİRER; aksi
    /// hâlde ikisinden biri kilitliyken diğerini pasifleştirmek galeriyi yöneticisiz
    /// bırakırdı.
    /// </summary>
    [Fact(DisplayName = "0006/AC-45: Geçici olarak kilitlenmiş bir Yönetici aktif sayılır: iki Yöneticiden biri kilitliyken diğeri pasifleştirilemez")]
    public async Task AktifSayilir_GeciciKilitliYonetici()
    {
        IdentityHarness harness = await KurAsync();
        Guid ikinci = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"))).Value;

        // İkinci Yöneticiyi kilitle.
        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            await harness.GirisAsync("ikinci@ornek.com", "yanlis-parola-123");
        }

        Assert.True((await harness.GirisAsync("ikinci@ornek.com", "cok-guclu-parola")).IsFailure);

        // Kilitli olan yine de AKTİF sayılır: birinciyi pasifleştirmek serbest kalır...
        Guid birinci = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))
            .Single(item => item.LoginId == "yonetici@ornek.com").StaffId;

        Assert.True((await harness.KomutAsync(k => k.DeactivateAsync(birinci, CancellationToken.None))).IsSuccess);

        // ...ama kilitli Yönetici tek başına kalınca artık düşürülemez.
        Result son = await harness.KomutAsync(k => k.DeactivateAsync(ikinci, CancellationToken.None));

        Assert.True(son.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, son.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-46: Bir Yönetici kendi hesabını pasifleştirebilir ve kendi rolünü düşürebilir — son aktif Yönetici olmadığı sürece")]
    public async Task Yapabilir_KendiHesabinda_SonAktifDegilse()
    {
        IdentityHarness harness = await KurAsync();
        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"));

        Guid birinci = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))
            .Single(item => item.LoginId == "yonetici@ornek.com").StaffId;

        Assert.True((await harness.KomutAsync(
            k => k.ChangeRoleAsync(birinci, "content_editor", CancellationToken.None))).IsSuccess);
    }

    /// <summary>
    /// R12/AC-42 — invariant EŞZAMANLILIK altında da tutar.
    ///
    /// Kilitsiz oku-karar-ver-yaz deseninde iki eşzamanlı pasifleştirme isteği, İKİSİ de
    /// "benden başka bir Yönetici var" okuyup ikisi de geçiyordu; sonuç sıfır aktif
    /// Yönetici, yani sistemin kalıcı olarak kapanmasıydı (Tuzak 1) — ve kurulum hesabı
    /// idempotent olduğu için (var olan hesaba dokunmaz) oradan dönüş yoktu.
    ///
    /// Yarış tek turda her zaman gerçekleşmez; test bu yüzden turu TEKRARLIYOR. Doğru
    /// koddaki sonuç her turda aynı: tam olarak biri geçer. Yanlış kodda bir turun
    /// kaçması kaçınılmaz değil — birkaç tur onu güvenilir biçimde yakalıyor.
    /// </summary>
    [Fact(DisplayName = "0006/AC-42: İki Yönetici aynı anda pasifleştirilmek istendiğinde yalnızca biri geçer; galeride her zaman en az bir aktif Yönetici kalır")]
    public async Task Korunur_SonAktifYonetici_EsZamanliPasiflestirmede()
    {
        for (int tur = 0; tur < 5; tur++)
        {
            IdentityHarness harness = await KurAsync();
            await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"));

            IReadOnlyList<StaffListItem> liste =
                await harness.KomutAsync(k => k.ListAsync(CancellationToken.None));

            Guid birinci = liste.Single(item => item.LoginId == "yonetici@ornek.com").StaffId;
            Guid ikinci = liste.Single(item => item.LoginId == "ikinci@ornek.com").StaffId;

            // İki AYRI istek, iki ayrı `DbContext` — `KomutAsync` her çağrıda kendi
            // bağlamını açar. Gerçek boru hattındaki iki eşzamanlı isteğin karşılığı.
            Task<Result> a = harness.KomutAsync(k => k.DeactivateAsync(birinci, CancellationToken.None));
            Task<Result> b = harness.KomutAsync(k => k.DeactivateAsync(ikinci, CancellationToken.None));

            Result[] sonuclar = await Task.WhenAll(a, b);

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));

            Result reddedilen = sonuclar.Single(sonuc => sonuc.IsFailure);
            Assert.Equal(IdentityErrors.LastActiveManager, reddedilen.Error.Code);

            // Asıl iddia: galeri açık kaldı.
            IReadOnlyList<StaffListItem> kalan =
                await harness.KomutAsync(k => k.ListAsync(CancellationToken.None));

            Assert.Single(kalan, item => item.Role == "manager" && item.Status == "active");
        }
    }

    private async Task<IdentityHarness> KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Result<Guid> sonuc = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", rol: "manager"));

        Assert.True(sonuc.IsSuccess);

        return harness;
    }
}
