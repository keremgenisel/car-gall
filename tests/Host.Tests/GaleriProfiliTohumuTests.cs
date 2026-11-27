using System.Text.Json;
using AngleSharp.Html.Parser;
using CarGallery.Modules.Dealership;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0024` P9 · `0025` P9 · `0027` P10 — perf koşusunun galeri profili tohumu (`Development:SeedGalleryProfile`),
/// perf koşusunun kurulumuyla: taze veritabanı ve kurulum tohumunun İKİ aktif şubesi
/// (`0025` D10(c)). Ayrı sınıf, ayrı veritabanı: tohum bütün aktif şubelere yazar ve başka bir
/// sınıfın şubelerinin beklentisini bozmamalı.
/// </summary>
public sealed class GaleriProfiliTohumuTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);
        });
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0026/AC-77, AC-59: Perf tohumu logo kimliğini atar ve önceki kartların tohumunu korur: altı hesap ve iki aktif şubenin en kötü programı yerinde.")]
    public async Task Tohumlar_Logo_OncekileriKorur()
    {
        Guid logo = CarGallery.Modules.VehicleCatalog.VehicleCatalogModule.SeedLogoMediaAssetId;

        await factory!.Services.SeedGalleryProfileForDevelopmentAsync(logo);

        HttpClient ziyaretci = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        JsonElement galeri = JsonDocument.Parse(await ziyaretci.GetStringAsync("/api/dealership/gallery?locale=ar")).RootElement;
        Assert.Equal(6, galeri.GetProperty("socialLinks").GetArrayLength());
        Assert.All(galeri.GetProperty("branches").EnumerateArray(), sube =>
            Assert.All(sube.GetProperty("openingHours").EnumerateObject(), gun => Assert.Equal(3, gun.Value.GetArrayLength())));

        await using Npgsql.NpgsqlConnection baglanti = new(container.GetConnectionString());
        await baglanti.OpenAsync();
        await using Npgsql.NpgsqlCommand komut = baglanti.CreateCommand();
        komut.CommandText = "SELECT logo_media_asset_id FROM dealership.galleries";
        Assert.Equal(logo, (Guid)(await komut.ExecuteScalarAsync())!);
    }

    [Fact(DisplayName = "0025/AC-62, AC-71, 0028/AC-14, AC-17: Perf tohumu her aktif şubeye AC-60'ın programını (yedi günde üçer, 21 farklı çift) yazar ve `0024`'ün tohumunu korur: `sameAs` altı adres.")]
    public async Task Tohumlar_ProgramVeHesaplar()
    {
        await factory!.Services.SeedGalleryProfileForDevelopmentAsync();

        HttpClient ziyaretci = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        JsonElement galeri = JsonDocument.Parse(await ziyaretci.GetStringAsync("/api/dealership/gallery?locale=ar")).RootElement;
        JsonElement[] subeler = [.. galeri.GetProperty("branches").EnumerateArray()];

        Assert.Equal(2, subeler.Length);
        Assert.Equal(6, galeri.GetProperty("socialLinks").GetArrayLength());

        foreach (JsonElement sube in subeler)
        {
            JsonProperty[] gunler = [.. sube.GetProperty("openingHours").EnumerateObject()];
            Assert.Equal(7, gunler.Length);
            Assert.All(gunler, gun => Assert.Equal(3, gun.Value.GetArrayLength()));
            Assert.Equal(21, gunler.SelectMany(gun => gun.Value.EnumerateArray()).Select(aralik => aralik.GetRawText()).Distinct().Count());
        }

        // `0028` D13 — kök ve `sameAs` ana sayfada kalır; şube öğesi yalnız ad, adres, telefon (AC-17).
        JsonElement dealer = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/ar/")).JsonLd());
        Assert.Equal(6, dealer.GetProperty("sameAs").GetArrayLength());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.Equal(HomeDepartmentKeys, oge.Anahtarlar().Order(StringComparer.Ordinal)));

        // Saatler iletişim belgesinde, aynı sayıyla (AC-14).
        JsonElement iletisim = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/ar/contact")).JsonLd());
        Assert.All(iletisim.GetProperty("department").EnumerateArray(), oge => Assert.Equal(21, oge.GetProperty("openingHoursSpecification").GetArrayLength()));
    }

    [Fact(DisplayName = "0027/AC-70, 0028/AC-14: Perf tohumu her aktif şubenin penceresine 12 özel gün (üçer aralık) yazar; `0024`'ün hesapları ve `0025`'in programı korunur; yapısal veride 36 öğe")]
    public async Task Tohumlar_OzelGunler_OncekileriKorur()
    {
        await factory!.Services.SeedGalleryProfileForDevelopmentAsync();

        HttpClient ziyaretci = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        JsonElement galeri = JsonDocument.Parse(await ziyaretci.GetStringAsync("/api/dealership/gallery?locale=ar")).RootElement;
        Assert.Equal(6, galeri.GetProperty("socialLinks").GetArrayLength());
        JsonElement[] subeler = [.. galeri.GetProperty("branches").EnumerateArray()];
        Assert.Equal(2, subeler.Length);

        foreach (JsonElement sube in subeler)
        {
            Assert.All(sube.GetProperty("openingHours").EnumerateObject(), gun => Assert.Equal(3, gun.Value.GetArrayLength()));
            JsonElement[] ozel = [.. sube.GetProperty("specialHours").EnumerateArray()];
            Assert.Equal(12, ozel.Length);
            Assert.All(ozel, kayit => Assert.Equal(3, kayit.GetProperty("intervals").GetArrayLength()));
        }

        // `0028` D13 — özel günler iletişim belgesinde (AC-14).
        JsonElement dealer = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/ar/contact")).JsonLd());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.Equal(36, oge.GetProperty("specialOpeningHoursSpecification").GetArrayLength()));
    }

    /// <summary>`0028` AC-17 — ana sayfanın şube öğesinin anahtar kümesi.</summary>
    private static readonly string[] HomeDepartmentKeys = ["@type", "address", "name", "telephone"];

    [Fact(DisplayName = "0028/AC-76 (D14 · P12): Perf tohumu her aktif şubeye e-posta ve koordinat yazar; ziyaretçi okumasında `email` ve `map` dolu, iletişim belgesinin öğesi ana sayfanınkine `email`, saatler, `hasMap` ve `geo` ekler")]
    public async Task Tohumlar_IletisimAlanlari()
    {
        await factory!.Services.SeedGalleryProfileForDevelopmentAsync();

        HttpClient ziyaretci = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        JsonElement[] subeler = [.. JsonDocument.Parse(await ziyaretci.GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement.GetProperty("branches").EnumerateArray()];
        Assert.Equal(2, subeler.Length);
        // Tohumun Kadıköy'ü kendi e-postasını taşır (kayıtlı değer korunur); e-postasız şubeye yazılır.
        Assert.Contains(subeler, sube => sube.GetProperty("email").GetString() == "kadikoy@otogaleri.com.tr");
        Assert.Contains(subeler, sube => sube.GetProperty("email").GetString()!.EndsWith("@galeri.example.com", StringComparison.Ordinal));
        Assert.All(subeler, sube =>
        {
            Assert.Equal(JsonValueKind.String, sube.GetProperty("email").ValueKind);
            Assert.StartsWith("https://www.google.com/maps/search/?api=1&query=", sube.GetProperty("map").GetProperty("linkUrl").GetString(), StringComparison.Ordinal);
        });

        // Anahtar kümesi KODLA kurulur: ana sayfanın kümesi + bu kartın eklediği beşi (sabit sayı değil).
        string[] beklenen = [.. HomeDepartmentKeys.Concat(["email", "geo", "hasMap", "openingHoursSpecification", "specialOpeningHoursSpecification"]).Order(StringComparer.Ordinal)];
        JsonElement dealer = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/tr/contact")).JsonLd());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.Equal(beklenen, oge.Anahtarlar().Order(StringComparer.Ordinal)));
    }
}
