using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0027` **D14** — özel günlerin tek kapısı (<see cref="SpecialHours.Parse"/>): her girdi bir alan
/// hatasına ya da geçerli bir değere döner, hiçbiri istisna fırlatmaz. Aynı kriterlerin uç yarısı
/// (`422`, asla `500`) Host'ta (`Host.Tests/OzelGunZinciriTests`).
/// </summary>
public sealed class OzelGunDogrulamaTests
{
    private static readonly DateOnly Bugun = new(2026, 11, 27);

    private static OpeningIntervalInput?[] Aralik(params string[] metinler) =>
        [.. metinler.Select(metin => (OpeningIntervalInput?)new OpeningIntervalInput(metin[..5], metin[6..]))];

    private static (SpecialHours? Sonuc, IReadOnlyDictionary<string, string> Hatalar) Dogrula(
        DateOnly bugun,
        params SpecialDayInput?[] kayitlar)
    {
        FieldErrors errors = new();
        SpecialHours? sonuc = SpecialHours.Parse(kayitlar, bugun, errors);
        Dictionary<string, string> hatalar = errors.Any
            ? errors.ToError().Parameters!.ToDictionary(pair => pair.Key, pair => pair.Value)
            : [];
        return (sonuc, hatalar);
    }

    public static TheoryData<string?> GecersizTarihler() =>
    [
        "2027-02-30", "2027-13-01", "27.11.2026", "2027-1-1", "", "2027-01-01\n", "2027-01\u00000-01", "2027-01-01 ", " 2027-01-01", null,
    ];

    [Theory(DisplayName = "0027/AC-10 (birim): geçersiz ya da eksik `from` → `specialHours.0.from`, `special_hours.invalid_date`; istisna yok")]
    [MemberData(nameof(GecersizTarihler))]
    public void GecersizTarih_From(string? tarih)
    {
        (SpecialHours? sonuc, IReadOnlyDictionary<string, string> hatalar) = Dogrula(Bugun, new SpecialDayInput(tarih, null, []));

        Assert.Null(sonuc);
        Assert.Equal(SpecialHours.InvalidDate, hatalar["specialHours.0.from"]);
    }

    [Fact(DisplayName = "0027/AC-10 (birim): şekil girdileri — `through` \"\", `[null]`, `intervals` yok, `[null]` aralık, eksik saat")]
    public void SekilGirdileri()
    {
        Assert.Equal(SpecialHours.InvalidDate, Dogrula(Bugun, new SpecialDayInput("2027-01-01", "", [])).Hatalar["specialHours.0.through"]);
        Assert.Equal(SpecialHours.InvalidEntry, Dogrula(Bugun, [null]).Hatalar["specialHours.0"]);
        Assert.Equal(SpecialHours.IntervalsRequired, Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, null)).Hatalar["specialHours.0.intervals"]);
        Assert.Equal(SpecialHours.InvalidEntry, Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, [null])).Hatalar["specialHours.0.intervals.0"]);

        IReadOnlyDictionary<string, string> saatsiz = Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, [new OpeningIntervalInput(null, null)])).Hatalar;
        Assert.Equal(OpeningHours.InvalidTime, saatsiz["specialHours.0.intervals.0.start"]);
        Assert.Equal(OpeningHours.InvalidTime, saatsiz["specialHours.0.intervals.0.end"]);
    }

    [Fact(DisplayName = "0027/AC-11, AC-12, AC-13: bitiş başlangıçtan önce; 61 gün reddedilir, 60 kabul; 366 gün ileri kabul, 367 reddedilir")]
    public void Sinirlar()
    {
        Assert.Equal(SpecialHours.EndBeforeStart, Dogrula(Bugun, new SpecialDayInput("2027-03-10", "2027-03-09", [])).Hatalar["specialHours.0.through"]);
        Assert.Equal(SpecialHours.TooLong, Dogrula(Bugun, new SpecialDayInput("2027-01-01", "2027-03-02", [])).Hatalar["specialHours.0.through"]);
        Assert.NotNull(Dogrula(Bugun, new SpecialDayInput("2027-01-01", "2027-03-01", [])).Sonuc);
        Assert.Equal(SpecialHours.TooFar, Dogrula(Bugun, new SpecialDayInput("2027-11-29", null, [])).Hatalar["specialHours.0.through"]);
        Assert.NotNull(Dogrula(Bugun, new SpecialDayInput("2027-11-28", null, [])).Sonuc);
    }

    [Fact(DisplayName = "0027/AC-14, AC-15, AC-16: çakışma sonraki kaydın `from`'unda; uç uca değen kabul; 13 kayıt reddedilir")]
    public void KumeKurallari()
    {
        Assert.Equal(
            SpecialHours.Overlap,
            Dogrula(Bugun, new SpecialDayInput("2027-03-08", "2027-03-10", []), new SpecialDayInput("2027-03-10", "2027-03-12", [])).Hatalar["specialHours.1.from"]);
        Assert.NotNull(Dogrula(Bugun, new SpecialDayInput("2027-03-08", "2027-03-09", []), new SpecialDayInput("2027-03-10", "2027-03-12", [])).Sonuc);

        SpecialDayInput?[] onUc = [.. Enumerable.Range(0, 13).Select(gun => (SpecialDayInput?)new SpecialDayInput($"2027-01-{gun + 1:00}", null, []))];
        Assert.Equal(SpecialHours.TooMany, Dogrula(Bugun, onUc).Hatalar["specialHours"]);
    }

    [Fact(DisplayName = "0027/AC-17, AC-18, AC-19: aralık kuralları `0025`'in kodlarıyla, kaydın alanında")]
    public void AralikKurallari()
    {
        Assert.Equal(OpeningHours.InvalidTime, Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, [new OpeningIntervalInput("9:00", "13:00")])).Hatalar["specialHours.0.intervals.0.start"]);
        Assert.Equal(OpeningHours.Overlap, Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, Aralik("09:00-13:00", "13:00-18:00"))).Hatalar["specialHours.0"]);
        Assert.Equal(OpeningHours.TooManyIntervals, Dogrula(Bugun, new SpecialDayInput("2027-01-01", null, Aralik("08:00-09:00", "10:00-11:00", "12:00-13:00", "14:00-15:00"))).Hatalar["specialHours.0"]);
    }

    [Fact(DisplayName = "0027/AC-104, AC-23 (birim): doğrulama düşürmeden ÖNCE; indeks gönderilenindir; geçerli geçmiş kayıt sessizce düşer")]
    public void DusurmedenOnce()
    {
        DateOnly bugun = new(2027, 1, 5);
        (SpecialHours? sonuc, IReadOnlyDictionary<string, string> hatalar) = Dogrula(
            bugun,
            new SpecialDayInput("2027-01-01", "2027-01-01", []),
            new SpecialDayInput("2027-02-10", "2027-02-01", []));
        Assert.Null(sonuc);
        Assert.Equal(SpecialHours.EndBeforeStart, hatalar["specialHours.1.through"]);

        Assert.Equal(SpecialHours.EndBeforeStart, Dogrula(bugun, new SpecialDayInput("2027-01-03", "2027-01-01", [])).Hatalar["specialHours.0.through"]);

        SpecialHours? kalan = Dogrula(bugun, new SpecialDayInput("2027-01-01", null, []), new SpecialDayInput("2027-02-10", null, [])).Sonuc;
        Assert.Equal([new DateOnly(2027, 2, 10)], kalan!.Days.Select(day => day.From));
    }
}
