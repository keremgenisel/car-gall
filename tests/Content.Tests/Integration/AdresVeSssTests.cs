using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>`0017` — adres, slug tekilliği, eski slug, diğer diller, SSS, altlık, sınırlar, saklama biçimi.</summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class AdresVeSssTests(ContentDatabase database) : IAsyncLifetime
{
    private readonly Icerik icerik = new(database);

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Adres ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-34 (okuma yarısı): about tr'de yayındayken hakkimizda slug'ı sayfanın tr başlığını ve gövdesini döner")]
    public async Task Okur_YayindakiSayfayi()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "Biz bir galeriyiz.", "hakkimizda");

        ContentPagePublicDto sayfa = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "hakkimizda", CancellationToken.None)))!;
        Assert.Equal(("about", "Hakkımızda", "Biz bir galeriyiz."), (sayfa.SystemKey, sayfa.Title, sayfa.Body));
    }

    [Fact(DisplayName = "0017/AC-35 (okuma yarısı): olmayan slug için okuma sonuç vermez")]
    public async Task Bulunmaz_OlmayanSlug() =>
        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "hic-olmayan", CancellationToken.None)));

    [Fact(DisplayName = "0017/AC-36 (okuma yarısı): hakkimizda yalnız tr slug'ıdır; ar'da bulunmaz")]
    public async Task Bulunmaz_BaskaDildekiSlug()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "ar", "من نحن", "x", "man-nahnu");

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("ar", "hakkimizda", CancellationToken.None)));
    }

    // ── Slug ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-44 (uç yarısı): geçersiz slug kaydı reddedilir ve slug alanı için errorCode döner")]
    public async Task Reddeder_GecersizSlugKaydi()
    {
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", "x", "x", "Hakkimizda")], CancellationToken.None));
        Assert.Equal(ContentErrors.SlugInvalid, Icerik.Alanlar(sonuc.Error)["tr.slug"]);
    }

    [Fact(DisplayName = "0017/AC-45: başlık girilip slug boş bırakılınca slug boş kalır; başlıktan üretilmez")]
    public async Task Uretmez_SlugBasliktan()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "Garanti Koşulları", "x", null));
        Assert.Equal(string.Empty, (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0].Slug);
    }

    [Fact(DisplayName = "0017/AC-46: A'nın tr slug'ı garanti iken B'nin tr slug'ı garanti yapılamaz; en slug'ı yapılabilir")]
    public async Task Tekil_DilBasina()
    {
        await icerik.SerbestAsync(new PageTextInput("tr", "A", "x", "garanti"));
        Guid b = await icerik.SerbestAsync(new PageTextInput("tr", "B", "x", "b-sayfasi"));
        int surum = await icerik.SurumAsync(b);

        Result<int> tr = await icerik.KomutAsync(k => k.SavePageAsync(b, surum, [new PageTextInput("tr", "B", "x", "garanti")], CancellationToken.None));
        Result<int> en = await icerik.KomutAsync(k => k.SavePageAsync(b, surum, [new PageTextInput("en", "B", "x", "garanti")], CancellationToken.None));

        Assert.Equal(ContentErrors.SlugTaken, Icerik.Alanlar(tr.Error)["tr.slug"]);
        Assert.True(en.IsSuccess);
    }

    [Fact(DisplayName = "0017/AC-47: aynı sayfanın tr ve en slug'ı aynı olabilir")]
    public async Task Ayni_IkiDilde() =>
        await icerik.SerbestAsync(new PageTextInput("tr", "A", "x", "garanti"), new PageTextInput("en", "A", "x", "garanti"));

    [Fact(DisplayName = "0017/AC-48: tekillik taslakları kapsar: A'nın taslak tr slug'ı garanti iken B'ninki garanti olamaz (S7)")]
    public async Task Tekil_TaslakDahil()
    {
        await icerik.SerbestAsync(new PageTextInput("tr", "A", string.Empty, "garanti"));

        Result<(Guid, int)> b = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", "B", "x", "garanti")], CancellationToken.None));
        Assert.Equal(ContentErrors.SlugTaken, Icerik.Alanlar(b.Error)["tr.slug"]);
    }

    [Theory(DisplayName = "0017/AC-49 (uç yarısı): ayrılmış slug üç dilin her birinde reddedilir")]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Reddeder_AyrilmisSlugu(string locale)
    {
        foreach (string slug in ContentReservedSlugs.All)
        {
            Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput(locale, "x", "x", slug)], CancellationToken.None));
            Assert.Equal(ContentErrors.SlugReserved, Icerik.Alanlar(sonuc.Error)[$"{locale}.slug"]);
        }

        await icerik.SerbestAsync(new PageTextInput(locale, "x", "x", "cars-ve-biz"));
    }

    // ── Eski slug (okuma tarafı; 301'in kendisi Host testinde) ──────────────

    [Fact(DisplayName = "0017/AC-51 (okuma yarısı): yayındaki tr slug'ı kvkk → kvkk-aydinlatma değişince kvkk güncele çözülür")]
    public async Task Cozer_EskiSlugu()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazVeYayinlaAsync(privacy, "tr", "KVKK", "metin", "kvkk");
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk-aydinlatma"));

        Assert.Equal("kvkk-aydinlatma", await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "kvkk", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-52 (okuma yarısı): eski slug yalnız tr için kaydedilmiştir; en'de çözülmez")]
    public async Task Cozmez_BaskaDilde()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazVeYayinlaAsync(privacy, "tr", "KVKK", "metin", "kvkk");
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk-aydinlatma"));

        Assert.Null(await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("en", "kvkk", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-53: taslak dilde slug değişikliği eski slug olarak kaydedilmez")]
    public async Task Kaydetmez_TaslaktakiDegisikligi()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk"));
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk-metni"));
        await icerik.YayinlaAsync(privacy, "tr");

        Assert.Null(await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "kvkk", CancellationToken.None)));
        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "kvkk", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-54: kaydedilmiş eski slug başka sayfaya verilemez; aynı sayfanın en slug'ı olabilir")]
    public async Task Ayirir_EskiSlugu()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazVeYayinlaAsync(privacy, "tr", "KVKK", "metin", "kvkk");
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk-aydinlatma"));

        Result<(Guid, int)> baska = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", "x", "x", "kvkk")], CancellationToken.None));
        Assert.Equal(ContentErrors.SlugTaken, Icerik.Alanlar(baska.Error)["tr.slug"]);

        await icerik.YazAsync(privacy, new PageTextInput("en", "Privacy", "x", "kvkk"));
    }

    [Fact(DisplayName = "0017/AC-55: sayfa kendi eski slug'ına dönebilir; o zaman yeni slug eskiye çözülür")]
    public async Task Doner_KendiEskiSlugunu()
    {
        Guid privacy = Icerik.Sistem("privacy");
        await icerik.YazVeYayinlaAsync(privacy, "tr", "KVKK", "metin", "kvkk");
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk-aydinlatma"));
        await icerik.YazAsync(privacy, new PageTextInput("tr", "KVKK", "metin", "kvkk"));

        Assert.NotNull(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "kvkk", CancellationToken.None)));
        Assert.Null(await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "kvkk", CancellationToken.None)));
        Assert.Equal("kvkk", await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "kvkk-aydinlatma", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-56 (okuma yarısı): a-1 → a-2 → a-3 sonrası a-1 ve a-2'nin her biri DOĞRUDAN a-3'e çözülür (S10)")]
    public async Task Cozer_ZincirsizGuncele()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "G", "x", "a-1"));
        await icerik.YayinlaAsync(id, "tr");
        await icerik.YazAsync(id, new PageTextInput("tr", "G", "x", "a-2"));
        await icerik.YazAsync(id, new PageTextInput("tr", "G", "x", "a-3"));

        Assert.Equal("a-3", await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "a-1", CancellationToken.None)));
        Assert.Equal("a-3", await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "a-2", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-57: sayfa silinince eski slug'ı çözülmez ve başka sayfanın slug'ı yapılabilir")]
    public async Task Siler_EskiSluglari()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "G", "x", "eski-garanti"));
        await icerik.YayinlaAsync(id, "tr");
        await icerik.YazAsync(id, new PageTextInput("tr", "G", "x", "garanti"));
        int surum = await icerik.SurumAsync(id);
        await icerik.KomutAsync(k => k.DeletePageAsync(id, surum, CancellationToken.None));

        Assert.Null(await icerik.VitrinAsync(q => q.FindCurrentSlugAsync("tr", "eski-garanti", CancellationToken.None)));
        await icerik.SerbestAsync(new PageTextInput("tr", "Yeni", "x", "eski-garanti"));
    }

    // ── Diğer diller ───────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-58: tr ve en'de yayında, ar'da taslak about'un tr yanıtı en slug'ını taşır; ar slug'ı yanıtta hiç geçmez")]
    public async Task Tasir_YalnizYayindakiDilleri()
    {
        Guid about = Icerik.Sistem("about");
        await icerik.YazVeYayinlaAsync(about, "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazVeYayinlaAsync(about, "en", "About us", "x", "about-us");
        await icerik.YazAsync(about, new PageTextInput("ar", "من نحن", "x", "man-nahnu"));

        ContentPagePublicDto sayfa = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "hakkimizda", CancellationToken.None)))!;

        Assert.Equal([new ContentPageAlternateDto("en", "about-us")], sayfa.Alternates);
        Assert.DoesNotContain("man-nahnu", System.Text.Json.JsonSerializer.Serialize(sayfa), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0017/AC-61 (okuma yarısı): faq ar'da yayında ama ar'da yayındaki girdisi yokken tr yanıtı ar slug'ı taşımaz (S11)")]
    public async Task Tasimaz_GirdisizSssDili()
    {
        Guid faq = Icerik.Sistem("faq");
        await icerik.YazVeYayinlaAsync(faq, "tr", "SSS", "x", "sss");
        await icerik.YazVeYayinlaAsync(faq, "ar", "الأسئلة الشائعة", "x", "al-asila");
        await icerik.SssAsync("A", "أ", "tr");

        ContentPagePublicDto sayfa = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)))!;
        Assert.DoesNotContain(sayfa.Alternates, item => item.Locale == "ar");
    }

    // ── SSS ────────────────────────────────────────────────────────────────

    private async Task<(Guid A, Guid B, Guid C)> OrnekSssAsync()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "Sıkça Sorulan Sorular", "Sorular", "sss");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "ar", "الأسئلة الشائعة", "أسئلة", "al-asila");
        Guid a = await icerik.SssAsync("Takas yapıyor musunuz?", "هل تقبلون المقايضة؟", "tr", "ar");
        Guid b = await icerik.SssAsync("Kredi imkânı var mı?", "هل يوجد تمويل؟", "tr");
        Guid c = await icerik.SssAsync("Ekspertiz raporu veriyor musunuz?", "هل تقدمون تقرير فحص؟", "tr", "ar");
        return (a, b, c);
    }

    [Fact(DisplayName = "0017/AC-75 (okuma yarısı): tr SSS A, B, C'yi; ar SSS yalnız A ve C'yi Arapça metinleriyle bu sırayla döner")]
    public async Task Doner_DilBasinaYayindakiGirdiler()
    {
        await OrnekSssAsync();

        ContentPagePublicDto tr = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)))!;
        ContentPagePublicDto ar = (await icerik.VitrinAsync(q => q.FindBySlugAsync("ar", "al-asila", CancellationToken.None)))!;

        Assert.Equal(["Takas yapıyor musunuz?", "Kredi imkânı var mı?", "Ekspertiz raporu veriyor musunuz?"], tr.FaqEntries.Select(item => item.Question));
        Assert.Equal(["هل تقبلون المقايضة؟", "هل تقدمون تقرير فحص؟"], ar.FaqEntries.Select(item => item.Question));
    }

    [Fact(DisplayName = "0017/AC-76 (uç yarısı): C bir yukarı taşınınca sıra A, C, B olur; tr A, C, B, ar A, C döner")]
    public async Task Tasir_Yukari()
    {
        (Guid a, Guid b, Guid c) = await OrnekSssAsync();
        Assert.True((await icerik.KomutAsync(k => k.MoveFaqEntryAsync(c, up: true, CancellationToken.None))).IsSuccess);

        Assert.Equal([a, c, b], (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Select(item => item.Id));
        Assert.Equal(2, (await icerik.VitrinAsync(q => q.FindBySlugAsync("ar", "al-asila", CancellationToken.None)))!.FaqEntries.Count);
        Assert.Equal("Ekspertiz raporu veriyor musunuz?", (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)))!.FaqEntries[1].Question);
    }

    [Fact(DisplayName = "0017/AC-77 (uç yarısı): A bir aşağı taşınınca sıra B, A, C olur")]
    public async Task Tasir_Asagi()
    {
        (Guid a, Guid b, Guid c) = await OrnekSssAsync();
        await icerik.KomutAsync(k => k.MoveFaqEntryAsync(a, up: false, CancellationToken.None));

        Assert.Equal([b, a, c], (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Select(item => item.Id));
    }

    [Fact(DisplayName = "0017/AC-78 (uç yarısı): ilk girdi yukarı, son girdi aşağı taşınınca sıra değişmez ve dönmez (S13)")]
    public async Task Donmez_Uclarda()
    {
        (Guid a, Guid b, Guid c) = await OrnekSssAsync();
        await icerik.KomutAsync(k => k.MoveFaqEntryAsync(a, up: true, CancellationToken.None));
        await icerik.KomutAsync(k => k.MoveFaqEntryAsync(c, up: false, CancellationToken.None));

        Assert.Equal([a, b, c], (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Select(item => item.Id));
    }

    [Fact(DisplayName = "0017/AC-79: yeni girdi D sıranın sonuna eklenir: A, B, C, D (S13)")]
    public async Task Ekler_Sona()
    {
        (Guid a, Guid b, Guid c) = await OrnekSssAsync();
        Guid d = await icerik.SssAsync("D");

        Assert.Equal([a, b, c, d], (await icerik.PanelAsync(q => q.ListFaqAsync(CancellationToken.None))).Select(item => item.Id));
    }

    [Fact(DisplayName = "0017/AC-83 (okuma yarısı): faq tr'de yayında ama girdilerin üçü de tr'de taslakken SSS bulunamaz")]
    public async Task Bulunmaz_GirdisizSss()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "SSS", "x", "sss");
        await icerik.SssAsync("A");
        await icerik.SssAsync("B");

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)));
        Assert.Empty(await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-84 (okuma yarısı): A tr'de yayına alınınca SSS A'yı gösterir ve altlıkta görünür")]
    public async Task Gorunur_IlkGirdiYayinlaninca()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "SSS", "x", "sss");
        Guid a = await icerik.SssAsync("A");
        int surum = await icerik.SssSurumAsync(a);
        await icerik.KomutAsync(k => k.PublishFaqEntryAsync(a, surum, "tr", CancellationToken.None));

        Assert.Equal(["A"], (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)))!.FaqEntries.Select(item => item.Question));
        Assert.Equal(["faq"], (await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None))).Select(item => item.SystemKey));
    }

    [Fact(DisplayName = "0017/AC-85 (okuma yarısı): faq'ın tr'si taslakken yayındaki girdiler olsa da SSS bulunamaz")]
    public async Task Bulunmaz_TaslakSss()
    {
        await icerik.YazAsync(Icerik.Sistem("faq"), new PageTextInput("tr", "SSS", "x", "sss"));
        await icerik.SssAsync("A", null, "tr");

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "sss", CancellationToken.None)));
    }

    // ── Altlık ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-86, AC-87 (okuma yarısı): yayın sırası ne olursa olsun bağlantılar about, faq, privacy, cookies sırasında, metni sayfanın başlığı (S12)")]
    public async Task Siralar_Sabit()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("cookies"), "tr", "Çerez Politikası", "x", "cerezler");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("privacy"), "tr", "KVKK Aydınlatma Metni", "x", "kvkk");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("faq"), "tr", "Sıkça Sorulan Sorular", "x", "sss");
        await icerik.SssAsync("A", null, "tr");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");

        IReadOnlyList<ContentNavigationLinkDto> linkler = await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None));

        Assert.Equal(
            [("Hakkımızda", "hakkimizda"), ("Sıkça Sorulan Sorular", "sss"), ("KVKK Aydınlatma Metni", "kvkk"), ("Çerez Politikası", "cerezler")],
            linkler.Select(item => (item.Title, item.Slug)));
    }

    [Fact(DisplayName = "0017/AC-88, AC-89, AC-90, AC-91 (okuma yarısı): yalnız o dilde yayındakiler; hiçbiri yoksa liste boş; dil başına; serbest sayfa yok")]
    public async Task Gosterir_YalnizYayindakiSistemSayfalari()
    {
        Assert.Empty(await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None)));

        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("cookies"), "tr", "Çerez Politikası", "x", "cerezler");
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("privacy"), "ar", "سياسة الخصوصية", "x", "al-khususiya");
        Guid g = await icerik.SerbestAsync(new PageTextInput("tr", "Garanti Koşulları", "x", "garanti-kosullari"));
        await icerik.YayinlaAsync(g, "tr");

        Assert.Equal(["Hakkımızda", "Çerez Politikası"], (await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None))).Select(item => item.Title));
        Assert.Equal(["سياسة الخصوصية"], (await icerik.VitrinAsync(q => q.NavigationAsync("ar", CancellationToken.None))).Select(item => item.Title));
        Assert.NotNull(await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "garanti-kosullari", CancellationToken.None)));
    }

    [Fact(DisplayName = "0017/AC-92 (okuma yarısı): about'un slug'ı değişince bağlantı doğrudan yeni slug'a gider")]
    public async Task Izler_YeniSlugu()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazAsync(Icerik.Sistem("about"), new PageTextInput("tr", "Hakkımızda", "x", "biz-kimiz"));

        Assert.Equal("biz-kimiz", (await icerik.VitrinAsync(q => q.NavigationAsync("tr", CancellationToken.None))).Single().Slug);
    }

    [Fact(DisplayName = "0017/AC-112: ar'ı taslak about'un ar okuması bulunmaz; tr yanıtında taslak ar başlığı geçmez")]
    public async Task Sizdirmaz_TaslakMetni()
    {
        Guid about = Icerik.Sistem("about");
        await icerik.YazVeYayinlaAsync(about, "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazAsync(about, new PageTextInput("ar", "مسودة سرية", "x", "man-nahnu"));

        Assert.Null(await icerik.VitrinAsync(q => q.FindBySlugAsync("ar", "man-nahnu", CancellationToken.None)));
        ContentPagePublicDto tr = (await icerik.VitrinAsync(q => q.FindBySlugAsync("tr", "hakkimizda", CancellationToken.None)))!;
        Assert.DoesNotContain("مسودة سرية", System.Text.Json.JsonSerializer.Serialize(tr, new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), StringComparison.Ordinal);
    }

    // ── Sınırlar ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-114: 120 karakterlik başlık kabul edilir; 121 karakterlik reddedilir")]
    public async Task Sinir_Baslik()
    {
        await icerik.SerbestAsync(new PageTextInput("tr", new string('a', 120), "x", "b120"));
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", new string('a', 121), "x", "b121")], CancellationToken.None));
        Assert.Equal(ContentErrors.TitleTooLong, Icerik.Alanlar(sonuc.Error)["tr.title"]);
    }

    [Fact(DisplayName = "0017/AC-115: 120 Arapça harften oluşan başlık kabul edilir")]
    public async Task Sinir_ArapcaBaslik() =>
        await icerik.SerbestAsync(new PageTextInput("ar", new string('ب', 120), "x", "arapca"));

    [Fact(DisplayName = "0017/AC-116: 50.000 karakterlik gövde kabul edilir; 50.001 reddedilir")]
    public async Task Sinir_Govde()
    {
        await icerik.SerbestAsync(new PageTextInput("tr", "x", new string('a', 50_000), "g50000"));
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", "x", new string('a', 50_001), "g50001")], CancellationToken.None));
        Assert.Equal(ContentErrors.BodyTooLong, Icerik.Alanlar(sonuc.Error)["tr.body"]);
    }

    [Fact(DisplayName = "0017/AC-117: 200 karakterlik SSS sorusu kabul edilir; 201 reddedilir")]
    public async Task Sinir_Soru()
    {
        Assert.True((await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("tr", new string('s', 200), "c")], CancellationToken.None))).IsSuccess);
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("tr", new string('s', 201), "c")], CancellationToken.None));
        Assert.Equal(ContentErrors.QuestionTooLong, Icerik.Alanlar(sonuc.Error)["tr.question"]);
    }

    [Fact(DisplayName = "0017/AC-118: 2.000 karakterlik SSS cevabı kabul edilir; 2.001 reddedilir")]
    public async Task Sinir_Cevap()
    {
        Assert.True((await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("tr", "s", new string('c', 2_000))], CancellationToken.None))).IsSuccess);
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("tr", "s", new string('c', 2_001))], CancellationToken.None));
        Assert.Equal(ContentErrors.AnswerTooLong, Icerik.Alanlar(sonuc.Error)["tr.answer"]);
    }

    [Fact(DisplayName = "0017/AC-119: 121 karakterlik başlık ve A slug'ıyla kaydetme tek yanıtta iki ayrı hata döner")]
    public async Task Doner_IkiHata()
    {
        Result<(Guid, int)> sonuc = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", new string('a', 121), "x", "A")], CancellationToken.None));

        Dictionary<string, string> alanlar = Icerik.Alanlar(sonuc.Error);
        Assert.Equal(("content.title_too_long", "content.slug_invalid"), (alanlar["tr.title"], alanlar["tr.slug"]));
    }

    [Fact(DisplayName = "0017/AC-70 (uç yarısı), AC-74 (cevap yarısı): izinsiz şemalı bağlantılı gövde ve cevap kaydı reddedilir")]
    public async Task Reddeder_IzinsizBaglanti()
    {
        Result<(Guid, int)> govde = await icerik.KomutAsync(k => k.CreatePageAsync([new PageTextInput("tr", "x", "[tıkla](javascript:alert(1))", "zararli")], CancellationToken.None));
        Result<(Guid, int)> cevap = await icerik.KomutAsync(k => k.CreateFaqEntryAsync([new FaqTextInput("tr", "s", "[tıkla](javascript:alert(1))")], CancellationToken.None));

        Assert.Equal(ContentErrors.LinkSchemeNotAllowed, Icerik.Alanlar(govde.Error)["tr.body"]);
        Assert.Equal(ContentErrors.LinkSchemeNotAllowed, Icerik.Alanlar(cevap.Error)["tr.answer"]);
    }

    [Fact(DisplayName = "0017/AC-68 (uç yarısı): <b>kalın</b> içeren gövde reddedilmez ve olduğu gibi saklanır")]
    public async Task Saklar_HamHtmlMetni()
    {
        Guid id = await icerik.SerbestAsync(new PageTextInput("tr", "x", "<b>kalın</b>", "ham-html"));
        Assert.Equal("<b>kalın</b>", (await icerik.PanelAsync(q => q.FindPageAsync(id, CancellationToken.None)))!.Locales[0].Body);
    }

    // ── Saklama biçimi ─────────────────────────────────────────────────────

    [Fact(DisplayName = "0017/AC-131: yayın durumu draft / published ve sistem sayfası kimliği kod olarak saklanır; çevrilmiş ad saklanmaz")]
    public async Task Saklar_Kodlari()
    {
        await icerik.YazVeYayinlaAsync(Icerik.Sistem("about"), "tr", "Hakkımızda", "x", "hakkimizda");
        await icerik.YazAsync(Icerik.Sistem("about"), new PageTextInput("en", "About", "x", "about-us"));

        await using ContentDbContext context = database.CreateContext();
        List<string> durumlar = await context.Database.SqlQueryRaw<string>("SELECT status AS \"Value\" FROM content.page_localizations").ToListAsync();
        List<string> kimlikler = await context.Database.SqlQueryRaw<string>("SELECT system_key AS \"Value\" FROM content.pages WHERE system_key IS NOT NULL").ToListAsync();

        Assert.Equal(["draft", "published"], durumlar.Distinct().Order());
        Assert.Equal(["about", "cookies", "faq", "privacy"], kimlikler.Order());
    }
}
