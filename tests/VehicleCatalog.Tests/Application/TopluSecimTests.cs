using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Listings;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0012c` **R3** — seçimin istek düzeyi kuralları. Saf: veritabanı yok, sınır bir
/// parametre ve testler sınırın **dâhil** olduğunu, seçimin **kırpılmadığını** ve
/// tekilleştirmenin sınırdan **önce** geldiğini ayrı ayrı ölçüyor.
/// </summary>
public sealed class TopluSecimTests
{
    [Fact(DisplayName = "0012c/AC-3: Yinelenen araç kimlikleri teklenir: aynı araç iki kez gönderildiğinde sonuç listesi o aracı **bir kez** taşır")]
    public void Tekler_YinelenenKimlikleri()
    {
        Guid arac = Guid.CreateVersion7();

        Result<BulkSelection> sonuc = BulkSelection.Create([arac, arac, arac], limit: 10);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal([arac], sonuc.Value.VehicleIds);
    }

    [Fact(DisplayName = "0012c/AC-4: Tekilleştirme sınır kontrolünden **önce** gelir: yalnız yinelemeler yüzünden sınırı aşan bir seçim kabul edilir")]
    public void Tekler_SinirKontrolundenOnce()
    {
        Guid birinci = Guid.CreateVersion7();
        Guid ikinci = Guid.CreateVersion7();

        // Beş kimlik, iki araç, sınır iki: tekilleştirme sonra gelseydi bu seçim
        // reddedilirdi — oysa personel iki araç seçmişti.
        Result<BulkSelection> sonuc = BulkSelection.Create(
            [birinci, ikinci, birinci, ikinci, birinci],
            limit: 2);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal([birinci, ikinci], sonuc.Value.VehicleIds);
    }

    [Fact(DisplayName = "0012c/AC-5: Üst sınırı aşan seçim **kırpılmaz, reddedilir**: yanıt `422` ve `listing.selection_limit_exceeded` taşır")]
    public void Reddeder_SiniriAsani_Kirpmadan()
    {
        List<Guid> secim = [.. Enumerable.Range(0, 4).Select(_ => Guid.CreateVersion7())];

        Result<BulkSelection> sonuc = BulkSelection.Create(secim, limit: 3);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(BulkSelectionErrors.LimitExceeded, sonuc.Error.Code);

        // Sınır yanıtta: personel neyi aştığını öğrenmeden seçimini küçültemez.
        Assert.Equal("3", sonuc.Error.Parameters!["limit"]);
    }

    [Fact(DisplayName = "0012c/AC-6: Üst sınır **dâhildir**: tam sınır kadar araç taşıyan seçim kabul edilir")]
    public void Kabul_Eder_TamSinirKadarSecimi()
    {
        List<Guid> secim = [.. Enumerable.Range(0, 3).Select(_ => Guid.CreateVersion7())];

        Result<BulkSelection> sonuc = BulkSelection.Create(secim, limit: 3);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(3, sonuc.Value.VehicleIds.Count);
    }

    /// <summary>
    /// Sıra **korunuyor**: satırlar gönderildiği sırada işleniyor (**D47**) ve sonuç
    /// listesi aynı sırayı taşıyor. Tekilleştirme sırayı bozsaydı o kriter sessizce
    /// yanlış olurdu.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-18: Satırlar **gönderildiği sırada** işlenir ve sonuç listesi aynı sırayı taşır")]
    public void Korur_GonderilenSirayi()
    {
        List<Guid> secim = [.. Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7())];

        Result<BulkSelection> sonuc = BulkSelection.Create([.. secim, secim[0]], limit: 10);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(secim, sonuc.Value.VehicleIds);
    }
}
