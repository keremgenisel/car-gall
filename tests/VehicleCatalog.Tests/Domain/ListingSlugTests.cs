using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class ListingSlugTests
{
    private static readonly ListingId Kimlik = new(Guid.Parse("0192c0de-0000-7000-8000-000000000001"));

    [Fact(DisplayName = "0002/AC-4: Aynı ilanın TR, AR ve EN adresleri aynı Latin slug'ı taşır; yalnızca dil öneki farklıdır")]
    public void AyniSlugUretilir_DildenBagimsiz()
    {
        // Slug kanonik Latin addan üretilir; locale hiç girdi değildir.
        ListingSlug slug = ListingSlug.Create("Toyota Corolla", 2019, Kimlik).Value;

        Assert.Equal("toyota-corolla-2019-000001", slug.Value);
    }

    [Fact]
    public void CakismazSonek_AyniAndaUretilenKimliklerde()
    {
        // Guid v7'nin baş baytları zaman damgasıdır: aynı milisaniyede üretilen iki kimlik
        // ilk altı hex karakteri paylaşır. Sonek son altı karakterden geldiği için çakışmaz.
        ListingId ilk = ListingId.New();
        ListingId ikinci = ListingId.New();

        string a = ListingSlug.Create("Toyota Corolla", 2019, ilk).Value.Value;
        string b = ListingSlug.Create("Toyota Corolla", 2019, ikinci).Value.Value;

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Degismez_ModelAdiSonradanDuzeltilse_Bile()
    {
        ListingSlug ilk = ListingSlug.Create("Toyota Corolla", 2019, Kimlik).Value;
        ListingSlug sonraki = ListingSlug.Create("Toyota Corolla Sedan", 2019, Kimlik).Value;

        // E9: slug ilk yayında üretilir ve saklanır. Bu test ÜRETİMİN girdiye duyarlı
        // olduğunu gösterir — kararlılık, yeniden üretmemekle sağlanır; saklanan değer
        // okuma modelinde durur ve bir daha hesaplanmaz.
        Assert.NotEqual(ilk.Value, sonraki.Value);
    }

    [Theory]
    [InlineData("Şahin Doğan", 2001, "sahin-dogan-2001-000001")]
    [InlineData("Işıl Ümit", 1999, "isil-umit-1999-000001")]
    [InlineData("BMW  3  Series", 2020, "bmw-3-series-2020-000001")]
    public void AsciiyeIndirger_TurkceKarakterleri(string modelAdi, int yil, string beklenen)
    {
        // lower() ile değil açık eşlemeyle: Türkçe collation altında I/İ sessizce bozulur.
        Assert.Equal(beklenen, ListingSlug.Create(modelAdi, yil, Kimlik).Value.Value);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("تويوتا")]
    [InlineData("")]
    public void Reddedilir_LatinOlmayanKanonikAd(string kanonikAd)
    {
        // Kanonik ad Latin olmak zorunda (architecture.md § 3); aksi hâlde adres yalnız
        // yıl ve sonekten oluşur ve hiçbir şey anlatmaz.
        Result<ListingSlug> sonuc = ListingSlug.Create(kanonikAd, 2019, Kimlik);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ListingSlug.InvalidSource, sonuc.Error.Code);
    }

    [Theory]
    [InlineData("Toyota-Corolla")]
    [InlineData("toyota corolla")]
    [InlineData("-toyota")]
    [InlineData("")]
    public void Taninmaz_BicimiTutmayanAdres(string deger)
    {
        Assert.False(ListingSlug.TryParse(deger, out _));
    }

    // ── 0002 E9 · kaçan hata (2026-11-26): uzunluk sınırı ──────────────────────────────────────

    /// <summary>`ReferenceNames`'in sınırları: marka 80, model 120; tam ad "marka model".</summary>
    private static readonly string EnUzunAd = "Marka" + new string('b', 75) + " " + "Model " + new string('m', 114);

    public static TheoryData<string, int> Girdiler() => new()
    {
        { EnUzunAd, 2019 },
        { new string('a', 201), 2024 },
        { string.Join(' ', Enumerable.Repeat("Kelime", 34)), 1999 },
        { "Şçğüöı İŞÇĞÜÖ " + new string('ğ', 150) + " Model", 2020 },
        { "Mercedes-Benz " + string.Join('-', Enumerable.Repeat("AMG", 60)), 2021 },
        { "Toyota Corolla", 2019 },
        { "a " + new string('b', 147), 2019 },
        { new string('c', 148) + " d", 2019 },
    };

    [Theory(DisplayName = "0002 E9 (kaçan hata): Create'in çıktısı her girdide TryParse'tan geçer ve en çok MaxLength (160) karakterdir; yıl ve kimlik soneki korunur")]
    [MemberData(nameof(Girdiler))]
    public void TryParsetanGecer_HerGirdide(string ad, int yil)
    {
        string slug = ListingSlug.Create(ad, yil, Kimlik).Value.Value;

        Assert.InRange(slug.Length, 1, ListingSlug.MaxLength);
        Assert.True(ListingSlug.TryParse(slug, out ListingSlug okunan));
        Assert.Equal(slug, okunan.Value);
        Assert.EndsWith($"-{yil}-000001", slug, StringComparison.Ordinal);
        Assert.DoesNotContain("--", slug, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): En uzun marka + model adı kelime sınırında kısalır: marka kalır, model kelimesi yarıda kesilmez")]
    public void KelimeSinirindaKisalir_EnUzunAd()
    {
        string slug = ListingSlug.Create(EnUzunAd, 2019, Kimlik).Value.Value;

        // Ad parçası 160 − "-2019-000001" = 148 karaktere sığmalı; 81. karakterden sonra gelen
        // "model-mmm…" kelimelerinden yarım kalanı atılır.
        Assert.Equal("marka" + new string('b', 75) + "-model-2019-000001", slug);
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): Sınırdan uzun TEK kelimelik ad sınırda kesilir; ad parçası tam 148 karakter")]
    public void SinirdaKesilir_TekKelime()
    {
        string slug = ListingSlug.Create(new string('a', 201), 2024, Kimlik).Value.Value;

        Assert.Equal(new string('a', 148) + "-2024-000001", slug);
        Assert.Equal(ListingSlug.MaxLength, slug.Length);
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): Sığan ad değişmez — tam 148 karakterlik ad kesilmez; bugünkü kısa adların adresi aynı")]
    public void Degismez_SiganAd()
    {
        string tam = "a-" + new string('b', 146);
        Assert.Equal(tam + "-2019-000001", ListingSlug.Create(tam.Replace('-', ' '), 2019, Kimlik).Value.Value);
        Assert.Equal("toyota-corolla-2019-000001", ListingSlug.Create("Toyota Corolla", 2019, Kimlik).Value.Value);
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): TryParse'ın sınırı MaxLength'tir: 160 karakter tanınır, 161 tanınmaz")]
    public void Sinir_TekSabittenGelir()
    {
        Assert.Equal(160, ListingSlug.MaxLength);
        Assert.True(ListingSlug.TryParse(new string('a', ListingSlug.MaxLength), out _));
        Assert.False(ListingSlug.TryParse(new string('a', ListingSlug.MaxLength + 1), out _));
    }
}
