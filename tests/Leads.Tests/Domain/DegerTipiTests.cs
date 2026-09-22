using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Tests.Domain;

public sealed class DegerTipiTests
{
    [Theory(DisplayName = "0005/AC-4: E.164 biçiminde olmayan bir telefon numarasıyla talep kaydedilmez")]
    [InlineData("05321112233")]
    [InlineData("+0532111223")]
    [InlineData("+90 532 111 22 33")]
    [InlineData("+90532")]
    [InlineData("905321112233")]
    public void Kaydedilmez_TelefonE164Degilse(string numara)
    {
        Result<Lead> sonuc = new LeadBuilder().Telefon(numara).Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.InvalidPhone, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-4: E.164 biçiminde olmayan bir telefon numarasıyla talep kaydedilmez")]
    public void Kaydedilir_TelefonE164Ise()
    {
        Result<Lead> sonuc = new LeadBuilder().Telefon("+905321112233").Olustur();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("+905321112233", sonuc.Value.Contact.Phone);
    }

    [Theory(DisplayName = "0005/AC-5: Geçerli biçimde olmayan bir e-posta ile talep kaydedilmez")]
    [InlineData("ayse")]
    [InlineData("ayse@")]
    [InlineData("@ornek.com")]
    [InlineData("ayse@ornek")]
    [InlineData("ayse ornek@ornek.com")]
    [InlineData("ayse@@ornek.com")]
    public void Kaydedilmez_EpostaBicimiGecersizse(string eposta)
    {
        Result<Lead> sonuc = new LeadBuilder().Telefon(null).Eposta(eposta).Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.InvalidEmail, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-5: Geçerli biçimde olmayan bir e-posta ile talep kaydedilmez")]
    public void Kaydedilir_YalnizcaEpostaVerildiginde()
    {
        Result<Lead> sonuc = new LeadBuilder().Telefon(null).Eposta("ayse@ornek.com").Olustur();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("ayse@ornek.com", sonuc.Value.Contact.Email);
        Assert.Null(sonuc.Value.Contact.Phone);
    }

    [Fact(DisplayName = "0005/AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    public void KopyalanmazSifir_FiyatSorunuzAracinda()
    {
        VehicleSnapshot anlikGoruntu = LeadBuilder.FiyatSorunuzAnlikGoruntusu();

        Assert.True(anlikGoruntu.PriceOnRequest);
        Assert.Null(anlikGoruntu.PriceAmount);
        Assert.Null(anlikGoruntu.PriceCurrencyCode);
        Assert.Null(anlikGoruntu.PriceMinorUnitExponent);
        Assert.True(anlikGoruntu.ReadPrice().IsOnRequest);
    }

    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public void UcOndalikTasir_KwdTutari()
    {
        VehicleSnapshot anlikGoruntu = LeadBuilder.KuveytAnlikGoruntusu();

        Assert.Equal(1250.500m, anlikGoruntu.PriceAmount);
        Assert.Equal("KWD", anlikGoruntu.PriceCurrencyCode);
        Assert.Equal(3, anlikGoruntu.PriceMinorUnitExponent);
    }

    /// <summary>
    /// Kriterin verdiği `1250,500` sayısı KESMEYİ YAKALAYAMAZ: iki ondalığa kesilse de
    /// aynı sayıdır (1250,50 = 1250,500). Üssün 3 olması kesme yapılmadığını göstermez,
    /// yalnız niyeti gösterir. Bu yüzden aynı kriter için üçüncü ondalığı SIFIR OLMAYAN
    /// bir tutarla ikinci bir test var: kesme burada, yalnız burada görünür.
    /// </summary>
    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public void KesilmezVeYuvarlanmaz_UcuncuOndaligiSifirOlmayanKwdTutari()
    {
        VehicleSnapshot anlikGoruntu = VehicleSnapshot.Create(
            Guid.CreateVersion7(),
            "Toyota Corolla",
            PriceSnapshot.Of(Money.Create(1250.505m, "KWD", 3).Value)).Value;

        Assert.Equal(1250.505m, anlikGoruntu.PriceAmount);
        Assert.NotEqual(1250.50m, anlikGoruntu.PriceAmount);
        Assert.NotEqual(1250.51m, anlikGoruntu.PriceAmount);
    }

    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public void Kurulamaz_UssuAsanHassasiyetliKwdTutari()
    {
        Result<Money> sonuc = Money.Create(1250.5051m, "KWD", 3);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Money.AmountPrecisionExceeded, sonuc.Error.Code);
    }
}
