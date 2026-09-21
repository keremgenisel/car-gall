using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>docs/architecture.md § 5 — "X.Contracts dışında hiçbir şey public değildir."</summary>
public sealed partial class PublicSurfaceTests
{
    [Fact]
    public void PublicTipBulunmaz_ModulunIcProjesinde()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);
            if (!relative.StartsWith("src/Modules/", StringComparison.Ordinal) || IsInsideContractsProject(relative))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int index = 0; index < lines.Length; index++)
            {
                if (!TopLevelPublicType().IsMatch(lines[index]))
                {
                    continue;
                }

                if (IsModuleEntryPoint(relative, lines[index]))
                {
                    continue;
                }

                violations.Add($"{relative}:{index + 1}  {lines[index].Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Varsayılan erişim internal'dır; public yüzey yalnız X.Contracts'tır:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void RecordOlarakTanimlanir_EntegrasyonOlaylari()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);
            if (!relative.Contains("/Events/", StringComparison.Ordinal) || !IsInsideContractsProject(relative))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (!text.Contains("record ", StringComparison.Ordinal))
            {
                violations.Add(relative);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Entegrasyon olayı record olmalıdır (docs/architecture.md § 2.B):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool IsInsideContractsProject(string relativePath) =>
        relativePath.Contains(".Contracts/", StringComparison.Ordinal);

    /// <summary>
    /// Tek istisna: modülün DI ve endpoint kaydını yapan giriş noktası. Host onu
    /// çağıramazsa modül kaydedilemez (docs/conventions.md § 1, § 3). İstisna DAR tutulur —
    /// dosya adı, tip adı ve konum birebir eşleşmek zorundadır; başka her public tip ihlaldir.
    ///
    /// AP-10: docs/architecture.md § 5 "Contracts dışında hiçbir şey public değildir" ile
    /// docs/conventions.md § 1'in MapXModule() çağrısı çelişiyor. Bu okuma Takım
    /// Yöneticisi onayı bekliyor.
    /// </summary>
    private static bool IsModuleEntryPoint(string relativePath, string line)
    {
        string fileName = Path.GetFileNameWithoutExtension(relativePath);
        if (!fileName.EndsWith("Module", StringComparison.Ordinal))
        {
            return false;
        }

        string[] segments = relativePath.Split('/');
        bool sitsAtModuleRoot = segments.Length == 5 && segments[3] == segments[2];

        return sitsAtModuleRoot && line.Contains($"class {fileName}", StringComparison.Ordinal);
    }

    // Dosya kapsamlı namespace kullandığımız için üst düzey tipler 0. sütundan başlar;
    // girintili "public" bir üyedir, tip değildir.
    [GeneratedRegex(@"^public\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+|readonly\s+|ref\s+)*(?:class|record|interface|enum|struct|delegate)\b")]
    private static partial Regex TopLevelPublicType();
}
