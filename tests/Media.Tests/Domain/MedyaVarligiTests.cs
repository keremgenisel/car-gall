using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;

namespace CarGallery.Media.Tests.Domain;

/// <summary>M1'in durum makinesi ve R9'un `alt` alanı. Veritabanı yok, mock yok.</summary>
public sealed class MedyaVarligiTests
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    private static MediaAsset Varlik() => MediaAsset.Create(
        Guid.CreateVersion7(),
        "image/jpeg",
        "private/originals/x.jpg",
        byteSize: 1024,
        sourceWidth: 1200,
        sourceHeight: 800,
        Simdi);

    [Fact(DisplayName = "0011/AC-8: Yüklenen varlık `Yüklendi` durumunda, depolama yolu ve kimliğiyle kaydedilir")]
    public void Kaydedilir_Varlik_YuklendiDurumunda()
    {
        MediaAsset asset = Varlik();

        Assert.Equal(MediaProcessingStatus.Uploaded, asset.Status);
        Assert.Equal("private/originals/x.jpg", asset.OriginalObjectKey);
        Assert.NotEqual(Guid.Empty, asset.Id);
    }

    [Fact(DisplayName = "0011/AC-9: İşleme başladığında durum `İşleniyor`, başarıyla bittiğinde `Hazır` olur")]
    public void Gecer_Durum_IsleniyordanHazira()
    {
        MediaAsset asset = Varlik();

        Assert.True(asset.BeginProcessing(Simdi).IsSuccess);
        Assert.Equal(MediaProcessingStatus.Processing, asset.Status);

        Result hazir = asset.MarkReady(
            [new MediaDerivative { VariantCode = "display", Width = 800, Height = 533, ObjectKey = "public/a" }],
            Simdi);

        Assert.True(hazir.IsSuccess);
        Assert.Equal(MediaProcessingStatus.Ready, asset.Status);
        Assert.Single(asset.Derivatives);
    }

    [Theory(DisplayName = "0011/AC-10: Tanımlı olmayan durum geçişi yapılamaz")]
    [InlineData("hazir-sonra-islemeye")]
    [InlineData("basarisiz-sonra-hazira")]
    [InlineData("yuklendi-dogrudan-hazira")]
    [InlineData("islemede-tekrar-islemeye")]
    public void Yapilamaz_TanimsizGecis(string senaryo)
    {
        MediaAsset asset = Varlik();

        Result sonuc = senaryo switch
        {
            "yuklendi-dogrudan-hazira" => asset.MarkReady([], Simdi),
            "islemede-tekrar-islemeye" => Islemede(asset).BeginProcessing(Simdi),
            "hazir-sonra-islemeye" => Hazir(asset).BeginProcessing(Simdi),
            _ => Basarisiz(asset).MarkReady([], Simdi),
        };

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.InvalidStatusTransition, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0011/AC-11: Denemeler tükendiğinde durum `Başarısız` olur; varlık `Yüklendi` durumunda kalmaz")]
    public void Olur_Durum_Basarisiz_DenemelerTukendiginde()
    {
        MediaAsset asset = Varlik();
        const int enFazla = 3;

        // İki tur başarısız ama hak var: varlık `Yüklendi`'ye döner (M1).
        for (int tur = 1; tur < enFazla; tur++)
        {
            Assert.True(asset.BeginProcessing(Simdi).IsSuccess);
            Assert.True(asset.HasAttemptsLeft(enFazla));
            Assert.True(asset.Reschedule("media.storage_unavailable", Simdi, TimeSpan.FromSeconds(30)).IsSuccess);
            Assert.Equal(MediaProcessingStatus.Uploaded, asset.Status);
        }

        // Üçüncü tur: hak tükendi.
        Assert.True(asset.BeginProcessing(Simdi).IsSuccess);
        Assert.False(asset.HasAttemptsLeft(enFazla));
        Assert.True(asset.MarkFailed("media.storage_unavailable", Simdi).IsSuccess);

        Assert.Equal(MediaProcessingStatus.Failed, asset.Status);
        Assert.NotEqual(MediaProcessingStatus.Uploaded, asset.Status);
        Assert.Equal("media.storage_unavailable", asset.FailureReason);
    }

    [Fact]
    public void Ileriye_Atar_SonrakiDeneme_ArtanAralikla()
    {
        MediaAsset asset = Varlik();

        asset.BeginProcessing(Simdi);
        asset.Reschedule("x", Simdi, TimeSpan.FromSeconds(30));
        DateTimeOffset birinci = asset.NextAttemptAt;

        asset.BeginProcessing(Simdi);
        asset.Reschedule("x", Simdi, TimeSpan.FromSeconds(30));
        DateTimeOffset ikinci = asset.NextAttemptAt;

        // Taban 30 sn: birinci tur +30, ikinci tur +60. Sabit aralık olsaydı bozuk bir
        // dosya turu her beş saniyede bir meşgul ederdi.
        Assert.Equal(Simdi.AddSeconds(30), birinci);
        Assert.Equal(Simdi.AddSeconds(60), ikinci);
    }

    [Fact(DisplayName = "0011/AC-27: Bir varlık etkin her locale için ayrı `alt` metni taşıyabilir")]
    public void Tasiyabilir_AltMetni_LocaleBasina()
    {
        MediaAsset asset = Varlik();

        asset.SetAltText(
            LocalizedText.From(
            [
                new KeyValuePair<Locale, string>(Locale.Create("tr").Value, "beyaz BMW 320i, önden"),
                new KeyValuePair<Locale, string>(Locale.Create("ar").Value, "بي إم دبليو 320i بيضاء، من الأمام"),
                new KeyValuePair<Locale, string>(Locale.Create("en").Value, "white BMW 320i, front view"),
            ]),
            Simdi);

        Dictionary<string, string> metinler = asset.AltText.ToDictionary();

        Assert.Equal(3, metinler.Count);
        Assert.Equal("beyaz BMW 320i, önden", metinler["tr"]);
        Assert.Equal("بي إم دبليو 320i بيضاء، من الأمام", metinler["ar"]);
        Assert.Equal("white BMW 320i, front view", metinler["en"]);
    }

    [Fact(DisplayName = "0011/AC-28: `alt` metni boş olan bir varlık yine hazır olabilir")]
    public void Olabilir_Hazir_AltMetniBosOlsaDa()
    {
        MediaAsset asset = Varlik();
        asset.BeginProcessing(Simdi);

        Result hazir = asset.MarkReady(
            [new MediaDerivative { VariantCode = "display", Width = 800, Height = 533, ObjectKey = "public/a" }],
            Simdi);

        Assert.True(hazir.IsSuccess);
        Assert.Equal(MediaProcessingStatus.Ready, asset.Status);
        Assert.Empty(asset.AltText.ToDictionary());
    }

    /// <summary>
    /// R10/M2 — medya sahipsizdir. Agregatta hiçbir araç/ilan/şube kimliği bulunmadığını
    /// TİP ÜZERİNDEN sınıyor: yorum değil, kapı. İleride biri `VehicleId` eklerse burası
    /// kırmızı verir ve Yasak Listesi 33 sessizce delinmez.
    /// </summary>
    [Fact]
    public void Tasimaz_Varlik_BaskaModulunKimligini()
    {
        string[] yasakli = ["VehicleId", "ListingId", "BranchId", "StaffId", "LeadId"];

        IEnumerable<string> bulunan = typeof(MediaAsset)
            .GetProperties(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic)
            .Select(property => property.Name)
            .Where(yasakli.Contains);

        Assert.Empty(bulunan);
    }

    private static MediaAsset Islemede(MediaAsset asset)
    {
        asset.BeginProcessing(Simdi);
        return asset;
    }

    private static MediaAsset Hazir(MediaAsset asset)
    {
        asset.BeginProcessing(Simdi);
        asset.MarkReady([], Simdi);
        return asset;
    }

    private static MediaAsset Basarisiz(MediaAsset asset)
    {
        asset.BeginProcessing(Simdi);
        asset.MarkFailed("x", Simdi);
        return asset;
    }
}
