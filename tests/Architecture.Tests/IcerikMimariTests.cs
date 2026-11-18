using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>`0017` — `Content` modülünün sınırları ve saklama kuralları (C1, C2, C9, D25).</summary>
public sealed class IcerikMimariTests
{
    private static IEnumerable<string> ContentKaynaklari() =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.Relative(file).StartsWith("src/Modules/Content/", StringComparison.Ordinal));

    [Fact(DisplayName = "0017/AC-113: Content'te rol kodu (content_editor, admin, sales_advisor, manager) karşılaştıran kod yoktur; rol kısıtı Identity'nin politikasından gelir")]
    public void Karsilastirmaz_RolKodu()
    {
        // Kurulumun rol kodları `manager`, `sales_advisor`, `content_editor` (Identity'nin
        // `StaffRoleCode`'u). Kriterin saydığı `admin` bir rol kodu DEĞİL; Content'te o dize
        // yalnız ayrılmış slug olarak geçer (C11) ve rol karşılaştırması sayılmaz. Rol
        // karşılaştırmasının ikinci biçimi API'dir: `IsInRole`, `RequireRole`, `ClaimTypes.Role`.
        Regex rolKodu = new(
            "\"(content_editor|sales_advisor|manager)\"|IsInRole\\(|RequireRole\\(|ClaimTypes\\.Role",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));

        List<string> ihlaller =
        [
            .. ContentKaynaklari()
                .Where(file => !SolutionLayout.Relative(file).Contains("/Migrations/", StringComparison.Ordinal))
                .SelectMany(file => SolutionLayout.CodeLines(file).Where(line => rolKodu.IsMatch(line)).Select(line => $"{SolutionLayout.Relative(file)}: {line.Trim()}")),
        ];

        Assert.NotEmpty(ContentKaynaklari());
        Assert.Empty(ihlaller);
        Assert.Contains(
            ContentKaynaklari(),
            file => File.ReadAllText(file).Contains("AdminSurface.ContentPolicy", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0017/AC-129: content şemasına Content dışında yazan kod yoktur; diğer modüller Content'e yalnız Content.Contracts üzerinden bağlanır")]
    public void Erisir_YalnizSozlesmeUzerinden()
    {
        Regex sema = SolutionLayout.SchemaReference("content");

        List<string> semaIhlali =
        [
            .. SolutionLayout.SourceFiles()
                .Where(file => !SolutionLayout.Relative(file).StartsWith("src/Modules/Content/", StringComparison.Ordinal))
                .SelectMany(file => SolutionLayout.CodeLines(file).Where(line => sema.IsMatch(line)).Select(line => $"{SolutionLayout.Relative(file)}: {line.Trim()}")),
        ];

        List<string> referansIhlali =
        [
            .. SolutionLayout.ProjectFiles()
                .Where(SolutionLayout.IsModuleProject)
                .Where(project => SolutionLayout.ModuleNameOf(project) != "Content")
                .SelectMany(project => SolutionLayout.ProjectReferences(project)
                    .Where(reference => SolutionLayout.ModuleNameOf(reference) == "Content" && !SolutionLayout.IsContractsProject(reference))
                    .Select(reference => $"{SolutionLayout.Relative(project)} → {SolutionLayout.Relative(reference)}")),
        ];

        Assert.Empty(semaIhlali);
        Assert.Empty(referansIhlali);
    }

    [Fact(DisplayName = "0017/AC-130: hiçbir migration translations adlı bir tablo oluşturmaz; içerik metni yalnız content şemasında saklanır")]
    public void Kurmaz_MerkeziCeviriTablosu()
    {
        Regex ceviriTablosu = new(@"name:\s*""translations""", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        List<string> migrationlar = [.. SolutionLayout.SourceFiles().Where(file => SolutionLayout.Relative(file).Contains("/Migrations/", StringComparison.Ordinal))];
        Assert.NotEmpty(migrationlar);
        Assert.DoesNotContain(migrationlar, file => ceviriTablosu.IsMatch(File.ReadAllText(file)));

        string icerik = File.ReadAllText(migrationlar.Single(file => file.EndsWith("_InitialContent.cs", StringComparison.Ordinal)));
        Assert.Contains("name: \"page_localizations\"", icerik, StringComparison.Ordinal);
        Assert.DoesNotContain("schema: \"vehicle_catalog\"", icerik, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0017/AC-132: content migration'ı yalnız content şemasına dokunur")]
    public void Dokunur_YalnizKendiSemasina()
    {
        string icerik = File.ReadAllText(SolutionLayout.SourceFiles().Single(file => file.EndsWith("_InitialContent.cs", StringComparison.Ordinal)));

        List<string> semalar = [.. Regex.Matches(icerik, "schema: \"(?<s>[a-z_]+)\"").Select(match => match.Groups["s"].Value).Distinct()];
        Assert.Equal(["content"], semalar);
    }
}
