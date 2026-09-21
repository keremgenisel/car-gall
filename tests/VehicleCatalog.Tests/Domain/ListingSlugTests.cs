using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class ListingSlugTests
{
    private static readonly ListingId Kimlik = new(Guid.Parse("0192c0de-0000-7000-8000-000000000001"));

    [Fact(DisplayName = "AC-4: Aynı ilanın TR, AR ve EN adresleri aynı Latin slug'ı taşır; yalnızca dil öneki farklıdır")]
    public void AyniSlugUretilir_DildenBagimsiz()
    {
        // Slug kanonik Latin addan üretilir; locale hiç girdi değildir.
        ListingSlug slug = ListingSlug.Create("Toyota Corolla", 2019, Kimlik).Value;

        Assert.Equal("toyota-corolla-2019-0192c0", slug.Value);
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
    [InlineData("Şahin Doğan", 2001, "sahin-dogan-2001-0192c0")]
    [InlineData("Işıl Ümit", 1999, "isil-umit-1999-0192c0")]
    [InlineData("BMW  3  Series", 2020, "bmw-3-series-2020-0192c0")]
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
}
