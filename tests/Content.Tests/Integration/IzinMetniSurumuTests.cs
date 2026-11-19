using System.Data.Common;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>
/// `0019` — KVKK aydınlatma metninin yayınlanmış sürümleri: ne zaman doğar (AC-1…AC-14),
/// canlı metin değişmezi (AC-15), okuma sözleşmesi (AC-26, AC-50), eşzamanlılık (AC-36) ve
/// değişmezlik (AC-39…AC-41). Gerçek PostgreSQL; saat her işlemden önce ilerletilir ki
/// "en son sürüm" anı tekil olsun ve AC-15 işaretçiye güvenmeden ölçebilsin.
/// </summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class IzinMetniSurumuTests(ContentDatabase database) : IAsyncLifetime
{
    private static readonly Guid Privacy = Icerik.Sistem(SystemPageKeys.Privacy);

    private static readonly PageTextInput Tr1 = new("tr", "KVKK Aydınlatma Metni", "Kişisel verileriniz 6698 sayılı Kanun kapsamında işlenir.", "kvkk-aydinlatma");
    private static readonly PageTextInput En1 = new("en", "Privacy Notice", "Your personal data is processed under Law No. 6698.", "privacy-notice");
    private static readonly PageTextInput Ar1 = new("ar", "إشعار الخصوصية", "تتم معالجة بياناتك الشخصية وفق القانون.", "ishaar-al-khususiya");

    private readonly Icerik icerik = new(database);

    /// <summary>AC-15'in koştuğu işlem dizileri: AC-1…AC-14'ün her biri.</summary>
    public static TheoryData<string> Diziler { get; } = new()
    {
        "AC-1", "AC-2", "AC-3", "AC-4", "AC-5", "AC-6", "AC-7", "AC-8", "AC-9", "AC-10", "AC-11", "AC-12", "AC-13", "AC-14",
    };

    /// <inheritdoc />
    public Task InitializeAsync() => database.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "0019/AC-1: privacy'nin tr dili başlık, gövde ve slug'ı dolu taslakken yayına alındığında, dili tr olan ve başlığı ile gövdesi o anki metinle birebir aynı olan bir yayınlanmış sürüm oluşur; kayda geçtiği an, sabit saatle kurulan testte işlemi yürüten saatin (IClock) değeridir")]
    public Task Dogar_TaslaktanYayina() => Dizi1Async();

    [Fact(DisplayName = "0019/AC-2: Yayındaki tr'de gövde değiştirilip kaydedildiğinde yeni bir sürüm oluşur ve önceki sürümün başlığı, gövdesi ve anı değişmeden kalır")]
    public Task Dogar_GovdeDegisince() => Dizi2Async();

    [Fact(DisplayName = "0019/AC-3: Yayındaki tr'de yalnız başlık değiştirildiğinde yeni bir sürüm oluşur")]
    public Task Dogar_BaslikDegisince() => Dizi3Async();

    [Fact(DisplayName = "0019/AC-4: Yayındaki tr'de yalnız slug değiştirildiğinde yeni sürüm oluşmaz")]
    public Task Dogmaz_YalnizSlugDegisince() => Dizi4Async();

    [Fact(DisplayName = "0019/AC-5: Taslaktaki ar'da metin kaydedildiğinde sürüm oluşmaz")]
    public Task Dogmaz_TaslakDildeKayit() => Dizi5Async();

    [Fact(DisplayName = "0019/AC-6: tr ve en ikisi de yayındayken tek bir kayıt isteğiyle iki dilin gövdesi değiştirildiğinde tr için bir ve en için bir olmak üzere iki sürüm oluşur")]
    public Task Dogar_DilBasinaBir_TekKayittaIkiDil() => Dizi6Async();

    [Fact(DisplayName = "0019/AC-7: tr ve en ikisi de yayındayken istek iki dili de taşır ve yalnız en gövdesi değişirse (tr başlık ve gövdesi aynen gönderilir) yalnız bir en sürümü oluşur; tr sürümü oluşmaz (D16)")]
    public Task Dogmaz_DegismeyenDil() => Dizi7Async();

    [Fact(DisplayName = "0019/AC-8: Yayındaki tr gövdesi yalnız sonuna eklenen bir boşlukla kaydedildiğinde değişmiş sayılır ve yeni sürüm oluşur; sürümün gövdesi sondaki boşluğu taşır (D16)")]
    public Task Dogar_SondakiBoslukFarki() => Dizi8Async();

    [Fact(DisplayName = "0019/AC-9: Yayındaki tr yayından kaldırıldığında sürüm oluşmaz ve var olan tr sürümleri okunabilir kalır")]
    public Task Dogmaz_YayindanKaldirinca() => Dizi9Async();

    [Fact(DisplayName = "0019/AC-10: Yayından kaldırılan tr metni değiştirilmeden yeniden yayına alındığında yeni bir sürüm oluşur")]
    public Task Dogar_YenidenYayinda() => Dizi10Async();

    [Fact(DisplayName = "0019/AC-11: Zaten yayındaki tr'ye yayına alma çağrısı başarılı döner ama sürüm oluşturmaz (D17)")]
    public Task Dogmaz_ZatenYayindakiDileYayin() => Dizi11Async();

    [Fact(DisplayName = "0019/AC-12: about, cookies, faq sistem sayfalarında ve bir serbest sayfada yayına alma ve yayındaki dilde kaydetme sürüm oluşturmaz")]
    public Task Dogmaz_DigerSayfalarda() => Dizi12Async();

    [Fact(DisplayName = "0019/AC-13: Bayat sürüm belirteciyle (409) ya da eksik zorunlu alanla reddedilen kayıt ve yayına alma sürüm oluşturmaz")]
    public Task Dogmaz_ReddedilenIstekte() => Dizi13Async();

    [Fact(DisplayName = "0019/AC-14: Yayındaki tr'de başka bir sayfanın slug'ıyla yapılan ve slug çakışmasıyla reddedilen kayıt — çakışma ister ön denetimde ister veritabanının tekil indeksinde yakalansın — sürüm oluşturmaz (D26, Bilinen tuzak 5)")]
    public Task Dogmaz_SlugCakismasinda() => Dizi14Async();

    [Theory(DisplayName = "0019/AC-15: privacy hangi dilde yayındaysa o dilin en son sürümünün başlığı ve gövdesi canlı metinle birebir aynıdır; bu, kartın migration'ından sonra ve AC-1…AC-14'ün her işlem dizisinin sonunda doğrulanır")]
    [MemberData(nameof(Diziler))]
    public async Task Esittir_EnSonSurumCanliMetne_HerDizidenSonra(string dizi)
    {
        await (dizi switch
        {
            "AC-1" => Dizi1Async(),
            "AC-2" => Dizi2Async(),
            "AC-3" => Dizi3Async(),
            "AC-4" => Dizi4Async(),
            "AC-5" => Dizi5Async(),
            "AC-6" => Dizi6Async(),
            "AC-7" => Dizi7Async(),
            "AC-8" => Dizi8Async(),
            "AC-9" => Dizi9Async(),
            "AC-10" => Dizi10Async(),
            "AC-11" => Dizi11Async(),
            "AC-12" => Dizi12Async(),
            "AC-13" => Dizi13Async(),
            "AC-14" => Dizi14Async(),
            _ => throw new ArgumentOutOfRangeException(nameof(dizi), dizi, "Tanınmayan dizi."),
        });

        await using ContentDbContext context = database.CreateContext();
        await CanliMetin.DogrulaAsync(context);
    }

    /// <summary>Sıfır vakayla koşan bir Theory yeşildir ve kapı sessizce açılır (`docs/testing.md` § 3).</summary>
    [Fact(DisplayName = "0019/AC-15: AC-15'in vaka listesi AC-1…AC-14'ün on dördünü de taşır")]
    public void Tasir_OnDortDizi() =>
        Assert.Equal(Enumerable.Range(1, 14).Select(n => $"AC-{n}"), ((IEnumerable<object[]>)Diziler).Select(row => (string)row[0]));

    [Fact(DisplayName = "0019/AC-17: Başlangıç sürümü okunduğunda başlangıç sürümü olduğu ayırt edilir; editörün yayınıyla doğan sürüm başlangıç sürümü olarak görünmez")]
    public async Task GorunmezBaslangic_EditorSurumu()
    {
        await YayindaAsync(Tr1);
        Guid id = (await SurumlerAsync("tr")).Single().Id;

        PrivacyNoticeVersionDto? surum = await OkuAsync(q => q.FindVersionAsync(id, CancellationToken.None));

        Assert.False(surum!.IsInitial);
    }

    [Fact(DisplayName = "0019/AC-26: IPrivacyNoticeQueries.FindVersionAsync var olan bir sürüm kimliği için dili, başlığı, gövdeyi, kayda geçtiği anı ve başlangıç sürümü olup olmadığını döner; var olmayan bir kimlik için null döner (D26)")]
    public async Task Doner_SurumunTamHali_YoksaNull()
    {
        await YazAsync(Ar1);
        DateTimeOffset an = await YayinlaAsync("ar");
        Guid id = (await SurumlerAsync("ar")).Single().Id;

        PrivacyNoticeVersionDto? surum = await OkuAsync(q => q.FindVersionAsync(id, CancellationToken.None));

        Assert.Equal(new PrivacyNoticeVersionDto(id, "ar", Ar1.Title!, Ar1.Body!, an, false), surum);
        Assert.Null(await OkuAsync(q => q.FindVersionAsync(Guid.CreateVersion7(), CancellationToken.None)));
    }

    [Fact(DisplayName = "0019/AC-36: Aynı privacy sayfası için iki editör aynı sürüm belirteciyle aynı anda tr metnini kaydettiğinde yalnız biri başarılı olur ve yalnız bir yeni sürüm oluşur")]
    public async Task Dogar_TekSurum_IkiEditorAyniAnda()
    {
        await YayindaAsync(Tr1);
        int belirtec = await icerik.SurumAsync(Privacy);
        icerik.Ilerlet();

        // Bariyer KAYIT anındadır: ikisi de belirteci okuyup doğrulamayı geçer ve
        // SaveChanges'te buluşur. `Task.WhenAll` tek başına ikisini sıraya dizerdi.
        KayitBariyeri bariyer = new(2);
        string[] govdeler = ["Birinci editörün gövdesi.", "İkinci editörün gövdesi."];
        Result<int>[] sonuclar = await Task.WhenAll(govdeler.Select(govde =>
            icerik.KomutAsync(
                k => k.SavePageAsync(Privacy, belirtec, [Tr1 with { Body = govde }], CancellationToken.None),
                bariyer)));

        Assert.Single(sonuclar, sonuc => sonuc.IsSuccess);
        Assert.Single(sonuclar, sonuc => sonuc.IsFailure && sonuc.Error.Code == ConcurrencyErrors.Lost);
        List<PrivacyNoticeVersion> surumler = await SurumlerAsync("tr");
        Assert.Equal(2, surumler.Count);
        Assert.Equal(govdeler[Array.FindIndex(sonuclar, sonuc => sonuc.IsSuccess)], surumler[^1].Body);
    }

    [Fact(DisplayName = "0019/AC-39: content şemasında yayınlanmış sürüm tablosuna doğrudan DELETE girişimi — hiçbir satıra denk gelmese de — veritabanında reddedilir ve satırlar yerinde kalır")]
    public async Task Reddedilir_DogrudanDelete()
    {
        await YayindaAsync(Tr1);

        PostgresException hicbiri = await Assert.ThrowsAsync<PostgresException>(() =>
            SqlAsync("DELETE FROM content.privacy_notice_versions WHERE false"));
        PostgresException hepsi = await Assert.ThrowsAsync<PostgresException>(() =>
            SqlAsync("DELETE FROM content.privacy_notice_versions"));

        Assert.Equal("content.privacy_notice_version_immutable", hicbiri.MessageText);
        Assert.Equal("content.privacy_notice_version_immutable", hepsi.MessageText);
        Assert.Single(await SurumlerAsync());
    }

    [Fact(DisplayName = "0019/AC-40: Yayınlanmış sürüm satırını doğrudan UPDATE girişimi veritabanında reddedilir ve satır değişmez")]
    public async Task Reddedilir_DogrudanUpdate()
    {
        await YayindaAsync(Tr1);

        PostgresException hata = await Assert.ThrowsAsync<PostgresException>(() =>
            SqlAsync("UPDATE content.privacy_notice_versions SET title = 'Değiştirildi', body = 'Değiştirildi'"));

        Assert.Equal("content.privacy_notice_version_immutable", hata.MessageText);
        PrivacyNoticeVersion surum = (await SurumlerAsync()).Single();
        Assert.Equal((Tr1.Title, Tr1.Body), (surum.Title, surum.Body));
    }

    [Fact(DisplayName = "0019/AC-41: Yayınlanmış sürüm kaydının kolonları yalnız kimlik, dil, başlık, gövde, kayda geçtiği an ve başlangıç işaretidir; ziyaretçi, talep ya da personel kimliği taşıyan bir kolon yoktur")]
    public async Task Tasir_YalnizAltiKolon()
    {
        await using NpgsqlConnection baglanti = new(database.ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(
            "SELECT column_name FROM information_schema.columns WHERE table_schema = 'content' AND table_name = 'privacy_notice_versions' ORDER BY column_name",
            baglanti);
        List<string> kolonlar = [];
        await using (DbDataReader okuyucu = await komut.ExecuteReaderAsync())
        {
            while (await okuyucu.ReadAsync())
            {
                kolonlar.Add(okuyucu.GetString(0));
            }
        }

        Assert.Equal(["body", "id", "is_initial", "locale", "recorded_at", "title"], kolonlar);
    }

    [Fact(DisplayName = "0019/AC-50: Content.Tests'te gerçek PostgreSQL'de FindPublishedAsync tek bir SQL komutu çalıştırır (scalar ve non-query komutlar dâhil sayılır) ve content şemasında hiçbir satırı eklemez, değiştirmez ya da silmez")]
    public async Task Calistirir_TekKomut_HicYazmadan()
    {
        await YayindaAsync(Tr1);
        string once = await ParmakIziAsync();

        foreach ((string locale, bool yayinda) in new[] { ("tr", true), ("ar", false) })
        {
            KomutSayaci sayac = new();
            await using ContentDbContext context = database.CreateContext(sayac);

            PrivacyNoticeLookup sonuc = await new PrivacyNoticeQueries(context, icerik.Locales)
                .FindPublishedAsync(locale, CancellationToken.None);

            Assert.Equal(yayinda, sonuc.IsPublished);
            string tek = Assert.Single(sayac.Komutlar);
            Assert.StartsWith("SELECT", tek.TrimStart(), StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(once, await ParmakIziAsync());
    }

    private async Task Dizi1Async()
    {
        await YazAsync(Tr1);
        Assert.Empty(await SurumlerAsync());

        DateTimeOffset an = await YayinlaAsync("tr");

        PrivacyNoticeVersion surum = Assert.Single(await SurumlerAsync());
        Assert.Equal(("tr", Tr1.Title, Tr1.Body, an), (surum.Locale, surum.Title, surum.Body, surum.RecordedAt));
    }

    private async Task Dizi2Async()
    {
        await YayindaAsync(Tr1);
        PrivacyNoticeVersion ilk = (await SurumlerAsync("tr")).Single();

        await YazAsync(Tr1 with { Body = "Kişisel verileriniz güncellenmiş metne göre işlenir." });

        List<PrivacyNoticeVersion> surumler = await SurumlerAsync("tr");
        Assert.Equal(2, surumler.Count);
        Assert.Equal((ilk.Id, Tr1.Title, Tr1.Body, ilk.RecordedAt), (surumler[0].Id, surumler[0].Title, surumler[0].Body, surumler[0].RecordedAt));
        Assert.Equal("Kişisel verileriniz güncellenmiş metne göre işlenir.", surumler[1].Body);
    }

    private async Task Dizi3Async()
    {
        await YayindaAsync(Tr1);

        await YazAsync(Tr1 with { Title = "KVKK Aydınlatma Metni (güncel)" });

        List<PrivacyNoticeVersion> surumler = await SurumlerAsync("tr");
        Assert.Equal(2, surumler.Count);
        Assert.Equal(("KVKK Aydınlatma Metni (güncel)", Tr1.Body), (surumler[1].Title, surumler[1].Body));
    }

    private async Task Dizi4Async()
    {
        await YayindaAsync(Tr1);

        await YazAsync(Tr1 with { Slug = "kvkk-yeni-adres" });

        Assert.Single(await SurumlerAsync("tr"));
    }

    private async Task Dizi5Async()
    {
        await YayindaAsync(Tr1);

        await YazAsync(Ar1);
        await YazAsync(Ar1 with { Body = "نص مسودة آخر." });

        Assert.Empty(await SurumlerAsync("ar"));
        Assert.Single(await SurumlerAsync());
    }

    private async Task Dizi6Async()
    {
        await YayindaAsync(Tr1);
        await YayindaAsync(En1);

        await YazAsync(Tr1 with { Body = "Yeni tr gövdesi." }, En1 with { Body = "New en body." });

        Assert.Equal(["Yeni tr gövdesi."], (await SurumlerAsync("tr")).Skip(1).Select(s => s.Body));
        Assert.Equal(["New en body."], (await SurumlerAsync("en")).Skip(1).Select(s => s.Body));
    }

    private async Task Dizi7Async()
    {
        await YayindaAsync(Tr1);
        await YayindaAsync(En1);

        await YazAsync(Tr1, En1 with { Body = "Only the en body changed." });

        Assert.Single(await SurumlerAsync("tr"));
        Assert.Equal(["Only the en body changed."], (await SurumlerAsync("en")).Skip(1).Select(s => s.Body));
    }

    private async Task Dizi8Async()
    {
        await YayindaAsync(Tr1);

        await YazAsync(Tr1 with { Body = Tr1.Body + " " });

        List<PrivacyNoticeVersion> surumler = await SurumlerAsync("tr");
        Assert.Equal(2, surumler.Count);
        Assert.Equal(Tr1.Body + " ", surumler[1].Body);
    }

    private async Task Dizi9Async()
    {
        await YayindaAsync(Tr1);
        await YazAsync(Tr1 with { Body = "İkinci gövde." });
        List<PrivacyNoticeVersion> once = await SurumlerAsync("tr");

        await KaldirAsync("tr");

        Assert.Equal(once.Select(s => s.Id), (await SurumlerAsync("tr")).Select(s => s.Id));
        foreach (PrivacyNoticeVersion surum in once)
        {
            PrivacyNoticeVersionDto? okunan = await OkuAsync(q => q.FindVersionAsync(surum.Id, CancellationToken.None));
            Assert.Equal(("tr", surum.Title, surum.Body, surum.RecordedAt), (okunan!.Locale, okunan.Title, okunan.Body, okunan.RecordedAt));
        }
    }

    private async Task Dizi10Async()
    {
        await YayindaAsync(Tr1);
        await KaldirAsync("tr");

        await YayinlaAsync("tr");

        List<PrivacyNoticeVersion> surumler = await SurumlerAsync("tr");
        Assert.Equal(2, surumler.Count);
        Assert.All(surumler, s => Assert.Equal((Tr1.Title, Tr1.Body), (s.Title, s.Body)));
    }

    private async Task Dizi11Async()
    {
        await YayindaAsync(Tr1);

        await YayinlaAsync("tr");

        Assert.Single(await SurumlerAsync("tr"));
    }

    private async Task Dizi12Async()
    {
        string[] sistem = [SystemPageKeys.About, SystemPageKeys.Cookies, SystemPageKeys.Faq];

        foreach (string key in sistem)
        {
            Guid id = Icerik.Sistem(key);
            await icerik.YazVeYayinlaAsync(id, "tr", $"{key} başlığı", $"{key} gövdesi.", $"{key}-sayfasi");
            await icerik.YazAsync(id, new PageTextInput("tr", $"{key} başlığı", $"{key} yeni gövdesi.", $"{key}-sayfasi"));
        }

        Guid serbest = await icerik.SerbestAsync(new PageTextInput("tr", "Garanti", "Gövde.", "garanti-sayfasi"));
        await icerik.YayinlaAsync(serbest, "tr");
        await icerik.YazAsync(serbest, new PageTextInput("tr", "Garanti", "Yeni gövde.", "garanti-sayfasi"));

        Assert.Empty(await SurumlerAsync());
    }

    private async Task Dizi13Async()
    {
        await YayindaAsync(Tr1);
        await YazAsync(Ar1);
        int bayat = await icerik.SurumAsync(Privacy) - 1;

        Result<int> bayatKayit = await icerik.KomutAsync(k => k.SavePageAsync(Privacy, bayat, [Tr1 with { Body = "Bayat kayıt." }], CancellationToken.None));
        Result<int> bayatYayin = await icerik.KomutAsync(k => k.PublishPageAsync(Privacy, bayat, "ar", CancellationToken.None));
        int guncel = await icerik.SurumAsync(Privacy);
        Result<int> eksikKayit = await icerik.KomutAsync(k => k.SavePageAsync(Privacy, guncel, [Tr1 with { Body = "   " }], CancellationToken.None));
        await YazAsync(Ar1 with { Slug = null });
        int sonBelirtec = await icerik.SurumAsync(Privacy);
        Result<int> eksikYayin = await icerik.KomutAsync(k => k.PublishPageAsync(Privacy, sonBelirtec, "ar", CancellationToken.None));

        Assert.Equal(ConcurrencyErrors.Lost, bayatKayit.Error.Code);
        Assert.Equal(ConcurrencyErrors.Lost, bayatYayin.Error.Code);
        Assert.Equal(ContentErrors.FieldRequired, Icerik.Alanlar(eksikKayit.Error)["tr.body"]);
        Assert.Equal(ContentErrors.FieldRequired, Icerik.Alanlar(eksikYayin.Error)["ar.slug"]);
        Assert.Single(await SurumlerAsync());
    }

    private async Task Dizi14Async()
    {
        await YayindaAsync(Tr1);
        await icerik.SerbestAsync(new PageTextInput("tr", "Başka", "Gövde.", "baska-sayfa"));

        // Ön denetim: slug zaten başka sayfanın.
        int ilkBelirtec = await icerik.SurumAsync(Privacy);
        Result<int> onDenetim = await icerik.KomutAsync(k => k.SavePageAsync(
            Privacy,
            ilkBelirtec,
            [Tr1 with { Body = "Ön denetimde reddedilen gövde.", Slug = "baska-sayfa" }],
            CancellationToken.None));

        Assert.Equal(ContentErrors.SlugTaken, Icerik.Alanlar(onDenetim.Error)["tr.slug"]);

        // Tekil indeks: ön denetim geçer, kayıt anında başka sayfa slug'ı alır.
        Guid rakip = await icerik.SerbestAsync(new PageTextInput("tr", "Rakip", "Gövde.", "rakip-sayfa"));
        int belirtec = await icerik.SurumAsync(Privacy);
        KayitKapisi kapi = new();
        Task<Result<int>> privacyKaydi = icerik.KomutAsync(
            k => k.SavePageAsync(Privacy, belirtec, [Tr1 with { Body = "İndekste reddedilen gövde.", Slug = "yaris-adresi" }], CancellationToken.None),
            kapi);

        await kapi.Geldi.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await icerik.YazAsync(rakip, new PageTextInput("tr", "Rakip", "Gövde.", "yaris-adresi"));
        kapi.Birak.SetResult();
        Result<int> indeks = await privacyKaydi;

        Assert.Equal(ContentErrors.SlugTaken, Icerik.Alanlar(indeks.Error)["tr.slug"]);
        Assert.Single(await SurumlerAsync());
    }

    private async Task YazAsync(params PageTextInput[] texts)
    {
        icerik.Ilerlet();
        await icerik.YazAsync(Privacy, texts);
    }

    private async Task<DateTimeOffset> YayinlaAsync(string locale)
    {
        DateTimeOffset an = icerik.Ilerlet();
        await icerik.YayinlaAsync(Privacy, locale);
        return an;
    }

    private async Task KaldirAsync(string locale)
    {
        icerik.Ilerlet();
        int surum = await icerik.SurumAsync(Privacy);
        Result<int> sonuc = await icerik.KomutAsync(k => k.UnpublishPageAsync(Privacy, surum, locale, CancellationToken.None));
        Assert.True(sonuc.IsSuccess);
    }

    private async Task YayindaAsync(PageTextInput text)
    {
        await YazAsync(text);
        await YayinlaAsync(text.Locale);
    }

    private async Task<List<PrivacyNoticeVersion>> SurumlerAsync(string? locale = null)
    {
        await using ContentDbContext context = database.CreateContext();
        return await context.PrivacyNoticeVersions
            .AsNoTracking()
            .Where(surum => locale == null || surum.Locale == locale)
            .OrderBy(surum => surum.RecordedAt)
            .ToListAsync();
    }

    private async Task<T> OkuAsync<T>(Func<IPrivacyNoticeQueries, Task<T>> okuma)
    {
        await using ContentDbContext context = database.CreateContext();
        return await okuma(new PrivacyNoticeQueries(context, icerik.Locales));
    }

    private async Task SqlAsync(string sql)
    {
        await using NpgsqlConnection baglanti = new(database.ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(sql, baglanti);
        await komut.ExecuteNonQueryAsync();
    }

    /// <summary>`content` şemasındaki her tablonun bütün satırlarının özeti — AC-50'nin "hiçbir satır değişmez"i.</summary>
    private async Task<string> ParmakIziAsync()
    {
        await using NpgsqlConnection baglanti = new(database.ConnectionString);
        await baglanti.OpenAsync();

        List<string> tablolar = [];
        await using (NpgsqlCommand liste = new(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'content' AND table_type = 'BASE TABLE' ORDER BY table_name",
            baglanti))
        await using (DbDataReader okuyucu = await liste.ExecuteReaderAsync())
        {
            while (await okuyucu.ReadAsync())
            {
                tablolar.Add(okuyucu.GetString(0));
            }
        }

        Assert.Contains("privacy_notice_versions", tablolar);
        List<string> ozetler = [];

        foreach (string tablo in tablolar)
        {
            await using NpgsqlCommand ozet = new(
                $"SELECT '{tablo}:' || count(*) || ':' || coalesce(string_agg(md5(t::text), ',' ORDER BY md5(t::text)), '') FROM content.\"{tablo}\" t",
                baglanti);
            ozetler.Add((string)(await ozet.ExecuteScalarAsync())!);
        }

        return string.Join("|", ozetler);
    }

    private sealed class KayitBariyeri(int sayi) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource hepsi = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int gelen;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref gelen) == sayi)
            {
                hepsi.SetResult();
            }

            await hepsi.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }

    /// <summary>Kaydı, test bırakana kadar SaveChanges'in eşiğinde tutar (AC-14'ün indeks yarışı).</summary>
    private sealed class KayitKapisi : SaveChangesInterceptor
    {
        internal TaskCompletionSource Geldi { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal TaskCompletionSource Birak { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Geldi.TrySetResult();
            await Birak.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }

    /// <summary>Okuma, scalar ve non-query komutlarının hepsini sayar (AC-50).</summary>
    private sealed class KomutSayaci : DbCommandInterceptor
    {
        internal List<string> Komutlar { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Komutlar.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Komutlar.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Komutlar.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}

/// <summary>
/// `0019` AC-15 / R13 — canlı metin değişmezi. "En son sürüm" İKİ yoldan bulunur ve ikisi de
/// ölçülür: kayda geçtiği ana göre en son sürüm (işaretçiye güvenmeden) ve dilin işaretçisi
/// (P2). Yayında olmayan dilin işaretçisi boştur.
/// </summary>
internal static class CanliMetin
{
    internal static async Task DogrulaAsync(ContentDbContext context)
    {
        ContentPage sayfa = await context.Pages
            .AsNoTracking()
            .SingleAsync(page => page.SystemKey == SystemPageKeys.Privacy);
        List<PrivacyNoticeVersion> surumler = await context.PrivacyNoticeVersions.AsNoTracking().ToListAsync();

        foreach (PageLocalization satir in sayfa.Localizations)
        {
            if (satir.Status != PublicationStatus.Published)
            {
                Assert.Null(satir.PublishedVersionId);
                continue;
            }

            List<PrivacyNoticeVersion> dilde = [.. surumler.Where(s => s.Locale == satir.Locale).OrderByDescending(s => s.RecordedAt)];

            Assert.NotEmpty(dilde);
            Assert.True(dilde.Count == 1 || dilde[0].RecordedAt > dilde[1].RecordedAt, $"{satir.Locale}: en son sürümün anı tekil değil");
            Assert.Equal((satir.Title, satir.Body), (dilde[0].Title, dilde[0].Body));
            Assert.Equal(dilde[0].Id, satir.PublishedVersionId);
        }
    }
}
