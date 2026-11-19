using System.Buffers.Text;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0018` — bülten uçları GERÇEK boru hattında: hız sınırı (`UseRateLimiter`), hata sözleşmesi,
/// cevabın ayırt edilemezliği, sayfaların `noindex` ve `Referrer-Policy`'si. Modülün kendi
/// testi kuralları sınar; burası onların KABLOLANMIŞ olduğunu (`0003`'ün dersi).
///
/// <para>Her test kendi veritabanını ve uygulamasını kurar: geçerli istek bütçesi süreç
/// ömrü boyunca yaşar ve testler aynı kaynaktan geldiği için birbirini zehirlerdi.</para>
/// </summary>
public sealed class BultenZinciriTests : IAsyncLifetime
{
    private const string Abonelik = "/api/notifications/newsletter/subscriptions";
    private const string Teyit = "/api/notifications/newsletter/confirmations";
    private const string Cikis = "/api/notifications/newsletter/unsubscriptions";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private TcpListener? askidaSmtp;

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        // AC-24 — bağlantıyı kabul eden ama HİÇ cevap vermeyen bir SMTP sunucusu: MailKit
        // karşılama satırını bekler ve gönderim zaman aşımına kadar askıda kalır.
        askidaSmtp = new TcpListener(IPAddress.Loopback, 0);
        askidaSmtp.Start();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);
            builder.UseSetting("Notifications:Smtp:Host", "127.0.0.1");
            builder.UseSetting("Notifications:Smtp:Port", ((IPEndPoint)askidaSmtp.LocalEndpoint).Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("Notifications:SendTimeoutSeconds", "60");
        });
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        askidaSmtp?.Stop();
        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0018/AC-16: Hiç kaydı olmayan, teyit bekleyen, etkin ve çıkmış dört adres için abonelik isteğinin cevabı aynı durum kodunu ve bayt bayt aynı gövdeyi taşır (süre ölçülmez — X16)")]
    public async Task AyniCevap_DortDurumda()
    {
        HttpClient client = factory!.CreateClient();
        await AbonelikYazAsync("bekleyen@ornek.com", 1);
        await AbonelikYazAsync("etkin@ornek.com", 2);
        await AbonelikYazAsync("cikmis@ornek.com", 3);

        List<(HttpStatusCode Kod, byte[] Govde)> cevaplar = [];

        foreach (string adres in new[] { "yeni@ornek.com", "bekleyen@ornek.com", "etkin@ornek.com", "cikmis@ornek.com" })
        {
            HttpResponseMessage cevap = await client.PostAsJsonAsync(Abonelik, new { email = adres, locale = "tr" });
            cevaplar.Add((cevap.StatusCode, await cevap.Content.ReadAsByteArrayAsync()));
        }

        Assert.All(cevaplar, cevap => Assert.Equal(HttpStatusCode.Accepted, cevap.Kod));
        Assert.All(cevaplar, cevap => Assert.Equal(cevaplar[0].Govde, cevap.Govde));
        Assert.DoesNotContain("ornek.com", Encoding.UTF8.GetString(cevaplar[0].Govde), StringComparison.Ordinal);
    }

    [Theory(DisplayName = "0018/AC-7: `ali@` ve boş adres alan hatasıyla reddedilir; API `400` ve `notification.subscription_email_invalid` döner; kayıt oluşmaz ve ileti kuyruğa girmez")]
    [InlineData("ali@")]
    [InlineData("")]
    public async Task Doner400_GecersizAdres(string adres)
    {
        HttpResponseMessage cevap = await factory!.CreateClient().PostAsJsonAsync(Abonelik, new { email = adres, locale = "tr" });
        JsonElement govde = await GovdeAsync(cevap);

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("notification.subscription_email_invalid", govde.GetProperty("errorCode").GetString());
        Assert.Equal("email", govde.GetProperty("field").GetString());
        Assert.Equal(0, await SayAsync("newsletter_subscriptions"));
        Assert.Equal(0, await SayAsync("messages"));
    }

    [Fact(DisplayName = "0018/AC-10: Kurulumda etkin olmayan ya da tanınmayan bir dille gelen istek `400` ve `notification.subscription_invalid_locale` alır; kayıt oluşmaz (D14)")]
    public async Task Doner400_EtkinOlmayanDil()
    {
        HttpResponseMessage cevap = await factory!.CreateClient().PostAsJsonAsync(Abonelik, new { email = "ali@ornek.com", locale = "de" });

        Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        Assert.Equal("notification.subscription_invalid_locale", (await GovdeAsync(cevap)).GetProperty("errorCode").GetString());
        Assert.Equal(0, await SayAsync("newsletter_subscriptions"));
    }

    [Fact(DisplayName = "0018/AC-92: Abonelik, teyit ve çıkış uçlarının hiçbir hata yanıtı e-posta adresini içermez")]
    public async Task Icermez_Adres_HataYanitlari()
    {
        HttpClient client = factory!.CreateClient();
        List<string> govdeler = [];

        foreach (object istek in new object[]
        {
            new { email = "Ali Veli <ali.veli@ornek.com>", locale = "tr" },
            new { email = "ali.veli@ornek.com", locale = "de" },
            new { email = "ali.veli@ornek", locale = "tr" },
        })
        {
            govdeler.Add(await (await client.PostAsJsonAsync(Abonelik, istek)).Content.ReadAsStringAsync());
        }

        govdeler.Add(await (await client.PostAsJsonAsync(Teyit, new { token = "ali.veli@ornek.com" })).Content.ReadAsStringAsync());
        govdeler.Add(await (await client.PostAsJsonAsync(Cikis, new { token = "ali.veli@ornek.com" })).Content.ReadAsStringAsync());

        Assert.Equal(5, govdeler.Count);
        Assert.All(govdeler, govde => Assert.DoesNotContain("ali.veli", govde, StringComparison.OrdinalIgnoreCase));
        Assert.All(govdeler, govde => Assert.Contains("errorCode", govde, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0018/AC-12: Aynı IP'den yapılandırılan pencerede geçerli istek eşiği kadar istekten sonraki geçerli istek `429` ve `notification.subscription_rate_limited` alır; kayıt oluşmaz, ileti kuyruğa girmez ve form hız sınırı mesajı gösterir (D9)")]
    public async Task Doner429_GecerliIstekEsigindenSonra()
    {
        HttpClient client = factory!.CreateClient();

        // Eşik `appsettings.json`'daki değer (5) — AC-98: test yapılandırılanı kullanır.
        for (int sira = 1; sira <= 5; sira++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync(Abonelik, new { email = $"kisi{sira}@ornek.com", locale = "tr" })).StatusCode);
        }

        HttpResponseMessage altinci = await client.PostAsJsonAsync(Abonelik, new { email = "fazla@ornek.com", locale = "tr" });

        Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);
        Assert.Equal("notification.subscription_rate_limited", (await GovdeAsync(altinci)).GetProperty("errorCode").GetString());
        Assert.Equal(5, await SayAsync("newsletter_subscriptions"));
        Assert.Equal(5, await SayAsync("messages"));
    }

    [Fact(DisplayName = "0018/AC-14: Aynı IP'den yapılandırılan pencerede kaba istek eşiği kadar istekten (geçersizler dâhil) sonraki istek `429` alır (D9)")]
    public async Task Doner429_KabaIstekEsigindenSonra()
    {
        HttpClient client = factory!.CreateClient();

        // Eşik `appsettings.json`'daki değer (30); hepsi GEÇERSİZ — geçerli bütçeye dokunmaz.
        for (int sira = 1; sira <= 30; sira++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Abonelik, new { email = "ali@", locale = "tr" })).StatusCode);
        }

        HttpResponseMessage fazla = await client.PostAsJsonAsync(Abonelik, new { email = "ali@ornek.com", locale = "tr" });

        Assert.Equal(HttpStatusCode.TooManyRequests, fazla.StatusCode);
        Assert.Equal("notification.subscription_rate_limited", (await GovdeAsync(fazla)).GetProperty("errorCode").GetString());
        Assert.Equal(0, await SayAsync("newsletter_subscriptions"));
    }

    [Fact(DisplayName = "0018/AC-24: Taşıyıcı yanıt vermezken (gönderim askıda) abonelik isteğinin cevabı yine döner; cevap gönderimi beklemez")]
    public async Task Doner_TasiyiciAskidayken()
    {
        HttpClient client = factory!.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync(Abonelik, new { email = "ilk@ornek.com", locale = "tr" })).StatusCode);

        // Kuyruk servisi teyit iletisini almış ve askıdaki sunucuya bağlanmış olmalı: bundan
        // sonra taşıyıcı ASKIDADIR (zaman aşımı 60 sn).
        using CancellationTokenSource bekleme = new(TimeSpan.FromSeconds(20));
        using TcpClient askidaki = await askidaSmtp!.AcceptTcpClientAsync(bekleme.Token);

        HttpResponseMessage ikinci = await client.PostAsJsonAsync(Abonelik, new { email = "ikinci@ornek.com", locale = "tr" });

        Assert.Equal(HttpStatusCode.Accepted, ikinci.StatusCode);
        Assert.Equal(2, await SayAsync("newsletter_subscriptions"));
        Assert.Equal(0, await SayAsync("messages WHERE status = 2"));
    }

    [Fact(DisplayName = "0018/AC-35: Hiç var olmamış ya da bozuk bir teyit bağlantısının cevabı, süresi dolmuş ve kullanılmış bağlantınınkiyle aynı durum kodu ve aynı gövdedir")]
    public async Task AyniCevap_GecersizTeyitBaglantilari()
    {
        HttpClient client = factory!.CreateClient();
        string suresiDolmus = await AbonelikYazAsync("dolmus@ornek.com", 1, anahtarSuresi: TimeSpan.FromHours(-1));
        string gecerli = await AbonelikYazAsync("gecerli@ornek.com", 1, anahtarSuresi: TimeSpan.FromHours(1));

        HttpResponseMessage ilkKullanim = await client.PostAsJsonAsync(Teyit, new { token = gecerli });
        Assert.Equal(HttpStatusCode.OK, ilkKullanim.StatusCode);

        List<(HttpStatusCode Kod, byte[] Govde)> cevaplar = [];

        foreach (string anahtar in new[] { Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)), "bozuk!!", suresiDolmus, gecerli })
        {
            HttpResponseMessage cevap = await client.PostAsJsonAsync(Teyit, new { token = anahtar });
            cevaplar.Add((cevap.StatusCode, await cevap.Content.ReadAsByteArrayAsync()));
        }

        Assert.All(cevaplar, cevap => Assert.Equal(HttpStatusCode.NotFound, cevap.Kod));
        Assert.All(cevaplar, cevap => Assert.Equal(cevaplar[0].Govde, cevap.Govde));
        Assert.Equal("notification.subscription_link_invalid", JsonDocument.Parse(cevaplar[0].Govde).RootElement.GetProperty("errorCode").GetString());
    }

    /// <summary>
    /// İstemci ÇEREZSİZDİR ve kimlik başlığı taşımaz: çıkış oturum ya da onay istemez (**D6**).
    /// Bağlantı, Host'a verilen test sırrıyla `UnsubscribeTokens`'ın sözleşmesine göre kurulur
    /// (abonelik kimliği + HMAC-SHA256) — bültendeki bağlantının biçimi `Notifications.Tests`'te.
    /// </summary>
    [Fact(DisplayName = "0018/AC-38: Çıkış bağlantısı oturum açılmamış bir tarayıcıda açıldığında başka bir düğme ya da onay istemeden kaydı **çıkmış** yapar, çıkış zamanını UTC olarak yazar, bir **çıkış** izin olayı ekler ve sayfa \"abonelikten çıktınız\" mesajını gösterir")]
    public async Task CikmisYapar_OturumsuzTekIstekte()
    {
        HttpClient client = factory!.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await AbonelikYazAsync("ali@ornek.com", 2);
        Guid id = await AbonelikIdAsync("ali@ornek.com");

        HttpResponseMessage cevap = await client.PostAsJsonAsync(Cikis, new { token = CikisAnahtari(id) });

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal(1, await SayAsync("newsletter_subscriptions WHERE status = 3 AND unsubscribed_at IS NOT NULL"));
        Assert.Equal(1, await SayAsync("consent_events WHERE kind = 3"));
    }

    [Fact(DisplayName = "0018/AC-44: Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa \"bağlantı artık geçerli değil\" mesajını gösterir")]
    public async Task Doner404_GecersizCikis()
    {
        HttpClient client = factory!.CreateClient();
        await AbonelikYazAsync("ali@ornek.com", 2);

        HttpResponseMessage cevap = await client.PostAsJsonAsync(Cikis, new { token = CikisAnahtari(Guid.CreateVersion7()) });

        Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        Assert.Equal("notification.subscription_link_invalid", (await GovdeAsync(cevap)).GetProperty("errorCode").GetString());
        Assert.Equal(1, await SayAsync("newsletter_subscriptions WHERE status = 2"));
        Assert.Equal(0, await SayAsync("consent_events"));
    }

    [Theory(DisplayName = "0018/AC-53: Teyit ve çıkış sayfaları `noindex` taşır")]
    [InlineData("/tr/newsletter/confirm?token=abc")]
    [InlineData("/ar/newsletter/unsubscribe?token=abc")]
    [InlineData("/en/newsletter/confirm")]
    public async Task Tasir_Noindex(string adres)
    {
        string html = await factory!.CreateClient().GetStringAsync(adres);

        Assert.Contains("<meta name=\"robots\" content=\"noindex\" />", html, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "0018/AC-54: Teyit ve çıkış sayfalarının yanıtı `Referrer-Policy: no-referrer` başlığını taşır")]
    [InlineData("/tr/newsletter/confirm?token=abc")]
    [InlineData("/ar/newsletter/unsubscribe?token=abc")]
    public async Task Tasir_ReferrerPolicy(string adres)
    {
        HttpResponseMessage cevap = await factory!.CreateClient().GetAsync(adres);

        Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        Assert.Equal("no-referrer", Assert.Single(cevap.Headers.GetValues("Referrer-Policy")));
    }

    /// <summary>Kapsam: yalnız iki sayfa. Ana sayfa ve ilan detayı indekslenmeye devam eder.</summary>
    [Fact(DisplayName = "0018/AC-53: Teyit ve çıkış sayfaları `noindex` taşır")]
    public async Task Tasimaz_Noindex_AnaSayfa()
    {
        HttpResponseMessage cevap = await factory!.CreateClient().GetAsync("/tr/");

        Assert.DoesNotContain("noindex", await cevap.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.False(cevap.Headers.Contains("Referrer-Policy"));
    }

    /// <summary>`UnsubscribeTokens`'ın sözleşmesi: Base64Url(kimlik ‖ HMAC-SHA256(sır, amaç ‖ kimlik)).</summary>
    private static string CikisAnahtari(Guid id)
    {
        byte[] kimlik = id.ToByteArray();
        byte[] mesaj = [.. Encoding.ASCII.GetBytes("newsletter-unsubscribe:"), .. kimlik];
        byte[] imza = HMACSHA256.HashData(Convert.FromBase64String(NotificationsTestSettings.SigningKey), mesaj);

        return Base64Url.EncodeToString([.. kimlik, .. imza]);
    }

    private static async Task<JsonElement> GovdeAsync(HttpResponseMessage cevap) =>
        JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement;

    /// <summary>Durumu doğrudan yazar; teyit bekleyen kayda istenirse geçerli bir anahtar verir.</summary>
    private async Task<string> AbonelikYazAsync(string adres, int durum, TimeSpan? anahtarSuresi = null)
    {
        byte[] ham = RandomNumberGenerator.GetBytes(32);

        await using NpgsqlConnection baglanti = new(container.GetConnectionString());
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = baglanti.CreateCommand();
        komut.CommandText = """
            INSERT INTO notifications.newsletter_subscriptions
                (id, email, normalized_email, locale, status, confirmation_token_hash, confirmation_expires_at, created_at, version)
            VALUES (gen_random_uuid(), @adres, @adres, 'tr', @durum, @ozet, @sure, now(), 0)
            """;
        komut.Parameters.AddWithValue("adres", adres);
        komut.Parameters.AddWithValue("durum", durum);
        komut.Parameters.AddWithValue("ozet", anahtarSuresi is null ? DBNull.Value : SHA256.HashData(ham));
        komut.Parameters.AddWithValue("sure", anahtarSuresi is null ? DBNull.Value : DateTimeOffset.UtcNow + anahtarSuresi.Value);
        await komut.ExecuteNonQueryAsync();

        return Base64Url.EncodeToString(ham);
    }

    private async Task<Guid> AbonelikIdAsync(string adres)
    {
        await using NpgsqlConnection baglanti = new(container.GetConnectionString());
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = baglanti.CreateCommand();
        komut.CommandText = "SELECT id FROM notifications.newsletter_subscriptions WHERE normalized_email = @adres";
        komut.Parameters.AddWithValue("adres", adres);

        return (Guid)(await komut.ExecuteScalarAsync())!;
    }

    private async Task<int> SayAsync(string tabloVeKosul)
    {
        await using NpgsqlConnection baglanti = new(container.GetConnectionString());
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = baglanti.CreateCommand();
        komut.CommandText = $"SELECT COUNT(*) FROM notifications.{tabloVeKosul}";

        return Convert.ToInt32(await komut.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
