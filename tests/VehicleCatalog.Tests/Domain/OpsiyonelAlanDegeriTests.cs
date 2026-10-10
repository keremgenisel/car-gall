using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// Tipli opsiyonel değerin ayrıştırılması (0010b **D17**, **R6**). Veritabanı görmeden
/// sınanıyor: tip denetimi bir BİÇİM kararıdır ve `Domain`'e aittir.
/// </summary>
public sealed class OpsiyonelAlanDegeriTests
{
    private static readonly CatalogCurrency Para = new("TRY", 2);

    [Fact(DisplayName = "0010b/AC-11: Tutar tipli bir alana sayı olmayan bir değer verildiğinde istek reddedilir")]
    public void Reddedilir_TutarAlaninaSayiOlmayanDeger()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Amount, "on iki bin", Para);

        Assert.Equal(OptionalFieldValue.InvalidValue, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010b/AC-11: Tutar tipli bir alana sayı olmayan bir değer verildiğinde istek reddedilir")]
    public void Kabul_GecerliTutar()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Amount, "12500.50", Para);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Assert.Equal(FieldValueKind.Amount, sonuc.Value.Kind);
        Assert.Equal(12_500.50m, sonuc.Value.Amount!.Value.Amount);
        Assert.Equal("TRY", sonuc.Value.Amount.Value.CurrencyCode);
    }

    /// <summary>
    /// `Money` sessiz yuvarlamayı reddediyor: iki ondalıklı bir kurulumda üç ondalıklı
    /// tutar KABUL EDİLMEZ, kırpılmaz. Kırpma yalnız gözle fark edilen bir veri kaybıdır.
    /// </summary>
    [Fact]
    public void Reddedilir_ParaBiriminden_FazlaOndalik()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Amount, "12500.505", Para);

        Assert.Equal(OptionalFieldValue.InvalidValue, sonuc.Error.Code);
    }

    [Theory(DisplayName = "0010b/AC-12: Tarih tipli bir alana geçerli bir tarih olmayan bir değer verildiğinde istek reddedilir")]
    [InlineData("dün")]
    [InlineData("2024-13-01")]
    [InlineData("2024-02-30")]
    // Belirsiz biçim: "15/03/2024" hem 15 Mart hem 3 Ekim olabilir ve hangisi olduğu
    // istekte yazmıyor. Belirsizi kabul etmek sessiz veri hatasıdır.
    [InlineData("15/03/2024")]
    public void Reddedilir_GecersizTarih(string gecersiz)
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Date, gecersiz, Para);

        Assert.Equal(OptionalFieldValue.InvalidValue, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010b/AC-12: Tarih tipli bir alana geçerli bir tarih olmayan bir değer verildiğinde istek reddedilir")]
    public void Kabul_IsoTarih()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Date, "2024-03-15", Para);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(new DateOnly(2024, 3, 15), sonuc.Value.Date);
    }

    /// <summary>
    /// Ayrıştırma her zaman `InvariantCulture` ile: `1.250` Türkçe'de bin iki yüz elli,
    /// İngilizce'de bir nokta iki beş eder ve hangi dilde girildiği istekte yazmıyor.
    /// </summary>
    [Fact]
    public void Ayristirir_SayiyiDildenBagimsiz()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.Integer, "1250", Para);

        Assert.Equal(1250L, sonuc.Value.Integer);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Ayristirir_EvetHayir(string ham, bool beklenen)
    {
        Assert.Equal(beklenen, OptionalFieldValue.Parse(FieldValueKind.Boolean, ham, Para).Value.Boolean);
    }

    [Fact]
    public void Reddedilir_EvetHayirDisindakiDeger()
    {
        Assert.Equal(
            OptionalFieldValue.InvalidValue,
            OptionalFieldValue.Parse(FieldValueKind.Boolean, "evet", Para).Error.Code);
    }

    /// <summary>
    /// Locale'li metin BU YOLDAN geçmez: taşıyıcısı tek bir dize değil, locale → metin
    /// eşlemesidir. Buraya o tiple gelmek bir çağrı hatasıdır ve sessizce metne düşmez.
    /// </summary>
    [Fact]
    public void Reddedilir_LocaleliMetinHamDizeden()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(FieldValueKind.LocalizedText, "Beyaz", Para);

        Assert.Equal(OptionalFieldValue.KindMismatch, sonuc.Error.Code);
    }

    [Fact]
    public void Reddedilir_UstUzunlukSiniriAsildiginda()
    {
        Result<OptionalFieldValue> sonuc = OptionalFieldValue.Parse(
            FieldValueKind.Text,
            new string('M', OptionalFieldValue.MaximumTextLength + 1),
            Para);

        Assert.Equal(OptionalFieldValue.InvalidValue, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010b/AC-9: Her opsiyonel alan R6'daki kapalı kümeden bir tipe sahiptir; tipi beyan edilmemiş bir alana değer kaydedilemez")]
    public void Tanimlidir_TramerinTipi()
    {
        Assert.True(VehicleFieldDefinitions.TryGet(VehicleFieldKeys.DamageRecordAmount, out VehicleFieldDefinition tanim));
        Assert.Equal(FieldValueKind.Amount, tanim.Kind);
    }

    [Fact(DisplayName = "0010b/AC-9: Her opsiyonel alan R6'daki kapalı kümeden bir tipe sahiptir; tipi beyan edilmemiş bir alana değer kaydedilemez")]
    public void Tanimsizdir_DefterdeOlmayanAlan()
    {
        Assert.False(VehicleFieldDefinitions.TryGet("uydurma_alan", out _));
    }

    /// <summary>
    /// Çekirdek alanlar defterde YOKTUR: onlar agregatın kendi alanları ve opsiyonel alan
    /// değeri olarak yazılamazlar.
    /// </summary>
    [Fact]
    public void Defterde_CekirdekAlanBulunmaz()
    {
        Assert.False(VehicleFieldDefinitions.TryGet(VehicleFieldKeys.BodyType, out _));
    }
}
