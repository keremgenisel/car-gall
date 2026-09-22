using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

public sealed class DegerTipiTests
{
    [Theory(DisplayName = "0003/AC-9: Telefon ve WhatsApp numarası E.164 biçiminde değilse şube kaydedilemez")]
    [InlineData("0216 555 44 33")]
    [InlineData("+90 216 555 44 33")]
    [InlineData("+00216555443")]
    [InlineData("2165554433")]
    [InlineData("+90216")]
    [InlineData("+9021655544336677")]
    public void Reddedilir_E164DisiTelefon(string numara)
    {
        Result<PhoneNumber> sonuc = PhoneNumber.Create(numara);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(PhoneNumber.InvalidFormat, sonuc.Error.Code);
    }

    [Theory]
    [InlineData("+902165554433")]
    [InlineData("+96522334455")]
    public void KabulEdilir_E164Telefon(string numara)
    {
        Assert.True(PhoneNumber.Create(numara).IsSuccess);
    }

    [Theory(DisplayName = "0003/AC-10: Verilen e-posta geçerli biçimde değilse şube kaydedilemez")]
    [InlineData("sube")]
    [InlineData("sube@")]
    [InlineData("@galeri.com")]
    [InlineData("sube@galeri")]
    [InlineData("sube@galeri.")]
    [InlineData("iki@adres@galeri.com")]
    [InlineData("bosluk var@galeri.com")]
    public void Reddedilir_GecersizEposta(string adres)
    {
        Result<EmailAddress> sonuc = EmailAddress.Create(adres);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(EmailAddress.InvalidFormat, sonuc.Error.Code);
    }

    [Fact]
    public void KabulEdilir_GecerliEposta()
    {
        Assert.True(EmailAddress.Create("kadikoy@otogaleri.com.tr").IsSuccess);
    }

    [Theory(DisplayName = "0003/AC-12: Slug küçük harfli ASCII slug biçiminde değilse şube kaydedilemez")]
    [InlineData("Kadıköy-Şubesi")]
    [InlineData("Kadikoy-Subesi")]
    [InlineData("kadikoy subesi")]
    [InlineData("kadikoy_subesi")]
    [InlineData("-kadikoy")]
    [InlineData("kadikoy-")]
    [InlineData("kadikoy--subesi")]
    [InlineData("")]
    public void Reddedilir_KanonikOlmayanSlug(string slug)
    {
        Result<BranchSlug> sonuc = BranchSlug.Create(slug);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(BranchSlug.InvalidFormat, sonuc.Error.Code);
    }

    [Fact]
    public void KabulEdilir_KanonikSlug()
    {
        Assert.Equal("kadikoy-subesi", BranchSlug.Create("kadikoy-subesi").Value.Value);
    }

    [Theory(DisplayName = "0003/AC-13: Saat dilimi IANA bölge kimliği değilse şube kaydedilemez; +03:00 reddedilir, Europe/Istanbul kabul edilir")]
    [InlineData("+03:00")]
    [InlineData("03:00")]
    [InlineData("UTC")]
    [InlineData("GMT+3")]
    [InlineData("Europe/Atlantis")]
    [InlineData("")]
    public void Reddedilir_IanaOlmayanSaatDilimi(string bolge)
    {
        Result<TimeZoneId> sonuc = TimeZoneId.Create(bolge);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(TimeZoneId.InvalidZone, sonuc.Error.Code);
    }

    [Theory]
    [InlineData("Europe/Istanbul")]
    [InlineData("Asia/Kuwait")]
    public void KabulEdilir_IanaBolgeKimligi(string bolge)
    {
        Assert.True(TimeZoneId.Create(bolge).IsSuccess);
    }

    [Fact(DisplayName = "D8: tzdata kapısı — işletim sisteminde saat dilimi veritabanı yoksa geçerli bölge kimliği de reddedilir")]
    public void TzdataMevcut()
    {
        // Bu test doğrulamayı değil ORTAMI korur: tzdata eksikse TimeZoneId.Create hiçbir
        // bölgeyi kabul etmez ve AC-13'ün yarısı sessizce yanlış sebeple geçer.
        Assert.True(
            TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Istanbul", out _),
            "İşletim sisteminde tzdata bulunamadı; dağıtım imajına tzdata eklenmeli (D8).");
    }
}
