using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec 0007 — AC-8, AC-9, AC-11.
///
/// Mimari testler kaynak METNİ tarar, IL değil (bkz. `SolutionLayout`). Bu, kuralın ne
/// kadar keskin yazılabileceğini de belirliyor: "ham sayım ile yazmanın aynı komutta yan
/// yana gelmesi" bir regex'le kesin yakalanamaz. R5 bu yüzden iki KESİN parçaya bölündü —
/// kilidin nerede yaşayabileceği (AC-9) ve invariant kararını çağıran dosyanın ortak
/// desene bağlı olma zorunluluğu (AC-11). Ölçemediğini ölçüyormuş gibi yazan bir test,
/// hiç testi olmamasından kötüdür.
/// </summary>
public sealed partial class EsZamanlilikKuraliTests
{
    private const string GuardFolder = "src/BuildingBlocks/Concurrency/";

    private const string GuardCall = "InvariantGuard.ExecuteAsync";

    [Fact(DisplayName = "0007/AC-9: Ortak desen tek bir yerde yaşar; modüller onu çağırır, kendi kopyalarını yazmaz")]
    public void YasarTekYerde_SatirKilidi()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);

            if (relative.StartsWith(GuardFolder, StringComparison.Ordinal))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(relative);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Satır kilidi yalnız {GuardFolder} altında yazılır; modüller `InvariantGuard`'ı çağırır (0007 R3):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0007/AC-10: `Identity`'nin son aktif Yönetici koruması ortak deseni kullanır; modül içinde ayrı bir kopyası kalmaz")]
    public void KullanirOrtakDeseni_Identity()
    {
        string commands = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/Identity/Identity/Application/StaffCommands.cs"));

        string queries = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/Identity/Identity/Application/StaffQueries.cs"));

        Assert.Contains(GuardCall, commands, StringComparison.Ordinal);

        // Kendi transaction'ını açmıyor: tören ortak desende yaşıyor.
        Assert.DoesNotContain("BeginTransactionAsync", commands, StringComparison.Ordinal);

        // Kendi kilidini almıyor: yalnız tarifini veriyor.
        Assert.DoesNotContain("SqlQueryRaw", queries, StringComparison.Ordinal);
        Assert.Contains("InvariantLock", queries, StringComparison.Ordinal);
    }

    /// <summary>
    /// Invariant kararını veren domain metotları, "benden başka kaç tane aktif var"
    /// sayısını PARAMETRE olarak alır. O parametreyi dolduran çağrı, sayıyı ortak desenin
    /// ürettiği değerden almak zorundadır — aksi hâlde kilitsiz bir sayım kararı besler ve
    /// koruma yalnız tek kullanıcılı bir dünyada çalışır.
    ///
    /// Kural adlandırma konvansiyonuna dayanıyor (`docs/conventions.md` § 2); zayıf noktası
    /// budur ve mesaj bunu söyler.
    /// </summary>
    [Fact(DisplayName = "0007/AC-11: Invariant kararına giren sayı yalnızca ortak desenin ürettiği değer üzerinden elde edilir; modüllerin Application katmanında ham sayım ile yazmanın aynı komutta yan yana geldiği bir yol bulunmaz")]
    public void BulunmazHamSayim_InvariantKomutlarinda()
    {
        List<string> invariantMethods = [.. InvariantDomainMethods()];

        Assert.NotEmpty(invariantMethods);

        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);

            if (!relative.Contains("/Application/", StringComparison.Ordinal))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            bool callsInvariantDecision = invariantMethods.Exists(
                method => text.Contains($".{method}(", StringComparison.Ordinal));

            // Aranan şey ÇAĞRININ kendisi, tipin adı değil: `InvariantGuardOptions`
            // parametresi dosyada "InvariantGuard" alt dizesini zaten geçiriyor ve
            // yalnız ada bakan bir kontrol hiçbir zaman kırmızıya dönmezdi. Sonda bunu
            // yakaladı.
            if (callsInvariantDecision && !text.Contains(GuardCall, StringComparison.Ordinal))
            {
                violations.Add(relative);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Invariant kararı veren komut ortak deseni ({nameof(violations)}) atlıyor (0007 R5); sayı kilit altında üretilmeli:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0007/AC-8: Şube kapatma komutunun HTTP ucu yoktur; komut yalnızca modül içinden çağrılabilir")]
    public void YayinlanmazHttpUcu_SubeKapatmaKomutu()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);

            if (!relative.StartsWith("src/Modules/Dealership/", StringComparison.Ordinal))
            {
                continue;
            }

            if (EndpointMapping().IsMatch(File.ReadAllText(file)))
            {
                violations.Add(relative);
            }
        }

        // Host da onu kendi başına yayınlayamaz: komut `internal`dır ve `Contracts`'ta yoktur.
        string contracts = Path.Combine(SolutionLayout.SourceRoot, "Modules/Dealership/Dealership.Contracts");
        List<string> leaked =
        [
            .. Directory.EnumerateFiles(contracts, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("Close", StringComparison.Ordinal))
                .Select(SolutionLayout.Relative)
        ];

        Assert.True(
            violations.Count == 0 && leaked.Count == 0,
            $"0003 § D1 — Dealership'in korumasız yazma ucu olamaz (0007 D2):{Environment.NewLine}{string.Join(Environment.NewLine, [.. violations, .. leaked])}");
    }

    /// <summary>
    /// `InvariantLock` yüklemleri SABİTTEN gelir; dize interpolasyonu kullanıcı girdisinin
    /// SQL'e karışabileceği tek yoldur ve o yol kapalı tutulur. Bu bir kriter testi değil,
    /// desenin güvenlik kapısıdır — `CodeRuleTests`'teki diğer kurallar gibi.
    /// </summary>
    [Fact]
    public void KurulmazInterpolasyonla_InvariantLock()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            foreach (Match match in InvariantLockConstruction().Matches(File.ReadAllText(file)))
            {
                if (match.Value.Contains("$\"", StringComparison.Ordinal))
                {
                    violations.Add($"{SolutionLayout.Relative(file)}: {match.Value.Split('\n')[0].Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"InvariantLock yüklemi sabitten gelir; değerler parametredir (0007 C1 gerekçesi):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Domain'de "benden başka kaç tane aktif var" sayısını parametre olarak alan metotlar.
    /// Envanter elle tutulmuyor: yeni bir invariant eklendiğinde kural onu kendiliğinden
    /// kapsar — elle tutulan liste, unutulacak bir adım demekti.
    /// </summary>
    private static IEnumerable<string> InvariantDomainMethods()
    {
        foreach (string file in SolutionLayout.SourceFiles())
        {
            if (!SolutionLayout.Relative(file).Contains("/Domain/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match match in InvariantDecisionSignature().Matches(File.ReadAllText(file)))
            {
                yield return match.Groups["method"].Value;
            }
        }
    }

    [GeneratedRegex(@"\bResult\s+(?<method>\w+)\s*\([^)]*\bint\s+other[A-Z]\w*Count\b", RegexOptions.Singleline)]
    private static partial Regex InvariantDecisionSignature();

    [GeneratedRegex(@"\bMap(Get|Post|Put|Patch|Delete|Group)\s*\(")]
    private static partial Regex EndpointMapping();

    [GeneratedRegex(@"new\s+InvariantLock\s*\([^;]*;", RegexOptions.Singleline)]
    private static partial Regex InvariantLockConstruction();
}
