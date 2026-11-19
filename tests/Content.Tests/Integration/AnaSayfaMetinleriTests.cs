using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Modules.Content.Tests.Integration;

/// <summary>
/// `0022` — ana sayfa metinlerinin kaydı ve okuması, gerçek PostgreSQL üzerinde: boş alanın
/// varsayılana düşmesi (R2), sınırlar (R4), eşzamanlılık (R8) ve etkin diller (AC-36).
/// Varsayılan metnin kendisi uçtadır (C2); burada "varsayılan" = okumanın <c>null</c> dönmesi.
/// </summary>
[Collection(ContentDatabaseCollection.Name)]
public sealed class AnaSayfaMetinleriTests(ContentDatabase database) : IAsyncLifetime
{
    private readonly Icerik icerik = new(database);

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "0022/AC-3: Türkçe giriş başlığı \"Kadıköy'ün güvenilir galerisi\" olarak kaydedildikten **hemen sonraki ilk** Türkçe ana sayfa isteği bu başlığı gösterir (D20).")]
    public async Task Okunur_KaydedilenBaslik_HemenSonra()
    {
        await KaydetAsync(Girdi("tr", ("introTitle", "Kadıköy'ün güvenilir galerisi")));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));

        Assert.Equal("Kadıköy'ün güvenilir galerisi", okunan.IntroTitle);
    }

    [Fact(DisplayName = "0022/AC-4: Türkçe giriş başlığı kaydedildiğinde İngilizce ve Arapça ana sayfa kendi varsayılan başlıklarını göstermeye devam eder.")]
    public async Task Kalir_DigerDillerVarsayilanda()
    {
        await KaydetAsync(Girdi("tr", ("introTitle", "Kadıköy'ün güvenilir galerisi")));

        Assert.Null((await icerik.AnaSayfaOkuAsync(q => q.FindAsync("en", CancellationToken.None))).IntroTitle);
        Assert.Null((await icerik.AnaSayfaOkuAsync(q => q.FindAsync("ar", CancellationToken.None))).IntroTitle);
    }

    [Fact(DisplayName = "0022/AC-5: Kaydedilmiş bir alan boşaltılıp kaydedildiğinde ana sayfa o dilde yeniden varsayılan metni gösterir.")]
    public async Task DonerVarsayilana_AlanBosaltilinca()
    {
        await KaydetAsync(Girdi("tr", ("introTitle", "Kadıköy'ün güvenilir galerisi"), ("formTitle", "Size uygun aracı seçin")));
        await KaydetAsync(Girdi("tr", ("introTitle", ""), ("formTitle", "Size uygun aracı seçin")));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));

        Assert.Null(okunan.IntroTitle);
        Assert.Equal("Size uygun aracı seçin", okunan.FormTitle);
    }

    [Fact(DisplayName = "0022/AC-6: Yalnız boşluktan oluşan bir giriş başlığı (`\"   \"`) boş sayılır ve ana sayfa varsayılan başlığı gösterir (D14).")]
    public async Task BosSayilir_YalnizBosluk()
    {
        await KaydetAsync(Girdi("tr", ("introTitle", "   "), ("emptyBody", "\n\t ")));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));
        AdminHomeTextsDto panel = await icerik.AnaSayfaOkuAsync(q => q.GetForAdminAsync(CancellationToken.None));

        Assert.Null(okunan.IntroTitle);
        Assert.Null(okunan.EmptyBody);
        Assert.Equal(string.Empty, panel.Locales.Single(l => l.Locale == "tr").Values["introTitle"]);
    }

    [Fact(DisplayName = "0022/AC-6: Yalnız boşluktan oluşan bir giriş başlığı (`\"   \"`) boş sayılır ve ana sayfa varsayılan başlığı gösterir (D14).")]
    public async Task Kirpilmaz_BosOlmayanDegerinUclari()
    {
        // R2 — boş olmayan değerin uçlarındaki boşluk kırpılmaz (`0017` ile aynı).
        await KaydetAsync(Girdi("tr", ("introTitle", "  Uçlarında boşluk  ")));

        Assert.Equal("  Uçlarında boşluk  ", (await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None))).IntroTitle);
    }

    [Fact(DisplayName = "0022/AC-7: Hiç kayıt yapılmamış bir kurulumda ana sayfanın altı metni bugünkü varsayılan metinlerle aynıdır.")]
    public async Task VarsayilandadirHepsi_KayitYokken()
    {
        foreach (string locale in new[] { "tr", "ar", "en" })
        {
            Assert.Equal(
                new HomeTextsDto(null, null, null, null, null, null),
                await icerik.AnaSayfaOkuAsync(q => q.FindAsync(locale, CancellationToken.None)));
        }

        AdminHomeTextsDto panel = await icerik.AnaSayfaOkuAsync(q => q.GetForAdminAsync(CancellationToken.None));
        Assert.Equal(1, panel.Version);
        Assert.All(panel.Locales.SelectMany(l => l.Values.Values), deger => Assert.Equal(string.Empty, deger));
    }

    [Fact(DisplayName = "0022/AC-8: Yayında ilan ve öne çıkan ilan varken, \"Aracınızı bulun\" ve \"Öne çıkan araçlar\" başlıkları kaydedildikten sonra ana sayfada kaydedilen metinle görünür (D18).")]
    public async Task Okunur_BolumBasliklari()
    {
        await KaydetAsync(Girdi("tr", ("formTitle", "Size uygun aracı seçin"), ("featuredTitle", "Haftanın seçkisi")));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));

        Assert.Equal(("Size uygun aracı seçin", "Haftanın seçkisi"), (okunan.FormTitle, okunan.FeaturedTitle));
    }

    [Fact(DisplayName = "0022/AC-9: Yayında hiç ilan yokken ana sayfa, kaydedilmiş boş stok başlığını ve açıklamasını gösterir.")]
    public async Task Okunur_BosStokMesaji()
    {
        await KaydetAsync(Girdi("tr", ("emptyTitle", "Yeni araçlar yolda"), ("emptyBody", "Satır bir\nSatır iki")));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));

        Assert.Equal(("Yeni araçlar yolda", "Satır bir\nSatır iki"), (okunan.EmptyTitle, okunan.EmptyBody));
    }

    [Fact(DisplayName = "0022/AC-16: 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.")]
    public async Task Reddeder_SinirAsimi_KayitDegismez()
    {
        await KaydetAsync(Girdi("tr", ("introTitle", "Önceki başlık")));
        int surum = await SurumAsync();

        Result<int> sonuc = await icerik.AnaSayfaKomutAsync(k => k.SaveAsync(
            surum,
            [Girdi("tr", ("introTitle", new string('a', 121)), ("introBody", new string('b', 501)))],
            CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        Dictionary<string, string> alanlar = Icerik.Alanlar(sonuc.Error);
        Assert.Equal("content.title_too_long", alanlar["tr.introTitle"]);
        Assert.Equal("content.description_too_long", alanlar["tr.introBody"]);
        Assert.Equal(2, alanlar.Count);
        Assert.Equal("Önceki başlık", (await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None))).IntroTitle);
        Assert.Equal(surum, await SurumAsync());
    }

    [Theory(DisplayName = "0022/AC-16: 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.")]
    [InlineData("formTitle", 121, "content.title_too_long")]
    [InlineData("featuredTitle", 121, "content.title_too_long")]
    [InlineData("emptyTitle", 121, "content.title_too_long")]
    [InlineData("emptyBody", 501, "content.description_too_long")]
    public async Task Reddeder_HerAlaninSiniri(string alan, int uzunluk, string kod)
    {
        Result<int> sonuc = await icerik.AnaSayfaKomutAsync(k => k.SaveAsync(
            1,
            [Girdi("ar", (alan, new string('ب', uzunluk)))],
            CancellationToken.None));

        Assert.Equal(kod, Icerik.Alanlar(sonuc.Error)[$"ar.{alan}"]);
    }

    [Fact(DisplayName = "0022/AC-17: 120 karakterlik başlık ve 500 karakterlik açıklama kabul edilir.")]
    public async Task KabulEder_TamSinirda()
    {
        string baslik = new('a', 120);
        string aciklama = new('b', 500);

        await KaydetAsync(Girdi(
            "tr",
            ("introTitle", baslik), ("introBody", aciklama), ("formTitle", baslik),
            ("featuredTitle", baslik), ("emptyTitle", baslik), ("emptyBody", aciklama)));

        HomeTextsDto okunan = await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None));
        Assert.Equal((baslik, aciklama, baslik, aciklama), (okunan.IntroTitle, okunan.IntroBody, okunan.EmptyTitle, okunan.EmptyBody));
    }

    [Fact(DisplayName = "0022/AC-18: Arapça harf ve emoji içeren, tam 120 kod noktalı bir başlık kabul edilir; 121 kod noktalı olanı reddedilir; panelin sayacı iki durumda da sunucuyla aynı sayıyı gösterir (D15).")]
    public async Task SayarKodNoktasi_ArapcaVeEmoji()
    {
        string yuzYirmi = ArapcaVeEmoji(120);
        Assert.Equal(120, yuzYirmi.EnumerateRunes().Count());
        Assert.True(yuzYirmi.Length > 120, "UTF-16 birimi kod noktasından fazla olmalı: emoji iki birim.");

        await KaydetAsync(Girdi("ar", ("introTitle", yuzYirmi)));
        Assert.Equal(yuzYirmi, (await icerik.AnaSayfaOkuAsync(q => q.FindAsync("ar", CancellationToken.None))).IntroTitle);

        int surum = await SurumAsync();
        Result<int> sonuc = await icerik.AnaSayfaKomutAsync(k => k.SaveAsync(
            surum,
            [Girdi("ar", ("introTitle", ArapcaVeEmoji(121)))],
            CancellationToken.None));
        Assert.Equal("content.title_too_long", Icerik.Alanlar(sonuc.Error)["ar.introTitle"]);
    }

    [Fact(DisplayName = "0022/AC-25: Okunduktan sonra başka bir kaydetmeyle değişmiş sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı metin değişmez.")]
    public async Task Reddeder_EskiSurum_KayitDegismez()
    {
        int okunan = await SurumAsync();
        await KaydetAsync(Girdi("tr", ("introTitle", "Birinci editör")));

        Result<int> sonuc = await icerik.AnaSayfaKomutAsync(k => k.SaveAsync(
            okunan,
            [Girdi("tr", ("introTitle", "İkinci editör"))],
            CancellationToken.None));

        Assert.Equal(ConcurrencyErrors.Lost, sonuc.Error.Code);
        Assert.Equal("Birinci editör", (await icerik.AnaSayfaOkuAsync(q => q.FindAsync("tr", CancellationToken.None))).IntroTitle);
    }

    [Fact(DisplayName = "0022/AC-25: Okunduktan sonra başka bir kaydetmeyle değişmiş sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı metin değişmez.")]
    public async Task Reddeder_ParalelKayitlardanBirini()
    {
        // `0017` AC-32'nin bariyeri: iki komut da sürüm kontrolünü geçip KAYIT anında buluşur;
        // kaybı yalnız eşzamanlılık belirteci yakalayabilir (sıralı test bunu görmez).
        KayitBariyeri bariyer = new(2);
        Result<int>[] sonuclar = await Task.WhenAll(Enumerable.Range(0, 2).Select(i =>
            Task.Run(() => icerik.AnaSayfaKomutAsync(
                k => k.SaveAsync(1, [Girdi("tr", ("introTitle", $"Paralel {i}"))], CancellationToken.None),
                bariyer))));

        Assert.Single(sonuclar, item => item.IsSuccess);
        Assert.Single(sonuclar, item => item.IsFailure && item.Error.Code == ConcurrencyErrors.Lost);
    }

    [Fact(DisplayName = "0022/AC-36: Etkin dil listesinden çıkarılmış bir dilin alanları ekranda gösterilmez.")]
    public async Task Gostermez_EtkinOlmayanDili()
    {
        await KaydetAsync(Girdi("ar", ("introTitle", "عنوان محفوظ")));
        Icerik ikiDilli = new(database, "tr", "en");

        AdminHomeTextsDto panel = await ikiDilli.AnaSayfaOkuAsync(q => q.GetForAdminAsync(CancellationToken.None));

        Assert.Equal(["tr", "en"], panel.Locales.Select(l => l.Locale));
        Assert.Null((await ikiDilli.AnaSayfaOkuAsync(q => q.FindAsync("ar", CancellationToken.None))).IntroTitle);

        Result<int> yazma = await ikiDilli.AnaSayfaKomutAsync(k => k.SaveAsync(
            panel.Version,
            [Girdi("ar", ("introTitle", "x"))],
            CancellationToken.None));
        Assert.Equal("content.locale_not_enabled", Icerik.Alanlar(yazma.Error)["ar"]);
    }

    private static HomeTextInput Girdi(string locale, params (string Alan, string Deger)[] degerler) =>
        new(locale, degerler.ToDictionary(item => item.Alan, item => (string?)item.Deger, StringComparer.Ordinal));

    private async Task<int> SurumAsync() =>
        (await icerik.AnaSayfaOkuAsync(q => q.GetForAdminAsync(CancellationToken.None))).Version;

    private async Task KaydetAsync(HomeTextInput girdi)
    {
        int surum = await SurumAsync();
        Result<int> sonuc = await icerik.AnaSayfaKomutAsync(k => k.SaveAsync(surum, [girdi], CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
    }

    /// <summary>Arapça harf ve emoji (iki UTF-16 birimi, tek kod noktası) karışık, tam <paramref name="kodNoktasi"/>.</summary>
    internal static string ArapcaVeEmoji(int kodNoktasi)
    {
        string[] parca = ["س", "ي", "ا", "ر", "ة", " ", "🚗", "✨", "م"];
        return string.Concat(Enumerable.Range(0, kodNoktasi).Select(i => parca[i % parca.Length]));
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
}
