using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0025` **D9** · **D10**(a) — şube öğesinin `openingHoursSpecification`'ı ve bayt bütçesi, sunucu
/// yanıtı JavaScript çalıştırılmadan ayrıştırılarak (`0004` C13). Kaydetmeden sonraki ilk belge
/// (AC-58), geçici kapalı şube (AC-57) ve saat dilimi (AC-26) gerçek DB'yle `CalismaSaatleriZinciriTests`'te.
/// </summary>
public sealed class CalismaSaatleriBelgeTests(HostFactory factory, Xunit.Abstractions.ITestOutputHelper olcum)
    : IClassFixture<HostFactory>, IDisposable
{
    private static readonly string[] Gunler = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    private readonly SeoOrtami ortam = new();

    public void Dispose() => ortam.Dispose();

    /// <summary>D8 biçiminde program: verilmeyen gün kapalı (boş dizi), D6 sırasıyla yedi anahtar.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>> Program(
        params (string Gun, (string Baslangic, string Bitis)[] Araliklar)[] gunler) =>
        Gunler.ToDictionary(
            gun => gun,
            gun => (IReadOnlyList<PublicOpeningIntervalDto>)[.. (gunler.FirstOrDefault(item => item.Gun == gun).Araliklar ?? [])
                .Select(aralik => new PublicOpeningIntervalDto(aralik.Baslangic, aralik.Bitis))],
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>> Programli() => Program(
        ("monday", [("09:00", "18:00")]),
        ("tuesday", [("09:00", "18:00")]),
        ("wednesday", [("09:00", "18:00")]),
        ("thursday", [("09:00", "18:00")]),
        ("friday", [("09:00", "18:00")]),
        ("saturday", [("10:00", "14:00")]));

    private void Subeler(params PublicBranchDto[] subeler) => ortam.Galeri = ortam.Galeri! with { Branches = subeler };

    private async Task<JsonElement> DealerAsync() =>
        Assert.Single((await SeoOrtami.GetAsync(ortam.Istemci(factory), "/tr/")).Belge.JsonLd());

    private static List<(string Gunler, string Acilis, string Kapanis)> Ogeler(JsonElement department) =>
        [.. department.GetProperty("openingHoursSpecification").EnumerateArray().Select(oge => (
            oge.GetProperty("dayOfWeek").GetRawText(),
            oge.GetProperty("opens").GetString()!,
            oge.GetProperty("closes").GetString()!))];

    [Fact(DisplayName = "0025/AC-52: Programlı şubenin dizisi tam olarak üç öğe: Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar 00:00–00:00.")]
    public async Task Programli_UcOge()
    {
        Subeler(new PublicBranchDto("Kadıköy", "Bağdat Cad. 1", "+902160000001", Programli()));

        Assert.Equal(
            [
                ("""["Monday","Tuesday","Wednesday","Thursday","Friday"]""", "09:00", "18:00"),
                ("""["Saturday"]""", "10:00", "14:00"),
                ("""["Sunday"]""", "00:00", "00:00"),
            ],
            Ogeler((await DealerAsync()).GetProperty("department")[0]));
    }

    [Fact(DisplayName = "0025/AC-53: Pzt 09:00–13:00 ve 16:00–21:00, Salı 09:00–13:00, diğerleri kapalı: [Pzt, Salı] 09–13; [Pzt] 16–21; [Çar…Paz] 00:00–00:00.")]
    public async Task Gruplama_IlkGorunduguGun()
    {
        Subeler(new PublicBranchDto("Kadıköy", "Bağdat Cad. 1", "+902160000001", Program(("monday", [("09:00", "13:00"), ("16:00", "21:00")]), ("tuesday", [("09:00", "13:00")]))));

        Assert.Equal(
            [
                ("""["Monday","Tuesday"]""", "09:00", "13:00"),
                ("""["Monday"]""", "16:00", "21:00"),
                ("""["Wednesday","Thursday","Friday","Saturday","Sunday"]""", "00:00", "00:00"),
            ],
            Ogeler((await DealerAsync()).GetProperty("department")[0]));
    }

    [Fact(DisplayName = "0025/AC-53 (D9): Kapalı grup da ilk göründüğü günün yerindedir — Pazartesi kapalıysa ilk öğe.")]
    public async Task Gruplama_KapaliIlkGun()
    {
        Subeler(new PublicBranchDto("Kadıköy", "Bağdat Cad. 1", "+902160000001", Program(("tuesday", [("09:00", "18:00")]))));

        Assert.Equal(
            ("""["Monday","Wednesday","Thursday","Friday","Saturday","Sunday"]""", "00:00", "00:00"),
            Ogeler((await DealerAsync()).GetProperty("department")[0])[0]);
    }

    [Fact(DisplayName = "0025/AC-54: Her öğenin anahtar kümesi tam olarak `@type`, `dayOfWeek`, `opens`, `closes` ve `@type` `OpeningHoursSpecification`.")]
    public async Task OgeAnahtarlari()
    {
        Subeler(new PublicBranchDto("Kadıköy", "Bağdat Cad. 1", "+902160000001", Programli()));

        foreach (JsonElement oge in (await DealerAsync()).GetProperty("department")[0].GetProperty("openingHoursSpecification").EnumerateArray())
        {
            Assert.Equal(["@type", "closes", "dayOfWeek", "opens"], oge.Anahtarlar().Order(StringComparer.Ordinal));
            Assert.Equal("OpeningHoursSpecification", oge.GetProperty("@type").GetString());
        }
    }

    [Fact(DisplayName = "0025/AC-56, AC-59: Programsız şubenin öğesinde anahtar yok; kök `AutoDealer` hiçbir kurulumda taşımaz; programlı şubeler varken tam bir JSON-LD bloğu.")]
    public async Task Programsiz_VeKok()
    {
        Subeler(
            new PublicBranchDto("Kadıköy", "Bağdat Cad. 1", "+902160000001", Programli()),
            new PublicBranchDto("Ataşehir", "Barbaros 7", "+902160000002"));

        IDocument belge = (await SeoOrtami.GetAsync(ortam.Istemci(factory), "/tr/")).Belge;
        JsonElement dealer = Assert.Single(belge.JsonLd());
        Assert.Single(belge.QuerySelectorAll("script[type='application/ld+json']"));

        Assert.DoesNotContain("openingHoursSpecification", dealer.Anahtarlar());
        Assert.Contains("openingHoursSpecification", dealer.GetProperty("department")[0].Anahtarlar());
        Assert.DoesNotContain("openingHoursSpecification", dealer.GetProperty("department")[1].Anahtarlar());
    }

    /// <summary>
    /// D10(a) — tek şube; yedi günde üçer aralık ve 21 (opens, closes) çiftinin hepsi farklı: hiçbir
    /// öğe birleşmez. Fark aynı şubenin programsız `/ar/` belgesine göre, sıkıştırmasız UTF-8.
    /// </summary>
    [Fact(DisplayName = "0025/AC-60: Tek şubeli kurulumda en kötü programlı şubenin `/ar/` belgesi programsız hâlinden en çok 2304 B büyüktür (21 farklı çift).")]
    public async Task Sigar_2304Bayta_EnKotuDurumda()
    {
        IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>> enKotu = EnKotuProgram();
        Assert.Equal(21, enKotu.Values.SelectMany(araliklar => araliklar).Select(aralik => (aralik.Start, aralik.End)).Distinct().Count());

        int programsiz = Encoding.UTF8.GetByteCount((await EvAsync(null)).Tags);
        int programli = Encoding.UTF8.GetByteCount((await EvAsync(enKotu)).Tags);

        olcum.WriteLine($"Ölçülen fark: {programli - programsiz} B (bütçe 2304 B)");
        Assert.InRange(programli - programsiz, 1_800, 2_304);
    }

    /// <summary>AC-60'ın kurgusu: gün başına üç aralık, her günün dakikası farklı (21 farklı çift).</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>> EnKotuProgram() =>
        Gunler.Select((gun, i) => (gun, i)).ToDictionary(
            pair => pair.gun,
            pair => (IReadOnlyList<PublicOpeningIntervalDto>)
            [
                new($"08:{pair.i:D2}", $"10:{pair.i:D2}"),
                new($"12:{pair.i:D2}", $"14:{pair.i:D2}"),
                new($"16:{pair.i:D2}", $"21:{pair.i:D2}"),
            ],
            StringComparer.Ordinal);

    private static async Task<SeoHead> EvAsync(IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>>? program)
    {
        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        SeoHeadBuilder seo = new(
            SiteRoot.Parse("https://www.uzun-bir-galeri-alanadi.com.tr"),
            SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled),
            diller,
            NullLogger<SeoHeadBuilder>.Instance);

        PublicGalleryDto galeri = new(
            new string('ع', 120),
            [new(new string('ف', 120), new string('ش', 300), "+96550000001", program)],
            []);

        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton<IPublicGalleryQueries>(new Sabit(galeri)).BuildServiceProvider(),
        };
        context.Request.Path = "/ar/";

        return await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create("ar").Value, galeri.DisplayName, null);
    }

    private sealed class Sabit(PublicGalleryDto galeri) : IPublicGalleryQueries
    {
        public Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<PublicGalleryDto?>(galeri);
    }
}
