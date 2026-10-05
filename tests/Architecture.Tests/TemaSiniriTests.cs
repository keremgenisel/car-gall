namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec frontend-002 — AC-14.
///
/// <para>C4 "ikinci bir mekanizma kurulmaz" diyor ve bu, tek bir yanıta bakmakla
/// kanıtlanamaz: ikinci bir enjeksiyon yolu eklendiğinde ilk yolun testleri yeşil kalır.
/// Kanıt kaynak taramasıdır — <c>index.html</c>'i okuyan tek bir yer olmalı.</para>
/// </summary>
public sealed class TemaSiniriTests
{
    [Fact(DisplayName = "frontend-002/AC-14: Tema değerleri mevcut `index.html` enjeksiyon yolundan gelir; ikinci bir mekanizma eklenmez")]
    public void Okur_YalnizBirYer_IndexHtmli()
    {
        List<string> yerler = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);

            if (relative.EndsWith("Localization/IndexHtmlRenderer.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (lines[index].Contains("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    yerler.Add($"{relative}:{index + 1}  {lines[index].Trim()}");
                }
            }
        }

        Assert.True(
            yerler.Count == 0,
            $"index.html'i okuyan tek yer IndexHtmlRenderer'dır; tema oradan enjekte edilir (C4):{Environment.NewLine}{string.Join(Environment.NewLine, yerler)}");
    }

    /// <summary>
    /// Tarama hiç dosya bulamazsa da yeşil kalacağı için, aradığı şeyin GERÇEKTEN var
    /// olduğunu da doğrulamak zorunda — 0006'nın dersi buydu: "sayılamayan bir ifade
    /// hiçbir testi kırmaz".
    /// </summary>
    [Fact]
    public void Bulur_EnjeksiyonYolunu()
    {
        string? renderer = SolutionLayout.SourceFiles()
            .FirstOrDefault(file => SolutionLayout.Relative(file)
                .EndsWith("Localization/IndexHtmlRenderer.cs", StringComparison.Ordinal));

        Assert.NotNull(renderer);
        Assert.Contains("index.html", File.ReadAllText(renderer), StringComparison.Ordinal);
    }
}
