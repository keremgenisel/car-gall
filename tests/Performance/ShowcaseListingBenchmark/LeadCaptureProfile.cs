using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// `0019` D21 / AC-52 — <c>POST /api/leads</c> profili. YALNIZ <c>PERF_PROFILE=lead-capture</c>
/// verildiğinde koşar (P11): C3 iş akışı bu profili görmez ve ölçüm elle yapılır.
///
/// <para>Protokol: ısınma 20 + ölçüm 200, eşzamanlılık 1, ağ hariç (yerel Host). Her istek
/// AYRI bir tekrar anahtarı taşır (aynı anahtar ilk talebi döndürür ve yazma yolunu ölçmez)
/// ve vitrindeki SABİT bir ilana gider: ilk rezerve olmayan ilan bir kez seçilir. Hız
/// sınırları Host'ta 220'nin üstüne çekilmiş olmalıdır; 220 isteğin hepsi <c>201</c> değilse
/// koşu GEÇERSİZDİR ve çıkış kodu 1'dir.</para>
///
/// <para>Aynı ikili iki Host'a (<c>PERF_BASE_URL</c>) sırayla koşulur; karşılaştırma (p95
/// medyanları, fark ≤ 10 ms) koşuların dışında yapılır — tek bir koşu "fark" ölçemez.</para>
/// </summary>
internal static class LeadCaptureProfile
{
    private const int WarmupRequests = 20;
    private const int MeasuredRequests = 200;

    internal static async Task<int> RunAsync(string baseUrl)
    {
        string locale = Environment.GetEnvironmentVariable("PERF_LEAD_LOCALE") ?? "tr";
        string label = Environment.GetEnvironmentVariable("PERF_LABEL") ?? baseUrl;
        string run = Guid.CreateVersion7().ToString("N");

        using HttpClient client = new() { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

        string slug = await FirstAvailableListingAsync(client, locale);
        Console.WriteLine($"Profil: POST /api/leads (0019 D21) — {label} — ilan '{slug}', dil '{locale}'");

        Dictionary<HttpStatusCode, int> statuses = [];
        double[] durations = new double[MeasuredRequests];

        for (int index = 0; index < WarmupRequests + MeasuredRequests; index++)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/leads")
            {
                Content = JsonContent.Create(new
                {
                    listingSlug = slug,
                    leadType = "information_request",
                    locale,
                    fullName = "Ölçüm Talebi",
                    phone = "+905321112233",
                    consentGiven = true,
                }),
            };
            request.Headers.Add("Idempotency-Key", $"perf-0019-{run}-{index}");

            long start = Stopwatch.GetTimestamp();
            using HttpResponseMessage response = await client.SendAsync(request);
            _ = await response.Content.ReadAsByteArrayAsync();
            double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            statuses[response.StatusCode] = statuses.GetValueOrDefault(response.StatusCode) + 1;

            if (index >= WarmupRequests)
            {
                durations[index - WarmupRequests] = elapsed;
            }
        }

        Array.Sort(durations);
        int created = statuses.GetValueOrDefault(HttpStatusCode.Created);

        Console.WriteLine(FormattableString.Invariant($"  p50: {Percentile(durations, 50):F2} ms"));
        Console.WriteLine(FormattableString.Invariant($"  p95: {Percentile(durations, 95):F2} ms"));
        Console.WriteLine(FormattableString.Invariant($"  p99: {Percentile(durations, 99):F2} ms"));
        Console.WriteLine($"  201: {created} / {WarmupRequests + MeasuredRequests}");
        Console.WriteLine("  durumlar: " + string.Join(", ", statuses.OrderBy(pair => pair.Key).Select(pair => string.Create(CultureInfo.InvariantCulture, $"{(int)pair.Key}×{pair.Value}"))));

        if (created != WarmupRequests + MeasuredRequests)
        {
            Console.Error.WriteLine("  GEÇERSİZ — her istek 201 olmalı (D21); hız sınırlarını ve ilanı denetle.");
            return 1;
        }

        return 0;
    }

    private static async Task<string> FirstAvailableListingAsync(HttpClient client, string locale)
    {
        using HttpResponseMessage response = await client.GetAsync($"/api/catalog/showcase-listings?locale={locale}");
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("items").EnumerateArray()
            .First(item => !item.GetProperty("isReserved").GetBoolean())
            .GetProperty("slug").GetString()!;
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        double rank = percentile / 100d * (sorted.Length - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);

        return lower == upper
            ? sorted[lower]
            : sorted[lower] + ((rank - lower) * (sorted[upper] - sorted[lower]));
    }
}
