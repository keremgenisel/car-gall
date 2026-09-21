using System.Text;
using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// docs/architecture.md § 5: "modül X'in migration'ı yalnızca x şemasına dokunabilir."
/// Bir modülün başka modülün şemasına dokunan migration'ı, sınırı veritabanı seviyesinde
/// deler ve geri alması en pahalı ihlal biçimidir.
/// </summary>
public sealed partial class MigrationSchemaTests
{
    [Fact]
    public void DokunurYalnizKendiSemasina_ModulMigrationlari()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);
            if (!relative.Contains("/Migrations/", StringComparison.Ordinal))
            {
                continue;
            }

            string expectedSchema = ToSnakeCase(SolutionLayout.ModuleNameOf(file));
            if (expectedSchema.Length == 0)
            {
                continue;
            }

            foreach (Match match in SchemaReference().Matches(File.ReadAllText(file)))
            {
                string schema = match.Groups["schema"].Value;
                if (schema != expectedSchema)
                {
                    violations.Add($"{relative}: '{schema}' (beklenen: '{expectedSchema}')");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Migration yalnız kendi şemasına dokunabilir:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void MigrationTasir_HerDbContextSahibiModul()
    {
        List<string> missing = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string fileName = Path.GetFileName(file);

            if (!fileName.EndsWith("DbContext.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // Kural SOMUT modül DbContext'lerini hedefler. Arayüz dosyaları (IOutboxDbContext)
            // ve BuildingBlocks'taki altyapı tipleri migration taşımaz — tespit yalnız dosya
            // adına bakarsa onları da modül sanır ve kural yanlış yerde kırmızı verir.
            if (fileName.Length > 1 && fileName[0] == 'I' && char.IsUpper(fileName[1]))
            {
                continue;
            }

            if (!SolutionLayout.IsModuleProject(file) && !SolutionLayout.Relative(file).StartsWith("src/Modules/", StringComparison.Ordinal))
            {
                continue;
            }

            string moduleRoot = Path.GetDirectoryName(Path.GetDirectoryName(file))!;
            if (!Directory.Exists(Path.Combine(moduleRoot, "Infrastructure", "Migrations")))
            {
                missing.Add(SolutionLayout.Relative(file));
            }
        }

        Assert.True(
            missing.Count == 0,
            $"DbContext taşıyan modülün migration zinciri olmalıdır:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static string ToSnakeCase(string pascalCase)
    {
        StringBuilder builder = new();

        foreach (char character in pascalCase)
        {
            if (char.IsUpper(character) && builder.Length > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"schema:\s*""(?<schema>[a-z_]+)""")]
    private static partial Regex SchemaReference();
}
