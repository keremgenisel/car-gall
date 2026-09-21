using System.Diagnostics;
using System.Globalization;

// C3 ölçüm protokolü — HARFİYEN uygulanır, gevşetilmez:
//   500 ilanlık sabit veri seti, iki kasa tipiyle daraltılmış istek,
//   20 istek ısınma + 200 istek ölçüm, eşzamanlılık 1,
//   p95 bu 200 istek üzerinden. Toplam sayı sorgusu (R6) ölçüme DÂHİLDİR.
// Eşik: p95 < 400 ms (ağ hariç — bu yüzden istek yerel Host'a gider).

const int WarmupRequests = 20;
const int MeasuredRequests = 200;
const int ThresholdMilliseconds = 400;

string baseUrl = Environment.GetEnvironmentVariable("PERF_BASE_URL") ?? "http://localhost:5080";
string path = "/api/catalog/showcase-listings?bodyType=suv&bodyType=sedan&locale=tr";

using HttpClient client = new() { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine($"Hedef: {baseUrl}{path}");

int totalCount = await ReadTotalCountAsync(client, path);
Console.WriteLine($"Daraltılmış küme: {totalCount} ilan");

for (int index = 0; index < WarmupRequests; index++)
{
    await SendAsync(client, path);
}

double[] durations = new double[MeasuredRequests];

// Eşzamanlılık 1: istekler sırayla gönderilir.
for (int index = 0; index < MeasuredRequests; index++)
{
    long start = Stopwatch.GetTimestamp();
    await SendAsync(client, path);
    durations[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
}

Array.Sort(durations);

double p50 = Percentile(durations, 50);
double p95 = Percentile(durations, 95);
double p99 = Percentile(durations, 99);

Console.WriteLine(FormattableString.Invariant($"p50: {p50:F1} ms"));
Console.WriteLine(FormattableString.Invariant($"p95: {p95:F1} ms  (eşik: {ThresholdMilliseconds} ms)"));
Console.WriteLine(FormattableString.Invariant($"p99: {p99:F1} ms"));

if (p95 >= ThresholdMilliseconds)
{
    Console.Error.WriteLine(FormattableString.Invariant(
        $"KIRMIZI — p95 {p95:F1} ms, eşik {ThresholdMilliseconds} ms. AP-12: önce ölç, TEK optimizasyon, aynı yöntemle yeniden ölç."));
    return 1;
}

Console.WriteLine("YEŞİL — C3 sunucu eşiği karşılandı.");
return 0;

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
