using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>docs/architecture.md § 5 — 26, 30 ve docs/conventions.md § 5, § 7, § 8.</summary>
public sealed partial class CodeRuleTests
{
    [Fact]
    public void ParaAlaniTanimlanmaz_DoubleVeyaFloatIle()
    {
        List<string> violations = Scan(DoubleOrFloatDeclaration(), _ => true);

        Assert.True(
            violations.Count == 0,
            $"Para ve yüzde her zaman decimal'dir, istisnasız (Yasak Listesi 26):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void DogrudanKullanilmaz_SistemSaati()
    {
        List<string> violations = Scan(
            SystemClockAccess(),
            relative => !relative.EndsWith("BuildingBlocks/IClock.cs", StringComparison.Ordinal));

        Assert.True(
            violations.Count == 0,
            $"Saat enjekte edilir; IClock dışında sistem saatine erişilmez (Yasak Listesi 30):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void YutulmazIstisna_BosCatchBlogu()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            if (EmptyCatchBlock().IsMatch(File.ReadAllText(file)))
            {
                violations.Add(SolutionLayout.Relative(file));
            }
        }

        Assert.True(
            violations.Count == 0,
            $"catch (Exception) {{ }} yasaktır; yutulan istisna yoktur (docs/conventions.md § 5):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void BirakilmazTodoIsareti()
    {
        List<string> violations = Scan(TodoMarker(), _ => true);

        Assert.True(
            violations.Count == 0,
            $"TODO yazılmaz; yapılacak iş spec'e girer (docs/conventions.md § 13):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void DomainKatmaniBilmez_EfCoreHttpVeYapilandirmayi()
    {
        List<string> violations = Scan(
            InfrastructureUsing(),
            relative => relative.Contains("/Domain/", StringComparison.Ordinal));

        Assert.True(
            violations.Count == 0,
            $"Domain EF Core'u, HTTP'yi, IConfiguration'ı bilmez (docs/conventions.md § 4):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static List<string> Scan(Regex pattern, Func<string, bool> fileFilter)
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);
            if (!fileFilter(relative))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (pattern.IsMatch(line))
                {
                    violations.Add($"{relative}:{index + 1}  {line.Trim()}");
                }
            }
        }

        return violations;
    }

    // Bildirim arar, dönüşüm değil: "(double)PageSize" eşleşmez, "double oran = …" eşleşir.
    [GeneratedRegex(@"\b(?:double|float)\s+[A-Za-z_]\w*\s*(?:\{|;|=|,|\))")]
    private static partial Regex DoubleOrFloatDeclaration();

    [GeneratedRegex(@"\b(?:DateTime|DateTimeOffset)\s*\.\s*(?:Now|UtcNow|Today)\b")]
    private static partial Regex SystemClockAccess();

    [GeneratedRegex(@"catch\s*\([^)]*\)\s*\{\s*\}", RegexOptions.Singleline)]
    private static partial Regex EmptyCatchBlock();

    [GeneratedRegex(@"\bTODO\b")]
    private static partial Regex TodoMarker();

    [GeneratedRegex(@"^\s*using\s+(?:Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|Microsoft\.Extensions\.Configuration|System\.Net\.Http)\b")]
    private static partial Regex InfrastructureUsing();
}
