using System.Text.Json;
using AngleSharp.Html.Parser;
using CarGallery.Modules.Dealership;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0024` P9 · `0025` P9 — perf koşusunun galeri profili tohumu (`Development:SeedGalleryProfile`),
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
}
