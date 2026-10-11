using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// `0012a` D3/D16 — `alt` metninin yazma komutu. Gerçek PostgreSQL: metin `jsonb`
/// kolonuna yazılıp geri okunuyor ve Arapça metnin bozulmadığı ölçülüyor — kodlama hatası
/// sessizdir ve yalnız Arapça vitrinde görünür.
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class AltMetniUcuTests(MedyaOrtami ortam)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0012a/AC-11: Personel bir fotoğrafın `alt` metnini etkin her dilde yazabilir")]
    public async Task Yazabilir_AltMetni_EtkinHerDilde()
    {
        Guid id = await HazirlaAsync();

        Result sonuc = await YazAsync(id, new Dictionary<string, string>
        {
            ["tr"] = "beyaz BMW 320i, önden",
            ["ar"] = "بي إم دبليو 320i بيضاء، من الأمام",
            ["en"] = "white BMW 320i, front view",
        });

        Assert.True(sonuc.IsSuccess);

        MediaAssetDto? okunan = await OkuAsync(id);

        Assert.NotNull(okunan);
        Assert.Equal(3, okunan.AltText.Count);
        Assert.Equal("beyaz BMW 320i, önden", okunan.AltText["tr"]);
        Assert.Equal("بي إم دبليو 320i بيضاء، من الأمام", okunan.AltText["ar"]);
        Assert.Equal("white BMW 320i, front view", okunan.AltText["en"]);
    }

    [Fact(DisplayName = "0012a/AC-12: Bir dili boş bırakmak geçerli bir durumdur; varlık yine okunabilir")]
    public async Task Gecerlidir_BirDiliBosBirakmak()
    {
        Guid id = await HazirlaAsync();

        // `ar` hiç verilmiyor, `en` boş dize olarak veriliyor — ikisi de aynı sonucu
        // vermek zorunda (D16: eksik locale boş sayılır).
        Result sonuc = await YazAsync(id, new Dictionary<string, string>
        {
            ["tr"] = "beyaz BMW 320i, önden",
            ["en"] = "   ",
        });

        Assert.True(sonuc.IsSuccess);

        MediaAssetDto? okunan = await OkuAsync(id);

        Assert.NotNull(okunan);
        Assert.Single(okunan.AltText);
        Assert.Equal("beyaz BMW 320i, önden", okunan.AltText["tr"]);
        Assert.False(okunan.AltText.ContainsKey("ar"));
        Assert.False(okunan.AltText.ContainsKey("en"));
    }

    [Theory(DisplayName = "0012a/AC-14: Etkin olmayan bir locale için `alt` metni yazılamaz")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("TR-tr")]
    [InlineData("")]
    public async Task Yazilamaz_EtkinOlmayanLocale(string kod)
    {
        Guid id = await HazirlaAsync();

        Result sonuc = await YazAsync(id, new Dictionary<string, string> { [kod] = "metin" });

        Assert.True(sonuc.IsFailure, $"'{kod}' kabul edildi.");
        Assert.Equal(MediaErrors.LocaleNotEnabled, sonuc.Error.Code);
        Assert.Equal(kod, sonuc.Error.Parameters!["locale"]);
    }

    /// <summary>
    /// Etkin liste YAPILANDIRMADAN geliyor; kriter yapılandırılan değeri doğruluyor. Aynı
    /// locale bir kurulumda kabul, diğerinde ret.
    /// </summary>
    [Fact]
    public async Task Gelir_EtkinListe_Yapilandirmadan()
    {
        Guid id = await HazirlaAsync();

        Assert.True((await YazAsync(
            id,
            new Dictionary<string, string> { ["ar"] = "metin" },
            ortam.Options(enabledLocales: ["tr", "ar"]))).IsSuccess);

        Result daraltilmis = await YazAsync(
            id,
            new Dictionary<string, string> { ["ar"] = "metin" },
            ortam.Options(enabledLocales: ["tr"]));

        Assert.True(daraltilmis.IsFailure);
        Assert.Equal(MediaErrors.LocaleNotEnabled, daraltilmis.Error.Code);
    }

    [Fact]
    public async Task Reddedilir_UstSiniriAsanMetin()
    {
        Guid id = await HazirlaAsync();

        Result sonuc = await YazAsync(
            id,
            new Dictionary<string, string> { ["tr"] = new string('a', SetAltTextCommand.MaximumLength + 1) });

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.AltTextTooLong, sonuc.Error.Code);
        Assert.Equal("tr", sonuc.Error.Parameters!["locale"]);
    }

    [Fact]
    public async Task Reddedilir_VarOlmayanVarlik()
    {
        await ortam.SifirlaAsync();

        Result sonuc = await YazAsync(
            Guid.CreateVersion7(),
            new Dictionary<string, string> { ["tr"] = "metin" });

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.NotFound, sonuc.Error.Code);
    }

    private async Task<Guid> HazirlaAsync()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(Fixtures.GorselFixture.FiligranPng());

        return await YuklemeYardimcisi.YukleAsync(ortam);
    }

    private async Task<Result> YazAsync(
        Guid id,
        IReadOnlyDictionary<string, string> altText,
        MediaOptions? options = null)
    {
        MediaOptions ayarlar = options ?? ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        SetAltTextCommand komut = new(context, ayarlar, new SabitSaat(Simdi));

        return await komut.ExecuteAsync(id, altText, CancellationToken.None);
    }

    private async Task<MediaAssetDto?> OkuAsync(Guid id)
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        return await new MediaAssetQueries(context, new MediaUrlResolver(ayarlar))
            .FindAsync(id, CancellationToken.None);
    }
}
