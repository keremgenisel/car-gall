using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class MoneyTests
{
    [Fact(DisplayName = "0001/AC-22: Kurulum para birimi KWD iken 1250,500 tutarlı ilanın kartında tutar üç ondalıkla gösterilir; yuvarlama veya kesme yapılmaz")]
    public void UcOndalikKorunur_KwdTutarinda()
    {
        Result<Money> result = Money.Create(1250.500m, "KWD", minorUnitExponent: 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(1250.500m, result.Value.Amount);
        Assert.Equal(3, result.Value.MinorUnitExponent);
        Assert.Equal("KWD", result.Value.CurrencyCode);
    }

    [Fact]
    public void Reddedilir_UssuAsanOndalikBasamak()
    {
        Result<Money> result = Money.Create(1250.5005m, "KWD", minorUnitExponent: 3);

        Assert.True(result.IsFailure);
        Assert.Equal(Money.AmountPrecisionExceeded, result.Error.Code);
    }

    [Fact]
    public void Yuvarlanir_AcikcaIstendiginde()
    {
        Result<Money> result = Money.Round(1250.5005m, "KWD", minorUnitExponent: 3, MidpointRounding.ToEven);

        Assert.True(result.IsSuccess);
        Assert.Equal(1250.500m, result.Value.Amount);
    }

    [Theory]
    [InlineData("kwd")]
    [InlineData("TR")]
    [InlineData("TRYY")]
    [InlineData("")]
    public void Reddedilir_Iso4217BicimineUymayanKod(string currencyCode)
    {
        Result<Money> result = Money.Create(100m, currencyCode, minorUnitExponent: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(Money.InvalidCurrencyCode, result.Error.Code);
    }

    [Fact]
    public void Reddedilir_SaklamaHassasiyetiniAsanUs()
    {
        Result<Money> result = Money.Create(100m, "TRY", minorUnitExponent: 5);

        Assert.True(result.IsFailure);
        Assert.Equal(Money.InvalidMinorUnitExponent, result.Error.Code);
    }
}
