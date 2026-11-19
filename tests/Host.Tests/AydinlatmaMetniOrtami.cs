using System.Data.Common;
using System.Globalization;
using System.Net.Http.Json;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0019` — iki modülün (`Content`, `Leads`) birlikte kayıtlı olduğu GERÇEK ana bilgisayar
/// (Bilinen tuzak 1): sürüm sahte bir porttan değil, `Content`'in kendi okumasından gelir.
/// Vitrinde sabit, yayındaki tek bir ilan vardır; talepler ona gider.
/// </summary>
internal sealed class AydinlatmaMetniOrtami : IAsyncDisposable
{
    /// <summary>`privacy` sistem sayfasının sabit kimliği (`0017` D6).</summary>
    internal static readonly Guid Privacy = Guid.Parse("0192c000-0000-7000-8000-0000c0a70002");

    internal const string IlanSlug = "toyota-corolla-2019-kvk019";

    private static readonly Guid SubeId = Guid.Parse("00000000-0000-7000-8000-00000000c019");
    private static readonly Guid ModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private readonly List<WebApplicationFactory<Program>> fabrikalar = [];

    internal string ConnectionString => container.GetConnectionString();

    internal static async Task<AydinlatmaMetniOrtami> KurAsync()
    {
        AydinlatmaMetniOrtami ortam = new();
        await ortam.container.StartAsync();
        await TestSchema.ApplyAsync(ortam.ConnectionString);

        WebApplicationFactory<Program> ilk = ortam.Fabrika();
        await ilk.Services.SeedVehicleReferenceForDevelopmentAsync();
        await ilk.Services.SeedVehicleCatalogForDevelopmentAsync(0, [ModelId]);
        await ortam.IlanEkleAsync();

        return ortam;
    }

    /// <summary>
    /// Yeni bir ana bilgisayar; hız bütçesi SÜREÇ başınadır, yani her fabrika kendi taze
    /// bütçesiyle başlar. <paramref name="talepSiniri"/> verilmezse spec'in sayısı (5) kalır.
    /// </summary>
    internal WebApplicationFactory<Program> Fabrika(int? talepSiniri = null)
    {
        WebApplicationFactory<Program> fabrika = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", ConnectionString);
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            if (talepSiniri is int sinir)
            {
                builder.UseSetting("Leads:RateLimit:CapturedLeadLimit", sinir.ToString(CultureInfo.InvariantCulture));
                builder.UseSetting("Leads:RateLimit:RequestLimit", sinir.ToString(CultureInfo.InvariantCulture));
            }
        });

        fabrikalar.Add(fabrika);
        return fabrika;
    }

    /// <summary>Vitrindeki ilana bir talep; anahtar talebin kimliğidir.</summary>
    internal static Task<HttpResponseMessage> TalepAsync(HttpClient client, string locale, string anahtar, bool izin = true)
    {
        HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(new
            {
                listingSlug = IlanSlug,
                leadType = "information_request",
                locale,
                fullName = "Ayşe Yılmaz",
                phone = "+905321112233",
                email = (string?)null,
                message = (string?)null,
                consentGiven = izin,
            }),
        };

        istek.Headers.Add("Idempotency-Key", anahtar);

        return client.SendAsync(istek);
    }

    /// <summary>Talebin kaydındaki bağ (D7: bugün bunu bir teknik kişi sorguyla okur).</summary>
    internal async Task<(string Durum, Guid? Surum, bool Izin)> BagAsync(string anahtar)
    {
        await using NpgsqlConnection baglanti = new(ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(
            "SELECT privacy_notice_status, privacy_notice_version_id, consent_given FROM leads.leads WHERE idempotency_key = @anahtar",
            baglanti);
        komut.Parameters.AddWithValue("anahtar", anahtar);
        await using DbDataReader okuyucu = await komut.ExecuteReaderAsync();

        Assert.True(await okuyucu.ReadAsync(), $"'{anahtar}' anahtarlı talep yok");
        return (okuyucu.GetString(0), okuyucu.IsDBNull(1) ? null : okuyucu.GetGuid(1), okuyucu.GetBoolean(2));
    }

    /// <summary>Sürümün tam hâli — `Content`'in kendi sözleşmesinden (R4).</summary>
    internal static async Task<PrivacyNoticeVersionDto?> SurumAsync(WebApplicationFactory<Program> fabrika, Guid id)
    {
        using IServiceScope scope = fabrika.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPrivacyNoticeQueries>().FindVersionAsync(id, CancellationToken.None);
    }

    /// <summary>Tek değerli bir sorgu.</summary>
    internal async Task<long> SayAsync(string sql)
    {
        await using NpgsqlConnection baglanti = new(ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(sql, baglanti);
        return Convert.ToInt64(await komut.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    /// <summary>Kurulum SQL'i (test durumunu hazırlamak için).</summary>
    internal async Task SqlAsync(string sql)
    {
        await using NpgsqlConnection baglanti = new(ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(sql, baglanti);
        await komut.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// `privacy`'yi yeni kurulumdaki hâline döndürür: hiçbir dilde metin yok, sürüm belirteci 1
    /// (`InitialContent` tohumu). Yayınlanmış sürümler silinmez (C5) ve silinmesi de gerekmez —
    /// her test kendi talebinin bağlandığı sürümü okur.
    /// </summary>
    internal Task YeniKurulumAsync() => SqlAsync(
        $"""
        DELETE FROM content.page_localizations WHERE page_id = '{Privacy}';
        DELETE FROM content.page_old_slugs WHERE page_id = '{Privacy}';
        UPDATE content.pages SET version = 1 WHERE id = '{Privacy}';
        """);

    public async ValueTask DisposeAsync()
    {
        foreach (WebApplicationFactory<Program> fabrika in fabrikalar)
        {
            await fabrika.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    private Task IlanEkleAsync() => SqlAsync(
        $$"""
        INSERT INTO vehicle_catalog.showcase_listings (
            listing_id, vehicle_id, is_reserved, showcase_sort_weight, published_at, slug,
            body_type_slug, fuel_type_slug, transmission_type_slug, drivetrain_type_slug,
            trim_level_slug, color_names, vehicle_model_id, model_year, mileage_kilometers,
            price_amount, price_currency_code, price_minor_unit_exponent, price_on_request,
            branch_id, branch_names, cover_asset_id, cover_media_url)
        VALUES (
            gen_random_uuid(), gen_random_uuid(), false, 0, now(), '{{IlanSlug}}',
            'sedan', 'diesel', 'automatic', 'front-wheel-drive',
            'comfort', '{"tr":"Beyaz"}'::jsonb, '{{ModelId}}', 2019, 125000,
            1250000, 'TRY', 2, false,
            '{{SubeId}}', '{"tr":"Merkez Şube"}'::jsonb, NULL, NULL)
        """);
}
