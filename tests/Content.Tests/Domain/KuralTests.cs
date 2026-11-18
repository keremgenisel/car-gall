using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Domain;

namespace CarGallery.Modules.Content.Tests.Domain;

/// <summary>`0017` C11, C12, R13, R17, D32 — alan kurallarının tek yeri (<c>ContentRules</c>).</summary>
public sealed class KuralTests
{
    [Theory(DisplayName = "0017/AC-43: Şu slug'lar kabul edilir: ab, kvkk, kvkk-aydinlatma-metni, sss-2026 ve 80 karakterlik bir slug")]
    [InlineData("ab")]
    [InlineData("kvkk")]
    [InlineData("kvkk-aydinlatma-metni")]
    [InlineData("sss-2026")]
    [InlineData("a234567890b234567890c234567890d234567890e234567890f234567890g234567890h234567890")]
    public void KabulEder_GecerliSluglari(string slug)
    {
        Assert.Equal(80, "a234567890b234567890c234567890d234567890e234567890f234567890g234567890h234567890".Length);
        Assert.Null(ContentRules.SlugError(slug));
    }

    [Theory(DisplayName = "0017/AC-44: Şu slug'ların her biri reddedilir ve slug alanı için errorCode döner")]
    [InlineData("a")]
    [InlineData("a234567890b234567890c234567890d234567890e234567890f234567890g234567890h2345678901")]
    [InlineData("Hakkimizda")]
    [InlineData("hakkımızda")]
    [InlineData("من-نحن")]
    [InlineData("-kvkk")]
    [InlineData("kvkk-")]
    [InlineData("kvkk--metni")]
    [InlineData("kvkk_metni")]
    [InlineData("kvkk metni")]
    public void Reddeder_GecersizSluglari(string slug) =>
        Assert.Equal(ContentErrors.SlugInvalid, ContentRules.SlugError(slug));

    [Theory(DisplayName = "0017/AC-49: cars, admin, api ve assets slug'ları reddedilir; cars-ve-biz kabul edilir")]
    [InlineData("cars")]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("assets")]
    public void Reddeder_AyrilmisSluglari(string slug)
    {
        Assert.Equal(ContentErrors.SlugReserved, ContentRules.SlugError(slug));
        Assert.Null(ContentRules.SlugError("cars-ve-biz"));
    }

    [Fact(DisplayName = "0017/AC-50: Ayrılmış slug listesi cars, admin, api, assets'i içerir")]
    public void Icerir_AyrilmisListe() =>
        Assert.Equal(["cars", "admin", "api", "assets"], ContentReservedSlugs.All);

    [Fact(DisplayName = "0017/AC-10 (kural yarısı): yalnız boşluktan oluşan değer boştur (S9)")]
    public void Bostur_YalnizBosluk()
    {
        Assert.Equal(string.Empty, ContentRules.Normalize("   "));
        Assert.Equal(string.Empty, ContentRules.Normalize("\t\n "));
        Assert.Equal(string.Empty, ContentRules.Normalize(null));
        Assert.Equal(" a ", ContentRules.Normalize(" a "));
    }

    [Fact(DisplayName = "0017/AC-115 (kural yarısı): uzunluk bayt değil karakterdir — 120 Arapça harf 120 sayılır")]
    public void Sayar_Karakteri()
    {
        string arapca = new('ب', 120);
        Assert.Equal(120, ContentRules.Length(arapca));
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(arapca) > 120);
    }

    [Theory(DisplayName = "0017/AC-70 (kural yarısı): izinsiz şemalı bağlantı reddedilir; büyük harf ve baştaki boşluk kaçış değildir")]
    [InlineData("[tıkla](javascript:alert(1))")]
    [InlineData("[tıkla](JavaScript:alert(1))")]
    [InlineData("[tıkla](data:text/html,x)")]
    [InlineData("[tıkla](vbscript:x)")]
    [InlineData("[tıkla]( javascript:alert(1))")]
    [InlineData("[tıkla](<javascript:alert(1)>)")]
    [InlineData("[tıkla](java\tscript:alert(1))")]
    public void Reddeder_IzinsizSemayi(string govde) =>
        Assert.Equal(ContentErrors.LinkSchemeNotAllowed, ContentRules.LinkError(govde));

    [Theory(DisplayName = "0017/AC-71 (kural yarısı): http, https, mailto, tel ve göreli bağlantılar kabul edilir")]
    [InlineData("[a](http://example.com)")]
    [InlineData("[a](https://example.com)")]
    [InlineData("[a](mailto:info@example.com)")]
    [InlineData("[a](tel:+905551112233)")]
    [InlineData("[a](/tr/cars)")]
    public void KabulEder_IzinliSemayi(string govde) => Assert.Null(ContentRules.LinkError(govde));

    [Fact(DisplayName = "0017/AC-68 (kural yarısı): ham HTML kural tarafından reddedilmez")]
    public void Reddetmez_HamHtml()
    {
        Assert.Null(ContentRules.LinkError("<b>kalın</b>"));
        Assert.Null(ContentRules.LinkError("<script>alert(1)</script>"));
    }
}
