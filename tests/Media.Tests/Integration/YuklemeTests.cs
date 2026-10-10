using CarGallery.BuildingBlocks;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// R1/R2/R11 — yükleme ve reddetme. Gerçek veritabanı ve gerçek depo: reddedilen dosyanın
/// varlık kaydı OLUŞTURMADIĞI ancak ikisine birlikte bakılarak kanıtlanır.
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class YuklemeTests(MedyaOrtami ortam)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0011/AC-10: Yüklenen varlık `yüklendi` durumunda, depolama yolu ve kimliğiyle kaydedilir")]
    public async Task Kaydedilir_Varlik_DepolamaYoluylaBirlikte()
    {
        await ortam.SifirlaAsync();

        Result<UploadMediaAssetResult> sonuc = await YukleAsync(GorselFixture.DikeyJpeg());

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(MediaProcessingStatus.Uploaded, sonuc.Value.Status);

        await using MediaDbContext oku = ortam.CreateContext();
        MediaAsset kayit = await oku.Assets.AsNoTracking().SingleAsync(a => a.Id == sonuc.Value.Id);

        Assert.Equal("image/jpeg", kayit.ContentType);
        Assert.Equal($"private/originals/{sonuc.Value.Id:D}.jpg", kayit.OriginalObjectKey);
        Assert.Equal(600, kayit.SourceWidth);
        Assert.Equal(900, kayit.SourceHeight);

        // Orijinal GERÇEKTEN depoda: kayıt yolu gösteriyor, depo onu veriyor.
        Assert.NotNull(await ortam.NesneOkuAsync(kayit.OriginalObjectKey));
    }

    [Fact(DisplayName = "0011/AC-3: Yapılandırılan tür kümesinde olmayan bir dosya `415` ile reddedilir ve varlık kaydı oluşmaz")]
    public async Task Reddedilir_DesteklenmeyenTur()
    {
        await ortam.SifirlaAsync();

        // GIF: tanınan bir görsel formatı ama kabul kümesinde değil.
        byte[] gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00];

        Result<UploadMediaAssetResult> sonuc = await YukleAsync(gif);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.UnsupportedType, sonuc.Error.Code);
        await KayitYokAsync();
    }

    [Fact(DisplayName = "0011/AC-4: Dosyanın türü içeriğinden belirlenir; `image/jpeg` adıyla gönderilen bir SVG reddedilir")]
    public async Task Reddedilir_JpegAdiylaGonderilenSvg()
    {
        await ortam.SifirlaAsync();

        Result<UploadMediaAssetResult> sonuc = await YukleAsync(GorselFixture.SvgGovdesi());

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.UnsupportedType, sonuc.Error.Code);

        // Hangi tür olarak TANINDIĞI da yanıtta: "bilinmeyen dosya" demek, SVG'nin neden
        // reddedildiğini logdan okunamaz kılardı.
        Assert.Equal("svg", sonuc.Error.Parameters!["detected"]);
        await KayitYokAsync();
    }

    [Fact(DisplayName = "0011/AC-6: Yapılandırılan boyut sınırını aşan dosya `413` ile reddedilir ve varlık kaydı oluşmaz")]
    public async Task Reddedilir_BoyutSinirindanBuyuk()
    {
        await ortam.SifirlaAsync();

        byte[] govde = GorselFixture.DikeyJpeg();

        // Sınır YAPILANDIRILAN değerdir, koda gömülü bir sabit değil: gövdenin bir baytı
        // altına çekiliyor ve aynı gövde reddediliyor.
        Result<UploadMediaAssetResult> sonuc = await YukleAsync(
            govde, ortam.Options(maximumByteSize: govde.Length - 1));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.TooLarge, sonuc.Error.Code);
        await KayitYokAsync();
    }

    [Fact(DisplayName = "0011/AC-7: Yapılandırılan piksel bütçesini aşan dosya reddedilir ve varlık kaydı oluşmaz")]
    public async Task Reddedilir_PikselButcesiniAsan()
    {
        await ortam.SifirlaAsync();

        // 600x900 = 540.000 piksel. Dosya küçük (onlarca KB) — bayt sınırı bunu GÖRMEZ,
        // D10(c)'nin varlık nedeni tam olarak bu.
        byte[] govde = GorselFixture.DikeyJpeg();

        Result<UploadMediaAssetResult> sonuc = await YukleAsync(
            govde, ortam.Options(maximumPixelCount: 539_999));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.PixelBudgetExceeded, sonuc.Error.Code);
        Assert.Equal("540000", sonuc.Error.Parameters!["pixels"]);
        await KayitYokAsync();

        // Aynı gövde, bütçe bir piksel yukarıda: kabul edilir. Reddin sebebinin BÜTÇE
        // olduğunun kanıtı budur, gövdenin kendisi değil.
        Result<UploadMediaAssetResult> sinirda = await YukleAsync(
            govde, ortam.Options(maximumPixelCount: 540_000));

        Assert.True(sinirda.IsSuccess);
    }

    /// <summary>
    /// D16/AC-21 — orijinal `private/` önekinde durur. Public önekte duran bir orijinal,
    /// CDN'e bağlı olduğu an ham EXIF'i (konum dâhil) yayınlamak demekti.
    /// </summary>
    [Fact]
    public async Task Durur_Orijinal_PrivateOnekinde()
    {
        await ortam.SifirlaAsync();

        Result<UploadMediaAssetResult> sonuc = await YukleAsync(GorselFixture.ExifliJpeg());

        Assert.True(sonuc.IsSuccess);

        await using MediaDbContext oku = ortam.CreateContext();
        MediaAsset kayit = await oku.Assets.AsNoTracking().SingleAsync(a => a.Id == sonuc.Value.Id);

        Assert.StartsWith("private/", kayit.OriginalObjectKey, StringComparison.Ordinal);
        Assert.False(MediaObjectKeys.IsPubliclyReachable(kayit.OriginalObjectKey));
    }

    private async Task<Result<UploadMediaAssetResult>> YukleAsync(
        byte[] content,
        MediaOptions? options = null)
    {
        MediaOptions ayarlar = options ?? ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        UploadMediaAssetCommand komut = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            ayarlar,
            new SabitSaat(Simdi));

        return await komut.ExecuteAsync(content, CancellationToken.None);
    }

    private async Task KayitYokAsync()
    {
        await using MediaDbContext oku = ortam.CreateContext();

        Assert.Empty(await oku.Assets.AsNoTracking().ToListAsync());
    }
}
