using CarGallery.BuildingBlocks;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// R9/AC-27 — `alt` metni locale başına ve GERÇEKTEN kalıcı. Domain testi alanın var
/// olduğunu gösteriyor; bu test onun `jsonb` kolonuna yazıldığını ve geri okunduğunu
/// gösteriyor. İkisi farklı şey: 0010a'da aynı sınıftan bir eşleme hatası yalnız gerçek
/// PostgreSQL'de görünmüştü.
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class AltMetniKaliciligiTests(MedyaOrtami ortam)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0011/AC-38: Bir varlık etkin her locale için ayrı `alt` metni taşıyabilir")]
    public async Task Tasir_AltMetni_UcLocaleIcinKalici()
    {
        Guid id = await YukleAsync();

        await using (MediaDbContext yaz = ortam.CreateContext())
        {
            MediaAsset asset = await yaz.Assets.SingleAsync(a => a.Id == id);

            asset.SetAltText(
                LocalizedText.From(
                [
                    new KeyValuePair<Locale, string>(Locale.Create("tr").Value, "beyaz BMW 320i, önden"),
                    new KeyValuePair<Locale, string>(Locale.Create("ar").Value, "بي إم دبليو 320i بيضاء، من الأمام"),
                    new KeyValuePair<Locale, string>(Locale.Create("en").Value, "white BMW 320i, front view"),
                ]),
                Simdi);

            await yaz.SaveChangesAsync();
        }

        await using MediaDbContext oku = ortam.CreateContext();
        MediaAsset geri = await oku.Assets.AsNoTracking().SingleAsync(a => a.Id == id);

        Dictionary<string, string> metinler = geri.AltText.ToDictionary();

        Assert.Equal(3, metinler.Count);
        Assert.Equal("beyaz BMW 320i, önden", metinler["tr"]);
        // Arapça metin `jsonb` üzerinden bozulmadan dönmeli: kodlama hatası sessizdir ve
        // yalnız Arapça vitrinde görünür.
        Assert.Equal("بي إم دبليو 320i بيضاء، من الأمام", metinler["ar"]);
        Assert.Equal("white BMW 320i, front view", metinler["en"]);
    }

    /// <summary>
    /// D5 — `0011` `alt` metni için HTTP yazma ucu açmıyor; giriş `0012` ile gelecek.
    /// Kritere değil KARARA bağlı bir test: yarın biri sessizce bir uç açarsa, kararın
    /// değiştiği burada görünür.
    /// </summary>
    [Fact]
    public void Acilmaz_AltMetniIcin_YazmaUcu()
    {
        string[] uclar = [.. System.IO.Directory
            .EnumerateFiles(KaynakKoku("src/Modules/Media/Media/Endpoints"), "*.cs")
            .Select(System.IO.File.ReadAllText)];

        foreach (string govde in uclar)
        {
            Assert.DoesNotContain("alt-text", govde, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPut", govde, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPatch", govde, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0011/AC-39: `alt` metni boş olan bir varlık yine hazır olabilir")]
    public async Task Olabilir_Hazir_AltMetniBosOlsaDa()
    {
        Guid id = await YukleAsync();

        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        MediaAssetDto? okunan = await SorguAsync(id);

        Assert.NotNull(okunan);
        Assert.Empty(okunan.AltText);
        Assert.Equal(MediaProcessingStatus.Uploaded, okunan.Status);
    }

    private async Task<Guid> YukleAsync()
    {
        await ortam.SifirlaAsync();

        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        UploadMediaAssetCommand komut = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            ayarlar,
            new SabitSaat(Simdi));

        Result<UploadMediaAssetResult> sonuc = await komut.ExecuteAsync(
            GorselFixture.DikeyJpeg(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);

        return sonuc.Value.Id;
    }

    private async Task<MediaAssetDto?> SorguAsync(Guid id)
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        MediaAssetQueries sorgular = new(context, new MediaUrlResolver(ayarlar));

        return await sorgular.FindAsync(id, CancellationToken.None);
    }

    private static string KaynakKoku(string goreli)
    {
        System.IO.DirectoryInfo? dizin = new(AppContext.BaseDirectory);

        while (dizin is not null)
        {
            if (System.IO.Directory.EnumerateFiles(dizin.FullName, "CarGallery.sln*").Any())
            {
                return System.IO.Path.Combine(dizin.FullName, goreli);
            }

            dizin = dizin.Parent;
        }

        throw new InvalidOperationException("Çözüm kökü bulunamadı.");
    }
}
