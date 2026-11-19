using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Domain;

namespace CarGallery.Modules.Content.Tests.Domain;

/// <summary>`0022` D11 — performans tohumu her alanı sınırında ve açıklamaları satır sonlu doldurur.</summary>
public sealed class AnaSayfaTohumuTests
{
    [Fact(DisplayName = "0022/AC-21: Her alanı sınırında (120/500, satır sonlu) kaydedilmiş metinlerle ana sayfa için performans kapısı (LCP < 2,5 sn, CLS < 0,1) yeşildir (D11).")]
    public void Doldurur_HerAlaniSinirinda()
    {
        IReadOnlyList<HomeTextInput> girdiler = HomeTextDevelopmentSeed.Inputs(["tr", "ar", "en"]);

        Assert.Equal(["tr", "ar", "en"], girdiler.Select(item => item.Locale));
        Assert.All(girdiler, girdi => Assert.All(HomeTextField.All, alan =>
        {
            string deger = girdi.Values[alan.Key]!;
            Assert.Equal(alan.MaxLength, ContentRules.Length(deger));
            Assert.Equal(alan.IsDescription, deger.Contains('\n', StringComparison.Ordinal));
        }));
    }
}
