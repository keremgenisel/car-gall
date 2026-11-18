using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>`0017` — sayfa yaşam döngüsü: oluşturma, yayına alma şartı, kaydetme, kaldırma, silme, eşzamanlılık.</summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class SayfaYasamTests(ContentDatabase database) : IAsyncLifetime
{
    private readonly Icerik icerik = new(database);

    private static readonly PageTextInput GTr = new("tr", "Garanti Koşulları", "Araçlarımız 12 ay garantilidir.", "garanti-kosullari");

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "0017/AC-1: Yeni bir kurulumda panel tam dört sistem sayfası listeler; üç dilde metin boş, durum taslak, serbest sayfa yok")]
    public async Task Listeler_DortSistemSayfasi()
    {
        IReadOnlyList<AdminPageDto> sayfalar = await icerik.PanelAsync(q => q.ListPagesAsync(CancellationToken.None));

        Assert.Equal(["about", "faq", "privacy", "cookies"], sayfalar.Select(item => item.SystemKey));
        Assert.All(sayfalar.SelectMany(item => item.Locales), satir =>
        {
            Assert.Equal((string.Empty, string.Empty, string.Empty, "draft"), (satir.Title, satir.Body, satir.Slug, satir.Status));
        });
        Assert.All(sayfalar, item => Assert.Equal(["tr", "ar", "en"], item.Locales.Select(satir => satir.Locale)));
    }

    [Fact(DisplayName = "0017/AC-2: tr başlığı, gövdesi ve slug'ı verilen sayfa serbest sayfa olarak oluşur ve üç dili taslaktır")]
    public async Task Olusturur_SerbestSayfa()
    {
        Guid id = await icerik.SerbestAsync(GTr);

        AdminPageDto sayfa = (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!;

        Assert.Null(sayfa.SystemKey);
        Assert.Equal(("Garanti Koşulları", "garanti-kosullari"), (sayfa.Locales[0].Title, sayfa.Locales[0].Slug));
        Assert.All(sayfa.Locales, satir => Assert.Equal("draft", satir.Status));
    }

    [Fact(DisplayName = "0017/AC-4: G'nin en metni kaydedildikten sonra tr metni değişmez")]
    public async Task Korur_DigerDiliMetnini()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YazAsync(id, new PageTextInput("en", "Warranty Terms", "Our cars carry a 12-month warranty.", "warranty-terms"));

        AdminPageDto sayfa = (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!;
        Assert.Equal(("Garanti Koşulları", "garanti-kosullari"), (sayfa.Locales.Single(s => s.Locale == "tr").Title, sayfa.Locales.Single(s => s.Locale == "tr").Slug));
    }

    [Fact(DisplayName = "0017/AC-5: G tr'de yayına alınınca tr adresinde başlığı ve gövdesiyle okunur; en ve ar taslak kalır")]
    public async Task Yayinlar_YalnizBirDili()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");

        ContentPagePublicDto? okunan = await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None));

        Assert.Equal(("Garanti Koşulları", "Araçlarımız 12 ay garantilidir."), (okunan!.Title, okunan.Body));
        AdminPageDto sayfa = (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!;
        Assert.Equal(["published", "draft", "draft"], sayfa.Locales.Select(s => s.Status));
    }

    [Fact(DisplayName = "0017/AC-6: en metni dolu ama taslakken en adresi bulunamaz")]
    public async Task Bulunmaz_TaslakDil()
    {
        Guid id = await icerik.SerbestAsync(GTr, new PageTextInput("en", "Warranty Terms", "x", "warranty-terms"));
        await icerik.YayinlaAsync(id, "tr");

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("en", "warranty-terms", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-7: iki serbest sayfa oluşturulup biri silinince panel dört sistem sayfası ve bir serbest sayfa listeler")]
    public async Task Listeler_SilmedenSonra()
    {
        Guid g1 = await icerik.SerbestAsync(GTr);
        await icerik.SerbestAsync(new PageTextInput("tr", "İkinci", string.Empty, "ikinci"));
        int surum = await icerik.SurumAsync(g1);
        Assert.True((await icerik.KomutAsync(k => k.DeletePageAsync(g1, surum, CancellationToken.None))).IsSuccess);

        IReadOnlyList<AdminPageDto> sayfalar = await icerik.PanelAsync(q => q.ListPagesAsync(CancellationToken.None));
        Assert.Equal(4, sayfalar.Count(item => item.SystemKey is not null));
        Assert.Single(sayfalar, item => item.SystemKey is null);
    }

    [Fact(DisplayName = "0017/AC-8: başlığı dolu, gövdesi ve slug'ı boş dili yayına alma isteği reddedilir; gövde ve slug için errorCode, başlık için yok; dil taslak kalır")]
    public async Task Reddeder_EksikDiliYayinlamayi()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "Garanti Koşulları", null, null));
        int surum = await icerik.SurumAsync(id);

        Result<int> sonuc = await icerik.KomutAsync(k => k.PublishPageAsync(id, surum, "tr", CancellationToken.None));

        Dictionary<string, string> alanlar = Icerik.Alanlar(sonuc.Error);
        Assert.Equal(["tr.body", "tr.slug"], alanlar.Keys.Order());
        Assert.All(alanlar.Values, kod => Assert.Equal(ContentErrors.FieldRequired, kod));
        Assert.Equal("draft", (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0].Status);
    }

    [Fact(DisplayName = "0017/AC-10: başlığı yalnız boşluk olan dili yayına alma isteği reddedilir ve başlık için errorCode döner (S9)")]
    public async Task Reddeder_BoslukBaslik()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "   ", "gövde", "bosluk"));
        int surum = await icerik.SurumAsync(id);

        Result<int> sonuc = await icerik.KomutAsync(k => k.PublishPageAsync(id, surum, "tr", CancellationToken.None));

        Assert.Equal(["tr.title"], Icerik.Alanlar(sonuc.Error).Keys);
    }

    [Fact(DisplayName = "0017/AC-11: taslak dil, başlığı dolu ve gövdesi ile slug'ı boşken kaydedilebilir; dil taslak kalır (S7)")]
    public async Task Kaydeder_EksikTaslak()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "Garanti Koşulları", string.Empty, string.Empty));

        Assert.Equal("draft", (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0].Status);
    }

    [Fact(DisplayName = "0017/AC-12: yayındaki dilin başlığı boşaltılıp kaydedilirse reddedilir; sayfa eski başlığıyla yayında kalır (S8)")]
    public async Task Reddeder_YayindakiZorunluAlaniBosaltmayi()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");
        int surum = await icerik.SurumAsync(id);

        Result<int> sonuc = await icerik.KomutAsync(k => k.SavePageAsync(id, surum, [GTr with { Title = string.Empty }], CancellationToken.None));

        Assert.Equal(ContentErrors.FieldRequired, Icerik.Alanlar(sonuc.Error)["tr.title"]);
        Assert.Equal("Garanti Koşulları", (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)))!.Title);
    }

    [Fact(DisplayName = "0017/AC-13: tr ve en tam, ar boş sayfada panel eksik dil olarak yalnız ar'ı gösterir")]
    public async Task Gosterir_EksikDil()
    {
        Guid id = await icerik.SerbestAsync(GTr, new PageTextInput("en", "Warranty Terms", "x", "warranty-terms"));

        Assert.Equal(["ar"], (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.MissingLocales);
    }

    [Fact(DisplayName = "0017/AC-14: etkin locale'leri tr, en olan kurulumda aynı sayfa için eksik dil yoktur: ar sayılmaz")]
    public async Task Saymaz_EtkinOlmayanDili()
    {
        Guid id = await icerik.SerbestAsync(GTr, new PageTextInput("en", "Warranty Terms", "x", "warranty-terms"));
        Icerik ikiDilli = new(database, "tr", "en");

        Assert.Empty((await ikiDilli.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.MissingLocales);
    }

    [Fact(DisplayName = "0017/AC-15: üç dilde tam, ar'ı taslak sayfada ar eksik gösterilmez (S6)")]
    public async Task Saymaz_TaslagiEksik()
    {
        Guid id = await icerik.SerbestAsync(
            GTr,
            new PageTextInput("en", "Warranty Terms", "x", "warranty-terms"),
            new PageTextInput("ar", "شروط الضمان", "x", "shurut-al-daman"));
        await icerik.YayinlaAsync(id, "tr");
        await icerik.YayinlaAsync(id, "en");

        Assert.Empty((await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.MissingLocales);
    }

    [Fact(DisplayName = "0017/AC-16: üç dilde başlık ve gövde dolu, en slug'ı boş sayfada eksik dil yalnız en'dir (S6)")]
    public async Task Sayar_EksikSlugu()
    {
        Guid id = await icerik.SerbestAsync(
            GTr,
            new PageTextInput("en", "Warranty Terms", "x", string.Empty),
            new PageTextInput("ar", "شروط الضمان", "x", "shurut-al-daman"));

        Assert.Equal(["en"], (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.MissingLocales);
    }

    [Fact(DisplayName = "0017/AC-17: sorusu dolu, ar cevabı boş SSS girdisini ar'da yayına alma reddedilir ve cevap için errorCode döner (S6)")]
    public async Task Reddeder_CevapsizSss()
    {
        Result<(Guid Id, int Version)> girdi = await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("ar", "هل تقبلون المقايضة؟", null)], CancellationToken.None));

        Result<int> sonuc = await icerik.KomutAsync(k => k.PublishFaqEntryAsync(girdi.Value.Id, girdi.Value.Version, "ar", CancellationToken.None));

        Assert.Equal(["ar.answer"], Icerik.Alanlar(sonuc.Error).Keys);
    }

    [Fact(DisplayName = "0017/AC-18: AC-17'nin girdisi için panel eksik dil olarak ar'ı gösterir (S6)")]
    public async Task Gosterir_SssEksikDili()
    {
        Result<(Guid Id, int Version)> girdi = await icerik.KomutAsync(k => k.CreateFaqEntryAsync(
            [new FaqTextInput("tr", "Takas?", "Evet."), new FaqTextInput("en", "Trade-in?", "Yes."), new FaqTextInput("ar", "هل تقبلون المقايضة؟", null)],
            CancellationToken.None));

        AdminFaqEntryDto entry = (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Single(item => item.Id == girdi.Value.Id);
        Assert.Equal(["ar"], entry.MissingLocales);
    }

    [Fact(DisplayName = "0017/AC-19: yayındaki dilde kaydetmek ayrı yayın eylemi olmadan ilk public okumada görünür")]
    public async Task Yayinlar_KaydedileniHemen()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");
        await icerik.YazAsync(id, GTr with { Body = "Araçlarımız 24 ay garantilidir." });

        ContentPagePublicDto okunan = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)))!;
        Assert.Contains("24 ay", okunan.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("12 ay", okunan.Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0017/AC-20: kayıttan sonra dil yayındadır ve panelin metni public okumayla aynıdır; bekleyen ikinci metin yok")]
    public async Task TekMetin_PanelVeVitrin()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");
        await icerik.YazAsync(id, GTr with { Body = "Araçlarımız 24 ay garantilidir." });

        AdminPageLocaleDto panel = (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0];
        ContentPagePublicDto vitrin = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)))!;
        Assert.Equal("published", panel.Status);
        Assert.Equal((panel.Title, panel.Body), (vitrin.Title, vitrin.Body));
    }

    [Fact(DisplayName = "0017/AC-21: yayından kaldırılan dil taslak olur, adresi bulunamaz ve panel metni değişmeden gösterir")]
    public async Task Kaldirir_MetniKoruyarak()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");
        int surum = await icerik.SurumAsync(id);
        Assert.True((await icerik.KomutAsync(k => k.UnpublishPageAsync(id, surum, "tr", CancellationToken.None))).IsSuccess);

        AdminPageLocaleDto panel = (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0];
        Assert.Equal(("draft", GTr.Title, GTr.Body, GTr.Slug), (panel.Status, panel.Title, panel.Body, panel.Slug));
        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-22: kaldırılan dil yeniden yayına alınınca metin yeniden girilmeden önceki başlık ve gövdeyle okunur")]
    public async Task YenidenYayinlar_EskiMetinle()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        await icerik.YayinlaAsync(id, "tr");
        int surum = await icerik.SurumAsync(id);
        await icerik.KomutAsync(k => k.UnpublishPageAsync(id, surum, "tr", CancellationToken.None));
        await icerik.YayinlaAsync(id, "tr");

        ContentPagePublicDto vitrin = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)))!;
        Assert.Equal((GTr.Title, GTr.Body), (vitrin.Title, vitrin.Body));
    }

    [Fact(DisplayName = "0017/AC-23: privacy'nin yayındaki tr dili yayından kaldırılabilir")]
    public async Task Kaldirir_SistemSayfasiniDa()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazVeYayinlaAsync(privacy, "tr", "KVKK Aydınlatma Metni", "metin", "kvkk");
        int surum = await icerik.SurumAsync(privacy);

        Assert.True((await icerik.KomutAsync(k => k.UnpublishPageAsync(privacy, surum, "tr", CancellationToken.None))).IsSuccess);
        Assert.Equal("draft", (await icerik.PanelAsync(q => q.FindPageAsync(privacy, CancellationToken.None)))!.Locales[0].Status);
    }

    [Fact(DisplayName = "0017/AC-25: about için silme isteği reddedilir; sayfa ve metni değişmez")]
    public async Task Reddeder_SistemSayfasiSilmeyi()
    {
        Guid about = Icerik.Sistem("about");
        await icerik.YazAsync(about, new PageTextInput("tr", "Hakkımızda", "biz", "hakkimizda"));
        int surum = await icerik.SurumAsync(about);

        Result sonuc = await icerik.KomutAsync(k => k.DeletePageAsync(about, surum, CancellationToken.None));

        Assert.Equal(ContentErrors.SystemPageNotDeletable, sonuc.Error.Code);
        Assert.Equal("Hakkımızda", (await icerik.PanelAsync(q => q.FindPageAsync(about, CancellationToken.None)))!.Locales[0].Title);
    }

    [Fact(DisplayName = "0017/AC-27: silinen G'nin üç dildeki adresi de bulunamaz")]
    public async Task Bulunmaz_SilinenSayfa()
    {
        Guid id = await icerik.SerbestAsync(
            GTr,
            new PageTextInput("en", "Warranty Terms", "x", "warranty-terms"),
            new PageTextInput("ar", "شروط الضمان", "x", "shurut-al-daman"));
        foreach (string locale in new[] { "tr", "en", "ar" })
        {
            await icerik.YayinlaAsync(id, locale);
        }

        int surum = await icerik.SurumAsync(id);
        Assert.True((await icerik.KomutAsync(k => k.DeletePageAsync(id, surum, CancellationToken.None))).IsSuccess);

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)));
        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("en", "warranty-terms", CancellationToken.None)));
        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("ar", "shurut-al-daman", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-28: silinen G geri getirilemez; kimliğiyle panel okuması bulunamaz")]
    public async Task GeriGelmez_SilinenSayfa()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        int surum = await icerik.SurumAsync(id);
        await icerik.KomutAsync(k => k.DeletePageAsync(id, surum, CancellationToken.None));

        Assert.Null(await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)));
        Assert.DoesNotContain((await icerik.PanelAsync(q => q.ListPagesAsync(CancellationToken.None))), item => item.Id == id);
    }

    [Fact(DisplayName = "0017/AC-29: A, B, C'den B silinince SSS A ve C'yi bu sırayla gösterir; panelde de sıra A, C")]
    public async Task Siler_SssGirdisini()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "Sıkça Sorulan Sorular", "Sorular", "sss");
        Guid a = await icerik.SssAsync("A", null, "tr");
        Guid b = await icerik.SssAsync("B", null, "tr");
        Guid c = await icerik.SssAsync("C", null, "tr");

        int surum = await icerik.SssSurumAsync(b);
        Assert.True((await icerik.KomutAsync(k => k.DeleteFaqEntryAsync(b, surum, CancellationToken.None))).IsSuccess);

        ContentPagePublicDto sss = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)))!;
        Assert.Equal(["A", "C"], sss.FaqEntries.Select(item => item.Question));
        Assert.Equal([a, c], (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Select(item => item.Id));
    }

    [Fact(DisplayName = "0017/AC-30: aynı sürümden açan ikinci editörün kaydı 409 olur ve kayıtlı başlık birincininki kalır")]
    public async Task Reddeder_EskiSurumKaydi()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        int surum = await icerik.SurumAsync(id);

        Result<int> birinci = await icerik.KomutAsync(k => k.SavePageAsync(id, surum, [GTr with { Title = "Garanti" }], CancellationToken.None));
        Result<int> ikinci = await icerik.KomutAsync(k => k.SavePageAsync(id, surum, [GTr with { Title = "Garanti Şartları" }], CancellationToken.None));

        Assert.True(birinci.IsSuccess);
        Assert.Equal(ConcurrencyErrors.Lost, ikinci.Error.Code);
        Assert.Equal("Garanti", (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0].Title);
    }

    [Fact(DisplayName = "0017/AC-32: aynı sürümden iki kayıt EŞZAMANLI gönderildiğinde tam biri başarılı olur, diğeri 409 alır")]
    public async Task Reddeder_ParalelKayitlardanBirini()
    {
        Guid id = await icerik.SerbestAsync(GTr);
        int surum = await icerik.SurumAsync(id);

        // Paralel, ardışık değil — ve bunu ZAMANLAMAYA bırakmadan: iki komut da okumasını
        // ve sürüm kontrolünü bitirip KAYIT anında bir bariyerde buluşur. Bariyersiz ilk
        // yazımda işlemler o kadar kısaydı ki ikinci komut çoğu zaman birincinin yeni
        // sürümünü okuyor ve sürüm kontrolüne takılıyordu: eşzamanlılık belirteci kaldırılınca
        // test YEŞİL kaldı (bozmayla ölçüldü). Kaybı burada yalnız veritabanı yakalayabilir.
        KayitBariyeri bariyer = new(2);
        Result<int>[] sonuclar = await Task.WhenAll(Enumerable.Range(0, 2).Select(i =>
            Task.Run(() => icerik.KomutAsync(k => k.SavePageAsync(id, surum, [GTr with { Title = $"Başlık {i}" }], CancellationToken.None), bariyer))));

        Assert.Single(sonuclar, item => item.IsSuccess);
        Assert.Single(sonuclar, item => item.IsFailure && item.Error.Code == ConcurrencyErrors.Lost);
    }

    /// <summary>Verilen sayıda bağlam kayda gelene kadar her birini bekletir.</summary>
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

    [Fact(DisplayName = "0017/AC-33: SSS girdisi için eski sürüm üzerinden kaydetme 409 döner ve soru değişmez")]
    public async Task Reddeder_EskiSurumSss()
    {
        Guid a = await icerik.SssAsync("Takas yapıyor musunuz?");
        int surum = await icerik.SssSurumAsync(a);

        await icerik.KomutAsync(k => k.SaveFaqEntryAsync(a, surum, [new FaqTextInput("tr", "Takas?", "Evet.")], CancellationToken.None));
        Result<int> ikinci = await icerik.KomutAsync(k => k.SaveFaqEntryAsync(a, surum, [new FaqTextInput("tr", "Değişmiş", "x")], CancellationToken.None));

        Assert.Equal(ConcurrencyErrors.Lost, ikinci.Error.Code);
        Assert.Equal("Takas?", (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Single().Locales[0].Question);
    }
}
