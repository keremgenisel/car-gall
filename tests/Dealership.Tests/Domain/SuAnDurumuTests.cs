using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0027` **D6**, **D7**, **D8** — "şu an" hesabının saf işlevi (<see cref="BranchClock"/>): sabit an,
/// gerçek tzdata. Aynı kriterler Host'ta gerçek uçla ve sabit `IClock`'la da ölçülür
/// (`Host.Tests/SuAnDurumuZinciriTests`); burası algoritmanın bütün dallarını hızlıca tarar.
/// </summary>
public sealed class SuAnDurumuTests
{
    private static readonly string[] Gunler = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    /// <summary>"Programlı şube": Pzt–Cum 09:00–18:00, Cmt 10:00–14:00, Pazar kapalı.</summary>
    private static Dictionary<string, string[]> Programli() => new()
    {
        ["monday"] = ["09:00-18:00"],
        ["tuesday"] = ["09:00-18:00"],
        ["wednesday"] = ["09:00-18:00"],
        ["thursday"] = ["09:00-18:00"],
        ["friday"] = ["09:00-18:00"],
        ["saturday"] = ["10:00-14:00"],
    };

    private static OpeningHours Haftalik(Dictionary<string, string[]> program)
    {
        FieldErrors errors = new();
        OpeningHours? parsed = OpeningHours.Parse(
            Gunler.ToDictionary(
                gun => gun,
                gun => (IReadOnlyList<OpeningIntervalInput>?)[.. (program.GetValueOrDefault(gun) ?? []).Select(Aralik)]),
            errors);
        Assert.False(errors.Any);
        return parsed!;
    }

    private static SpecialHours Ozel(params (string From, string Through, string[] Aralik)[] kayitlar)
    {
        FieldErrors errors = new();
        SpecialHours? parsed = SpecialHours.Parse(
            [.. kayitlar.Select(kayit => (SpecialDayInput?)new SpecialDayInput(kayit.From, kayit.Through, [.. kayit.Aralik.Select(Aralik)]))],
            kayitlar.Min(kayit => DateOnly.Parse(kayit.From, System.Globalization.CultureInfo.InvariantCulture)),
            errors);
        Assert.False(errors.Any);
        return parsed!;
    }

    private static OpeningIntervalInput Aralik(string metin) => new(metin[..5], metin[6..]);

    private static OpenNow Hesapla(
        Dictionary<string, string[]> program,
        string zaman,
        string bolge = "Europe/Istanbul",
        SpecialHours? ozel = null) =>
        BranchClock.Compute(
            Haftalik(program),
            ozel ?? SpecialHours.Empty,
            TimeZoneInfo.FindSystemTimeZoneById(bolge),
            DateTimeOffset.Parse(zaman, System.Globalization.CultureInfo.InvariantCulture));

    private static void Beklenen(OpenNow durum, bool acik, string bugun, string? sonraki, int? sure = null)
    {
        Assert.Equal(acik, durum.Open);
        Assert.Equal(DateOnly.Parse(bugun, System.Globalization.CultureInfo.InvariantCulture), durum.Today);
        Assert.Equal(sonraki, durum.Next is { } next ? $"{next.Date:yyyy-MM-dd} {next.Time:HH\\:mm}" : null);
        Assert.InRange(durum.RefreshInSeconds, 1, 90000);

        if (sure is { } beklenen)
        {
            Assert.Equal(beklenen, durum.RefreshInSeconds);
        }
    }

    [Fact(DisplayName = "0027/AC-30, AC-31, AC-32, AC-98: programlı şube — açık, akşam kapalı, Cumartesi öğleden sonra kapalı; sınır anları")]
    public void Programli_Durumlar()
    {
        Beklenen(Hesapla(Programli(), "2026-12-04T09:00Z"), true, "2026-12-04", "2026-12-04 18:00", 21600);
        Beklenen(Hesapla(Programli(), "2026-12-04T16:00Z"), false, "2026-12-04", "2026-12-05 10:00", 18000);
        Beklenen(Hesapla(Programli(), "2026-12-05T12:00Z"), false, "2026-12-05", "2026-12-07 09:00");
        Assert.False(Hesapla(Programli(), "2026-12-04T15:00:00Z").Open);
        Assert.True(Hesapla(Programli(), "2026-12-04T06:00:00Z").Open);
        Beklenen(Hesapla(Programli(), "2026-12-04T14:59:59.5Z"), true, "2026-12-04", "2026-12-04 18:00", 1);
    }

    [Fact(DisplayName = "0027/AC-33: Asia/Kuwait çift mesai, Cuma–Cumartesi kapalı — öğle arasında kapalı, açılış 16:00")]
    public void Kuveyt_CiftMesai()
    {
        Dictionary<string, string[]> program = new()
        {
            ["sunday"] = ["09:00-13:00", "16:00-21:00"],
            ["monday"] = ["09:00-13:00", "16:00-21:00"],
            ["tuesday"] = ["09:00-13:00", "16:00-21:00"],
            ["wednesday"] = ["09:00-13:00", "16:00-21:00"],
            ["thursday"] = ["09:00-13:00", "16:00-21:00"],
        };

        Beklenen(Hesapla(program, "2026-12-06T11:00Z", "Asia/Kuwait"), false, "2026-12-06", "2026-12-06 16:00");
    }

    [Fact(DisplayName = "0027/AC-34, AC-35, AC-36, AC-96, AC-97: özel gün haftalığı ezer (tam gün kapalı, kısa mesai); açılış özel günlerin ardından")]
    public void OzelGun_Ezer()
    {
        Beklenen(Hesapla(Programli(), "2027-01-01T09:00Z", ozel: Ozel(("2027-01-01", "2027-01-01", []))), false, "2027-01-01", "2027-01-02 10:00");

        SpecialHours ramazan = Ozel(("2027-02-08", "2027-03-09", ["10:00-16:00"]), ("2027-03-10", "2027-03-12", []));
        Beklenen(Hesapla(Programli(), "2027-03-09T14:00Z", ozel: ramazan), false, "2027-03-09", "2027-03-13 10:00");
        Beklenen(Hesapla(Programli(), "2027-03-09T08:00Z", ozel: ramazan), true, "2027-03-09", "2027-03-09 16:00");

        Beklenen(Hesapla(Programli(), "2026-12-05T09:00Z", ozel: Ozel(("2026-12-05", "2026-12-05", []))), false, "2026-12-05", "2026-12-07 09:00");
        Beklenen(Hesapla(Programli(), "2027-01-03T09:00Z", ozel: Ozel(("2027-01-04", "2027-01-13", []))), false, "2027-01-03", "2027-01-14 09:00");
    }

    [Fact(DisplayName = "0027/AC-37, AC-38, AC-93, AC-94: gece yarısı zinciri; sürekli açık; gün sonunda kapanış")]
    public void GeceYarisi_Zinciri()
    {
        Dictionary<string, string[]> gece = new() { ["friday"] = ["20:00-23:59"], ["saturday"] = ["00:00-02:00", "10:00-14:00"] };
        Beklenen(Hesapla(gece, "2026-12-04T20:30Z"), true, "2026-12-04", "2026-12-05 02:00");
        Assert.True(Hesapla(gece, "2026-12-04T20:59:30Z").Open);

        Dictionary<string, string[]> herGun = Gunler.ToDictionary(gun => gun, _ => new[] { "00:00-23:59" });
        Beklenen(Hesapla(herGun, "2026-12-04T09:00Z"), true, "2026-12-04", null);

        Beklenen(Hesapla(new() { ["friday"] = ["20:00-23:59"] }, "2026-12-04T18:00Z"), true, "2026-12-04", "2026-12-05 00:00", 10800);

        Dictionary<string, string[]> pazarKapali = Gunler.Where(gun => gun != "sunday").ToDictionary(gun => gun, _ => new[] { "00:00-23:59" });
        Beklenen(Hesapla(pazarKapali, "2026-12-07T09:00Z"), true, "2026-12-07", "2026-12-13 00:00", 43200);
    }

    [Fact(DisplayName = "0027/AC-39, AC-41, AC-92: bölge kuralı — Berlin yaz saati sonrası açık; Tokyo'da kapalı; New York'ta yerel tarih")]
    public void SaatDilimi()
    {
        Assert.True(Hesapla(new() { ["monday"] = ["09:00-18:00"] }, "2027-03-29T07:30Z", "Europe/Berlin").Open);
        Beklenen(Hesapla(Programli(), "2026-12-04T09:00Z", "Asia/Tokyo"), false, "2026-12-04", "2026-12-05 10:00");
        Beklenen(Hesapla(Programli(), "2026-12-05T02:00Z", "America/New_York"), false, "2026-12-04", "2026-12-05 10:00", 10800);
    }

    [Fact(DisplayName = "0027/AC-84, AC-85, AC-95: yaz saati günü 23 ve 25 saat — gece yarısına gerçek saniye")]
    public void YazSaati_GunUzunlugu()
    {
        Dictionary<string, string[]> pazartesi = new() { ["monday"] = ["09:00-18:00"] };
        Beklenen(Hesapla(pazartesi, "2027-03-27T23:00Z", "Europe/Berlin"), false, "2027-03-28", "2027-03-29 09:00", 82800);
        Assert.Equal(90000, Hesapla(pazartesi, "2027-10-30T22:00Z", "Europe/Berlin").RefreshInSeconds);
        Assert.Equal(81000, Hesapla(pazartesi, "2027-03-27T23:30Z", "Europe/Berlin").RefreshInSeconds);
    }

    [Fact(DisplayName = "0027/AC-86, AC-87, AC-88, AC-89, AC-90, AC-91: geçişi içine alan, var olmayan ve iki kez yaşanan saatler; Beirut'un var olmayan gece yarısı")]
    public void YazSaati_Gecisler()
    {
        Beklenen(Hesapla(new() { ["sunday"] = ["01:00-04:00"] }, "2027-03-28T00:30Z", "Europe/Berlin"), true, "2027-03-28", "2027-03-28 04:00", 5400);

        Dictionary<string, string[]> yokSaat = new() { ["sunday"] = ["02:30-05:00"] };
        Beklenen(Hesapla(yokSaat, "2027-03-28T00:45Z", "Europe/Berlin"), false, "2027-03-28", "2027-03-28 03:00", 900);
        Beklenen(Hesapla(yokSaat, "2027-03-28T01:00Z", "Europe/Berlin"), true, "2027-03-28", "2027-03-28 05:00", 7200);

        Beklenen(Hesapla(new() { ["sunday"] = ["01:00-04:00"] }, "2027-10-31T00:30Z", "Europe/Berlin"), true, "2027-10-31", "2027-10-31 04:00", 9000);

        Beklenen(Hesapla(yokSaat, "2027-10-31T00:15Z", "Europe/Berlin"), false, "2027-10-31", "2027-10-31 02:30", 900);
        Beklenen(Hesapla(yokSaat, "2027-10-31T01:15Z", "Europe/Berlin"), true, "2027-10-31", "2027-10-31 05:00", 9900);

        Dictionary<string, string[]> ikiKez = new() { ["sunday"] = ["01:00-02:30"] };
        Assert.True(Hesapla(ikiKez, "2027-10-31T00:15Z", "Europe/Berlin").Open);
        Assert.Equal(900, Hesapla(ikiKez, "2027-10-31T00:15Z", "Europe/Berlin").RefreshInSeconds);
        Assert.False(Hesapla(ikiKez, "2027-10-31T01:15Z", "Europe/Berlin").Open);

        Dictionary<string, string[]> beyrut = new() { ["saturday"] = ["20:00-23:59"], ["sunday"] = ["00:00-02:00"] };
        Beklenen(Hesapla(beyrut, "2027-03-27T21:30Z", "Asia/Beirut"), true, "2027-03-27", "2027-03-28 02:00", 1800);
        Beklenen(Hesapla(beyrut, "2027-03-27T22:00Z", "Asia/Beirut"), true, "2027-03-28", "2027-03-28 02:00", 3600);
    }

    [Fact(DisplayName = "0027/AC-99: açılış arama sınırı max(bugün, son through) + 7")]
    public void AramaSiniri()
    {
        Dictionary<string, string[]> pazartesi = new() { ["monday"] = ["09:00-10:00"] };
        Beklenen(Hesapla(pazartesi, "2026-12-07T08:00Z"), false, "2026-12-07", "2026-12-14 09:00");
        Beklenen(Hesapla(pazartesi, "2026-12-07T08:00Z", ozel: Ozel(("2026-12-14", "2026-12-20", []))), false, "2026-12-07", "2026-12-21 09:00");
    }
}
