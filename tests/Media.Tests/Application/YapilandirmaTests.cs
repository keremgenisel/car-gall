using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Media.Domain;

namespace CarGallery.Media.Tests.Application;

/// <summary>
/// C6'nın doğrulama tarafı. Uygulamanın GERÇEKTEN ayağa kalkmadığı `Host.Tests`'te
/// sınanıyor (AC-31…33); burada hangi eksikliğin hangi kodu ürettiği ölçülüyor —
/// operatör hata mesajından neyin eksik olduğunu okuyabilmeli.
/// </summary>
public sealed class YapilandirmaTests
{
    private static MediaSetup Tam() => new(
        ConnectionString: "Host=localhost;Database=cargallery",
        StorageServiceUrl: "http://localhost:54566",
        StorageBucket: "cargallery-media",
        StorageAccessKey: "anahtar",
        StorageSecretKey: "parola",
        StorageRegion: "us-east-1",
        StorageForcePathStyle: true,
        PublicBaseUrl: "https://cdn.ornek.test/cargallery-media/",
        WatermarkObjectKey: "private/brand/watermark.png",
        MaximumByteSize: 15_728_640,
        MaximumPixelCount: 50_000_000,
        AllowedImageFormats: ["jpeg", "png", "webp"]);

    [Fact]
    public void Gecerlidir_TamYapilandirma()
    {
        Result<MediaOptions> sonuc = MediaOptions.Create(Tam());

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(3, sonuc.Value.AllowedFormats.Count);
    }

    [Theory]
    [InlineData("storage_url_missing")]
    [InlineData("storage_bucket_missing")]
    [InlineData("storage_credentials_missing")]
    [InlineData("storage_region_missing")]
    [InlineData("public_base_url_missing")]
    [InlineData("watermark_missing")]
    [InlineData("invalid_upload_limits")]
    [InlineData("allowed_formats_missing")]
    public void Reddedilir_EksikYapilandirma(string beklenenKod)
    {
        MediaSetup eksik = beklenenKod switch
        {
            "storage_url_missing" => Tam() with { StorageServiceUrl = string.Empty },
            "storage_bucket_missing" => Tam() with { StorageBucket = "  " },
            "storage_credentials_missing" => Tam() with { StorageSecretKey = string.Empty },
            "storage_region_missing" => Tam() with { StorageRegion = string.Empty },
            "public_base_url_missing" => Tam() with { PublicBaseUrl = "/goreli/yol" },
            "watermark_missing" => Tam() with { WatermarkObjectKey = string.Empty },
            "invalid_upload_limits" => Tam() with { MaximumPixelCount = 0 },
            _ => Tam() with { AllowedImageFormats = [] },
        };

        Result<MediaOptions> sonuc = MediaOptions.Create(eksik);

        Assert.True(sonuc.IsFailure);
        Assert.Equal($"media.{beklenenKod}", sonuc.Error.Code);
    }

    /// <summary>
    /// Taban adres ÇAĞRILABİLİR olmalı, yalnız "mutlak" değil.
    ///
    /// <para><b>Bu test bir hata buldu ve sonra kuralı korudu:</b> ilk yazımda doğrulama
    /// yalnız <c>UriKind.Absolute</c>'a bakıyordu ve <c>/goreli/yol</c> GEÇİYORDU —
    /// Unix'te .NET onu <c>file:///goreli/yol</c> olarak ayrıştırıyor. Vitrindeki her
    /// görselin yolu <c>file://</c> olurdu ve sunucu logunda hiçbir şey görünmezdi.</para>
    /// </summary>
    [Theory]
    [InlineData("cdn.ornek.test")]
    [InlineData("/goreli/yol")]
    [InlineData("file:///tmp/kova/")]
    [InlineData("ftp://cdn.ornek.test/")]
    public void Reddedilir_CagrilamayanTabanAdres(string tabanAdres)
    {
        Result<MediaOptions> sonuc = MediaOptions.Create(Tam() with { PublicBaseUrl = tabanAdres });

        Assert.True(sonuc.IsFailure, $"'{tabanAdres}' kabul edildi.");
        Assert.Equal("media.public_base_url_missing", sonuc.Error.Code);
    }

    [Theory]
    [InlineData("/var/kova")]
    [InlineData("file:///tmp/kova")]
    public void Reddedilir_CagrilamayanDepolamaAdresi(string servisAdresi)
    {
        Result<MediaOptions> sonuc = MediaOptions.Create(Tam() with { StorageServiceUrl = servisAdresi });

        Assert.True(sonuc.IsFailure, $"'{servisAdresi}' kabul edildi.");
        Assert.Equal("media.storage_url_missing", sonuc.Error.Code);
    }

    /// <summary>
    /// AC-9 — kabul edilen tür kümesi YAPILANDIRMADAN okunur. Kriter yapılandırılan
    /// değeri doğruluyor: aynı gövde, iki ayrı küme, iki ayrı sonuç.
    /// </summary>
    [Theory(DisplayName = "0011/AC-9: Kabul edilen tür kümesi ve boyut sınırı yapılandırmadan okunur; koda gömülü bir sabit yoktur")]
    [InlineData("jpeg")]
    [InlineData("png")]
    [InlineData("webp")]
    public void Okunur_TurKumesi_Yapilandirmadan(string tur)
    {
        MediaOptions ayarlar = MediaOptions.Create(Tam() with { AllowedImageFormats = [tur] }).Value;

        Assert.Single(ayarlar.AllowedFormats);
        Assert.Equal(ImageFormatDetection.ParseConfigurable(tur), ayarlar.AllowedFormats.Single());

        // Diğer iki tür artık KABUL EDİLMİYOR — küme koda gömülü olsaydı üçü de geçerdi.
        foreach (DetectedImageFormat diger in ImageFormatDetection.ConfigurableFormats)
        {
            Assert.Equal(
                diger == ImageFormatDetection.ParseConfigurable(tur),
                ImageFormatDetection.IsAccepted(diger, ayarlar.AllowedFormats));
        }
    }

    /// <summary>
    /// AC-5 — yapılandırmaya SVG eklenemez. D23: bir yapılandırma satırıyla geri
    /// açılabilen güvenlik kararı, verilmemiş karardır.
    ///
    /// <para>Tanınmayan bir ad da reddediliyor: sessizce atlanan bir ad, operatörün
    /// yapılandırdığını sandığı türün hiç kabul edilmemesi demekti.</para>
    /// </summary>
    [Theory(DisplayName = "0011/AC-5: Yapılandırmaya SVG eklenemez; eklenmişse uygulama ayağa kalkmaz")]
    [InlineData("svg")]
    [InlineData("image/svg+xml")]
    [InlineData("SVG")]
    [InlineData("gif")]
    [InlineData("bmp")]
    public void Kalkmaz_Uygulama_SvgYapilandirildiginda(string tur)
    {
        Result<MediaOptions> sonuc = MediaOptions.Create(
            Tam() with { AllowedImageFormats = ["jpeg", tur] });

        Assert.True(sonuc.IsFailure, $"'{tur}' kabul edildi.");
        Assert.Equal("media.allowed_format_not_permitted", sonuc.Error.Code);
        Assert.Equal(tur.Trim().ToLowerInvariant(), sonuc.Error.Parameters!["format"]);
    }
}
