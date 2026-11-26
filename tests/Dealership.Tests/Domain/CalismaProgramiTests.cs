using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0025` D3, D4, D11, D12 — çalışma programının alan kuralları, veritabanı yok. Telden çıkan hâli
/// (`422`, kalıcılık, sürüm) `Host.Tests/CalismaSaatleriZinciriTests`'te.
/// </summary>
public sealed class CalismaProgramiTests
{
    [Theory(DisplayName = "0025/AC-10, AC-11, AC-12: `HH:mm` dışındaki saat `opening_hours.invalid_time` ile aralığın kendi alanında reddedilir.")]
    [InlineData("9:00", "18:00", "openingHours.monday.0.start")]
    [InlineData("09:00", "24:00", "openingHours.monday.0.end")]
    [InlineData("12:60", "18:00", "openingHours.monday.0.start")]
    [InlineData("09:00", null, "openingHours.monday.0.end")]
    [InlineData("0900", "18:00", "openingHours.monday.0.start")]
    [InlineData("09:00:00", "18:00", "openingHours.monday.0.start")]
    [InlineData("٠٩:٠٠", "18:00", "openingHours.monday.0.start")]
    public void Reddedilir_GecersizSaat(string? baslangic, string? bitis, string alan)
    {
        FieldErrors hatalar = new();

        Assert.Null(OpeningHours.Parse(Program(("monday", [new(baslangic, bitis)])), hatalar));

        Assert.Equal(OpeningHours.InvalidTime, hatalar.ToError().Parameters![alan]);
    }

    [Theory(DisplayName = "0025/AC-13, AC-14: Bitiş başlangıçtan sonra değilse `openingHours.monday.0.end` alanında `opening_hours.end_not_after_start`.")]
    [InlineData("18:00", "09:00")]
    [InlineData("09:00", "09:00")]
    public void Reddedilir_BitisBaslangictanSonraDegil(string baslangic, string bitis)
    {
        FieldErrors hatalar = new();

        Assert.Null(OpeningHours.Parse(Program(("monday", [new(baslangic, bitis)])), hatalar));

        Assert.Equal(OpeningHours.EndNotAfterStart, hatalar.ToError().Parameters!["openingHours.monday.0.end"]);
    }

    [Fact(DisplayName = "0025/AC-15: Dört aralık `openingHours.monday` alanında `opening_hours.too_many_intervals`.")]
    public void Reddedilir_DortAralik()
    {
        FieldErrors hatalar = new();

        Assert.Null(OpeningHours.Parse(Program(("monday", [new("08:00", "09:00"), new("10:00", "11:00"), new("12:00", "13:00"), new("14:00", "15:00")])), hatalar));

        Assert.Equal(OpeningHours.TooManyIntervals, hatalar.ToError().Parameters!["openingHours.monday"]);
    }

    [Theory(DisplayName = "0025/AC-16, AC-17: Çakışan ve uç uca değen aralıklar `openingHours.monday` alanında `opening_hours.overlap`.")]
    [InlineData("12:00", "18:00")]
    [InlineData("13:00", "18:00")]
    [InlineData("10:00", "11:00")]
    public void Reddedilir_CakismaVeUcUca(string baslangic, string bitis)
    {
        FieldErrors hatalar = new();

        Assert.Null(OpeningHours.Parse(Program(("monday", [new("09:00", "13:00"), new(baslangic, bitis)])), hatalar));

        Assert.Equal(OpeningHours.Overlap, hatalar.ToError().Parameters!["openingHours.monday"]);
    }

    [Fact(DisplayName = "0025/AC-18, AC-70: Bilinmeyen gün kodu kendi alanında `opening_hours.unknown_day`; harfe duyarlıdır (`Monday`).")]
    public void Reddedilir_BilinmeyenGun()
    {
        FieldErrors hatalar = new();
        Dictionary<string, IReadOnlyList<OpeningIntervalInput>?> program = Program();
        program["funday"] = [];
        program["Monday"] = [new("09:00", "18:00")];

        Assert.Null(OpeningHours.Parse(program, hatalar));

        IReadOnlyDictionary<string, string> alanlar = hatalar.ToError().Parameters!;
        Assert.Equal(OpeningHours.UnknownDay, alanlar["openingHours.funday"]);
        Assert.Equal(OpeningHours.UnknownDay, alanlar["openingHours.Monday"]);
    }

    [Fact(DisplayName = "0025/AC-68, AC-69: Eksik gün kendi alanında `opening_hours.missing_day`; boş nesne yedi günün hepsi için.")]
    public void Reddedilir_EksikGun()
    {
        FieldErrors eksikPazar = new();
        Dictionary<string, IReadOnlyList<OpeningIntervalInput>?> program = Program();
        program.Remove("sunday");
        Assert.Null(OpeningHours.Parse(program, eksikPazar));
        Assert.Equal("openingHours.sunday", eksikPazar.ToError().Parameters!["fields"]);
        Assert.Equal(OpeningHours.MissingDay, eksikPazar.ToError().Parameters!["openingHours.sunday"]);

        FieldErrors bos = new();
        Assert.Null(OpeningHours.Parse(new Dictionary<string, IReadOnlyList<OpeningIntervalInput>?>(), bos));
        Assert.All(OpeningHours.Days, gun => Assert.Equal(OpeningHours.MissingDay, bos.ToError().Parameters![$"openingHours.{gun}"]));
    }

    [Fact(DisplayName = "0025/AC-19, AC-23: Aralıklar başlangıca göre sıralı saklanır; 00:00–23:59 kabul edilir.")]
    public void Siralar_VeKabulEder()
    {
        FieldErrors hatalar = new();

        OpeningHours program = OpeningHours.Parse(
            Program(("monday", [new("16:00", "21:00"), new("09:00", "13:00")]), ("tuesday", [new("00:00", "23:59")])),
            hatalar)!;

        Assert.False(hatalar.Any);
        Assert.Equal(["09:00-13:00", "16:00-21:00"], program.InOrder().First().Intervals.Select(item => $"{item.StartText}-{item.EndText}"));
        Assert.Equal("00:00-23:59", Assert.Single(program.InOrder().ElementAt(1).Intervals) is { } aralik ? $"{aralik.StartText}-{aralik.EndText}" : null);
    }

    [Fact(DisplayName = "0025/AC-72, AC-21: Yedi boş gün `missing_day` döndürmez ve boş programdır; iki günün hatası birlikte gelir.")]
    public void BosProgram_VeIkiGununHatasi()
    {
        FieldErrors hatalar = new();
        OpeningHours bos = OpeningHours.Parse(Program(), hatalar)!;
        Assert.False(hatalar.Any);
        Assert.True(bos.IsEmpty);

        FieldErrors iki = new();
        Assert.Null(OpeningHours.Parse(Program(("monday", [new("9:00", "18:00")]), ("wednesday", [new("18:00", "09:00")])), iki));
        Assert.Equal("openingHours.monday.0.start,openingHours.wednesday.0.end", iki.ToError().Parameters!["fields"]);
    }

    [Fact(DisplayName = "0025/AC-42: JSON gidiş-dönüşü D8'in biçimini ve D6'nın sırasını korur.")]
    public void GidisDonus_Json()
    {
        OpeningHours program = OpeningHours.Parse(Program(("saturday", [new("10:00", "14:00")]), ("monday", [new("09:00", "18:00")])), new FieldErrors())!;

        string json = program.ToJson();

        Assert.Equal(json, OpeningHours.FromJson(json).ToJson());
        Assert.StartsWith("{\"monday\":[{\"start\":\"09:00\",\"end\":\"18:00\"}],\"tuesday\":[]", json, StringComparison.Ordinal);
        Assert.Contains("\"saturday\":[{\"start\":\"10:00\",\"end\":\"14:00\"}],\"sunday\":[]}", json, StringComparison.Ordinal);
    }

    private static Dictionary<string, IReadOnlyList<OpeningIntervalInput>?> Program(params (string Gun, OpeningIntervalInput[] Araliklar)[] gunler)
    {
        Dictionary<string, IReadOnlyList<OpeningIntervalInput>?> program = OpeningHours.Days.ToDictionary(
            gun => gun,
            _ => (IReadOnlyList<OpeningIntervalInput>?)[],
            StringComparer.Ordinal);

        foreach ((string gun, OpeningIntervalInput[] araliklar) in gunler)
        {
            program[gun] = araliklar;
        }

        return program;
    }
}
