using System.Diagnostics;
using System.Globalization;

// C3 ölçüm protokolü — HARFİYEN uygulanır, gevşetilmez:
//   500 ilanlık sabit veri seti, 20 istek ısınma + 200 istek ölçüm, eşzamanlılık 1,
//   p95 bu 200 istek üzerinden. Toplam sayı sorgusu (R6) ölçüme DÂHİLDİR.
// Eşik: p95 < 400 ms (ağ hariç — bu yüzden istek yerel Host'a gider).
//
// `0015` D13 — eşik ÜÇ profilde AYRI AYRI tutulur: filtresiz · R1'in bütün eksenleri dolu ·
// `sort=price-asc`. Profil yazılmazsa en ucuz sorgu ölçülür ve kriter boşa yeşil yanar.
// `0001`'in kendi profili (iki kasa tipi) regresyon olarak ölçülmeye devam eder.

const int WarmupRequests = 20;
const int MeasuredRequests = 200;
const int ThresholdMilliseconds = 400;

string baseUrl = Environment.GetEnvironmentVariable("PERF_BASE_URL") ?? "http://localhost:5080";

// `0019` P11 — talep yazma profili yalnız açıkça istendiğinde koşar; C3 profilleri koşmaz.
if (Environment.GetEnvironmentVariable("PERF_PROFILE") == "lead-capture")
{
    return await LeadCaptureProfile.RunAsync(baseUrl);
}

Profile[] profiles =
[
    new("0001", "iki kasa tipiyle daraltılmış", "/api/catalog/showcase-listings?bodyType=suv&bodyType=sedan&locale=tr", RequireResults: true),
    new("0015", "filtresiz (AC-43)", "/api/catalog/showcase-listings?locale=tr", RequireResults: true),
    new(
        "0015",
        "R1'in bütün eksenleri dolu (AC-44)",
        "/api/catalog/showcase-listings?bodyType=suv&bodyType=sedan&fuel=diesel&fuel=petrol&transmission=automatic&transmission=manual"
            + "&yearMin=2015&yearMax=2025&mileageMax=250000&priceMin=100000&priceMax=5000000&locale=tr",
        // Boş sonuç dönen bir "bütün eksenler" isteği hiçbir satırı sıralamaz ve sayfalamaz;
        // ölçüm en ucuz yolu ölçer. Sonuç sıfırsa profil GEÇERSİZ sayılır (D13).
        RequireResults: true),
    new("0015", "sort=price-asc (AC-45)", "/api/catalog/showcase-listings?sort=price-asc&locale=tr", RequireResults: true),

    // `0021` C5 · D24 — marka ekseni: iki marka (sözleşmeden model kimliği kümesine çevrilir)
    // ve kasa tipiyle birlikte. Tohum dokuz markaya yayıldığı için iki marka gerçek bir alt küme.
    new("0021", "iki marka (C5)", "/api/catalog/showcase-listings?brand=toyota&brand=honda&locale=tr", RequireResults: true),
    new("0021", "marka + kasa tipi (C5)", "/api/catalog/showcase-listings?brand=toyota&brand=honda&bodyType=suv&bodyType=sedan&locale=tr", RequireResults: true),
];

using HttpClient client = new() { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

bool allMet = true;
List<(Profile Profile, Measurement Result)> results = [];

foreach (Profile profile in profiles)
{
    Console.WriteLine($"Profil: {profile.Name} — {baseUrl}{profile.Path}");

    int totalCount = await ReadTotalCountAsync(client, profile.Path);
    Console.WriteLine($"  sonuç kümesi: {totalCount} ilan");

    if (profile.RequireResults && totalCount == 0)
    {
        Console.Error.WriteLine("  KIRMIZI — profil boş sonuç döndü; ölçüm hiçbir satırı sıralamıyor (D13).");
        allMet = false;
        continue;
    }

    for (int index = 0; index < WarmupRequests; index++)
    {
        await SendAsync(client, profile.Path);
    }

    double[] durations = new double[MeasuredRequests];

    // Eşzamanlılık 1: istekler sırayla gönderilir.
    for (int index = 0; index < MeasuredRequests; index++)
    {
        long start = Stopwatch.GetTimestamp();
        await SendAsync(client, profile.Path);
        durations[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    Array.Sort(durations);
    Measurement result = new(Percentile(durations, 50), Percentile(durations, 95), Percentile(durations, 99), totalCount);
    results.Add((profile, result));

    Console.WriteLine(FormattableString.Invariant($"  p50: {result.P50:F1} ms"));
    Console.WriteLine(FormattableString.Invariant($"  p95: {result.P95:F1} ms  (eşik: {ThresholdMilliseconds} ms)"));
    Console.WriteLine(FormattableString.Invariant($"  p99: {result.P99:F1} ms"));

    if (result.P95 >= ThresholdMilliseconds)
    {
        Console.Error.WriteLine(FormattableString.Invariant($"  KIRMIZI — p95 {result.P95:F1} ms, eşik {ThresholdMilliseconds} ms."));
        allMet = false;
    }
}

// Ölçüm yalnız iş log'unda kalırsa kanıt koşuyla birlikte kaybolur; rapora yazılır.
WriteReport("0001", results.Where(item => item.Profile.Spec == "0001").ToList());
WriteReport("0015", results.Where(item => item.Profile.Spec == "0015").ToList());
WriteReport("0021", results.Where(item => item.Profile.Spec == "0021").ToList());

if (!allMet)
{
    Console.Error.WriteLine("KIRMIZI — C3 sunucu eşiği en az bir profilde karşılanmadı. AP-12: önce ölç, TEK optimizasyon, aynı yöntemle yeniden ölç.");
    return 1;
}

Console.WriteLine("YEŞİL — C3 sunucu eşiği bütün profillerde karşılandı.");
return 0;

static void WriteReport(string spec, List<(Profile Profile, Measurement Result)> results)
{
    if (results.Count == 0)
    {
        return;
    }

    string reportPath = Path.Combine(
        Environment.GetEnvironmentVariable("PERF_REPORT_DIR") ?? Path.Combine("docs", "evidence"),
        $"{spec}-performance.json");

    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

    // Uç ölçümü aynı dosyanın "client" bölümünü yazar; birbirinin üstüne yazmazlar.
    Dictionary<string, System.Text.Json.JsonElement> report = File.Exists(reportPath)
        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(
              File.ReadAllText(reportPath)) ?? []
        : [];

    object Server((Profile Profile, Measurement Result) item) => new
    {
        profile = item.Profile.Name,
        path = item.Profile.Path,
        protocol = "500 ilan, 20 ısınma + 200 ölçüm, eşzamanlılık 1 (spec 0001 C3)",
        filteredCount = item.Result.TotalCount,
        p50Milliseconds = Math.Round(item.Result.P50, 1),
        p95Milliseconds = Math.Round(item.Result.P95, 1),
        p99Milliseconds = Math.Round(item.Result.P99, 1),
        thresholdMilliseconds = ThresholdMilliseconds,
        met = item.Result.P95 < ThresholdMilliseconds,
    };

    report["spec"] = System.Text.Json.JsonSerializer.SerializeToElement(spec);

    // `0001`'in raporunun şekli DEĞİŞMEZ (tek profil, "server" nesnesi); `0015` profilleri dizi.
    report["server"] = spec == "0001"
        ? System.Text.Json.JsonSerializer.SerializeToElement(Server(results[0]))
        : System.Text.Json.JsonSerializer.SerializeToElement(results.Select(Server).ToList());

    File.WriteAllText(
        reportPath,
        System.Text.Json.JsonSerializer.Serialize(
            report,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

    Console.WriteLine($"Rapor: {reportPath}");
}

static async Task SendAsync(HttpClient client, string path)
{
    using HttpResponseMessage response = await client.GetAsync(path);
    response.EnsureSuccessStatusCode();

    // Gövde tamamen okunur; aksi hâlde ölçüm yanıtın tamamını değil başlığını ölçerdi.
    _ = await response.Content.ReadAsByteArrayAsync();
}

static async Task<int> ReadTotalCountAsync(HttpClient client, string path)
{
    using HttpResponseMessage response = await client.GetAsync(path);
    response.EnsureSuccessStatusCode();

    using System.Text.Json.JsonDocument document =
        System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    return document.RootElement.GetProperty("totalCount").GetInt32();
}

static double Percentile(double[] sorted, double percentile)
{
    double rank = percentile / 100d * (sorted.Length - 1);
    int lower = (int)Math.Floor(rank);
    int upper = (int)Math.Ceiling(rank);

    return lower == upper
        ? sorted[lower]
        : sorted[lower] + ((rank - lower) * (sorted[upper] - sorted[lower]));
}

internal sealed record Profile(string Spec, string Name, string Path, bool RequireResults);

internal sealed record Measurement(double P50, double P95, double P99, int TotalCount);
