using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// AC-21 veritabanı GÖRMEDEN sınanıyor: dil tamlığı saf bir karardır ve saf bir metotta
/// yaşıyor. Sorgu tarafında sınamak, aynı kuralı test edilemez bir SQL'in içine gömerdi.
/// </summary>
public sealed class IlanMetniTamligiTests
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale En = Locale.Create("en").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;
    private static readonly IReadOnlyList<Locale> Etkin = [Tr, En, Ar];

    [Fact(DisplayName = "0010b/AC-21: Bir dil, başlık ve açıklamasının ikisi de doluysa tam sayılır; yalnız biri dolu olan dil eksik sayılır")]
    public void Eksiktir_YalnizBiriDoluOlanDil()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        listing.SetText(
            Metin((Tr, "Başlık"), (En, "Title"), (Ar, "عنوان")),
            // Arapça açıklama YOK: başlığı dolu ama açıklaması boş.
            Metin((Tr, "Açıklama"), (En, "Description")));

        IReadOnlyList<Locale> eksikler = listing.MissingLocales(Etkin);

        Assert.Equal([Ar], [.. eksikler]);
    }

    [Fact(DisplayName = "0010b/AC-21: Bir dil, başlık ve açıklamasının ikisi de doluysa tam sayılır; yalnız biri dolu olan dil eksik sayılır")]
    public void Tamdir_IkisiDeDoluOlanDil()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        listing.SetText(
            Metin((Tr, "Başlık"), (En, "Title"), (Ar, "عنوان")),
            Metin((Tr, "Açıklama"), (En, "Description"), (Ar, "وصف")));

        Assert.Empty(listing.MissingLocales(Etkin));
    }

    [Fact]
    public void Eksiktir_HicMetniOlmayanIlanTumDillerde()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        Assert.Equal(Etkin, [.. listing.MissingLocales(Etkin)]);
    }

    /// <summary>
    /// Boş dize DOLU sayılmaz. Aksi hâlde formda boşluk bırakıp kaydeden personel o dili
    /// "tam" görürdü ve gösterge tam da gizlemesi gereken şeyi gizlerdi.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Eksiktir_BoslukIcerenMetin(string bos)
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        listing.SetText(Metin((Tr, bos)), Metin((Tr, "Açıklama")));

        Assert.Contains(Tr, listing.MissingLocales([Tr]));
    }

    [Fact(DisplayName = "0010b/AC-20: İlan başlığı veya açıklaması locale başına üst uzunluk sınırını aştığında istek reddedilir")]
    public void Reddedilir_UzunlukSiniriAsildiginda()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        Result uzunBaslik = listing.SetText(
            Metin((Tr, new string('B', Listing.MaximumTitleLength + 1))),
            LocalizedText.Empty);

        Result uzunAciklama = listing.SetText(
            LocalizedText.Empty,
            Metin((Tr, new string('A', Listing.MaximumDescriptionLength + 1))));

        // İKİ AYRI KOD: tek koda eşlendiğinde komut katmanı hangi alanı işaretleyeceğini
        // bilemiyor ve açıklamayı uzun yazan personele başlık alanı kırmızı gösteriliyordu.
        Assert.Equal(Listing.TitleTooLong, uzunBaslik.Error.Code);
        Assert.Equal(Listing.DescriptionTooLong, uzunAciklama.Error.Code);
    }

    /// <summary>Sınır LOCALE BAŞINA: üç dilde yazan galeri, tek dilde yazandan erken kesilmez.</summary>
    [Fact]
    public void Kabul_SinirinAltindaUcDilBirden()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        string tamSinir = new('B', Listing.MaximumTitleLength);

        Result sonuc = listing.SetText(
            Metin((Tr, tamSinir), (En, tamSinir), (Ar, tamSinir)),
            LocalizedText.Empty);

        Assert.True(sonuc.IsSuccess);
    }

    private static LocalizedText Metin(params (Locale Locale, string Text)[] values) =>
        LocalizedText.From(values.Select(item => new KeyValuePair<Locale, string>(item.Locale, item.Text)));
}
