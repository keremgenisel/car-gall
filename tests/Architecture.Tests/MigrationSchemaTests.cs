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
    /// <summary>
    /// Kapı GENEL ve `0013`'ten eskidir; etiketi `0013`/AC-60 taşıyor çünkü o kriterin
    /// iddiası ("kayıt tablosu yalnız `identity` şemasına dokunur") bu kapının ölçtüğü
    /// şeyin ta kendisi ve dört yeni tablonun hepsi aynı şemada. Etiket, *"bu kriterin
    /// testi var mı"* sorusunu tek bir `grep` ile cevaplanabilir kılıyor
    /// (`docs/testing.md` § 1); kapının kapsamını daraltmıyor.
    /// </summary>
    [Fact(DisplayName = "0013/AC-60: Modül migration'ı yalnız kendi şemasına dokunur")]
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

    /// <summary>
    /// Migration zincirinin VAR olması yetmez; birinin onu UYGULAMASI gerekir.
    ///
    /// İki kaçan hata bu boşluktan çıktı (`specs/done/SCORECARD-ANALIZ.md` § Bulgu 3):
    /// `Dealership`'in `MigrateDealershipAsync`'i tanımlıydı ama hiçbir yerden
    /// çağrılmıyordu; `VehicleCatalog` ve `VehicleReference` şemaları yalnız bir
    /// geliştirme bayrağıyla uygulanıyordu. İkisi de var olan her testi geçiyordu,
    /// çünkü hiçbir test "bu yol gerçekten koşuluyor mu" diye sormuyordu.
    ///
    /// Tespit BİLEREK dar: yalnız somut modül `DbContext`'leri ve yalnız
    /// `ModuleSchema&lt;TContext&gt;` kaydı aranır. `0003`'ün regresyonu, fazla geniş bir
    /// tespitin arayüz dosyalarını da modül sanmasından doğmuştu.
    /// </summary>
    [Fact(DisplayName = "Kurulum adımına kayıtsız modül DbContext'i bulunamaz")]
    public void KurulumAdiminaKayitlidir_HerModulDbContexti()
    {
        List<string> unregistered = [];
        IReadOnlyList<string> sources = SolutionLayout.SourceFiles();

        foreach (string file in ModuleDbContextFiles(sources))
        {
            string contextType = Path.GetFileNameWithoutExtension(file);
            string moduleRoot = Path.GetDirectoryName(Path.GetDirectoryName(file))!;
            string kayit = $"ModuleSchema<{contextType}>";

            bool registered = sources
                .Where(candidate => candidate.StartsWith(moduleRoot, StringComparison.Ordinal))
                .Any(candidate => File.ReadAllText(candidate).Contains(kayit, StringComparison.Ordinal));

            if (!registered)
            {
                unregistered.Add($"{SolutionLayout.Relative(file)} → {kayit} kaydı yok");
            }
        }

        Assert.True(
            unregistered.Count == 0,
            "Migration zinciri olan her modül IModuleSchema kaydetmelidir; aksi hâlde kurulum "
            + "adımı o şemayı hiç uygulamaz ve eksiklik yalnız ilk sorguda görünür "
            + $"(docs/decisions/0002):{Environment.NewLine}{string.Join(Environment.NewLine, unregistered)}");
    }

    /// <summary>
    /// Somut modül `DbContext` dosyaları. Arayüz dosyaları (`IOutboxDbContext`) ve
    /// `BuildingBlocks` altyapısı hariç — ikisi de migration taşımaz.
    /// </summary>
    private static IEnumerable<string> ModuleDbContextFiles(IReadOnlyList<string> sources)
    {
        foreach (string file in sources)
        {
            string fileName = Path.GetFileName(file);

            if (!fileName.EndsWith("DbContext.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (fileName.Length > 1 && fileName[0] == 'I' && char.IsUpper(fileName[1]))
            {
                continue;
            }

            if (!SolutionLayout.IsModuleProject(file)
                && !SolutionLayout.Relative(file).StartsWith("src/Modules/", StringComparison.Ordinal))
            {
                continue;
            }

            yield return file;
        }
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
