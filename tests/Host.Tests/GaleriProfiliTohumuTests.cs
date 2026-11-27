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

    [Fact(DisplayName = "0025/AC-62, AC-71: Perf tohumu her aktif şubeye AC-60'ın programını (yedi günde üçer, 21 farklı çift) yazar ve `0024`'ün tohumunu korur: `sameAs` altı adres.")]
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

        JsonElement dealer = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/ar/")).JsonLd());
        Assert.Equal(6, dealer.GetProperty("sameAs").GetArrayLength());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.Equal(21, oge.GetProperty("openingHoursSpecification").GetArrayLength()));
    }

    [Fact(DisplayName = "0027/AC-70: Perf tohumu her aktif şubenin penceresine 12 özel gün (üçer aralık) yazar; `0024`'ün hesapları ve `0025`'in programı korunur; yapısal veride 36 öğe")]
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

        JsonElement dealer = Assert.Single(new HtmlParser().ParseDocument(await ziyaretci.GetStringAsync("/ar/")).JsonLd());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.Equal(36, oge.GetProperty("specialOpeningHoursSpecification").GetArrayLength()));
    }
}
