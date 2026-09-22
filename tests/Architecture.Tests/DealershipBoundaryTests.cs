using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>spec 0003 — AC-33 ve AC-35.</summary>
public sealed partial class DealershipBoundaryTests
{
    [Fact(DisplayName = "0003/AC-33: Dealership dışındaki hiçbir modül galeri veya şube tablosuna erişmez; mimari test bu ihlali kırar")]
    public void ErisilmezDisaridan_GaleriVeSubeTablolari()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);

            if (!relative.StartsWith("src/Modules/", StringComparison.Ordinal)
                || SolutionLayout.ModuleNameOf(file) == "Dealership")
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (string table in new[] { "dealership.galleries", "dealership.branches", "\"branches\"", "\"galleries\"" })
            {
                if (text.Contains(table, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: '{table}'");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Galeri ve şube verisinin tek yazma sahibi Dealership'tir (Yasak Listesi 1):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0003/AC-35: Mimari test, kod tabanında sabit hafta sonu günü adı, sabit çalışma günü sayısı ve varsayılan açılış/kapanış saati sabiti bulunmadığını doğrular")]
    public void BulunmazSabit_HaftaSonuVeMesaiVarsayimi()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            string relative = SolutionLayout.Relative(file);
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];

                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (WeekendAssumption().IsMatch(line) || WorkingHourAssumption().IsMatch(line))
                {
                    violations.Add($"{relative}:{index + 1}  {line.Trim()}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Hafta sonu günü, çalışma günü sayısı veya açılış/kapanış saati varsayılmaz (Yasak Listesi 34):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    // DayOfWeek.Saturday/Sunday karşılaştırması ve "hafta sonu" adlı sabitler.
    [GeneratedRegex(@"DayOfWeek\s*\.\s*(Saturday|Sunday)|\b(Weekend|HaftaSonu)\b")]
    private static partial Regex WeekendAssumption();

    // "09:00" gibi gömülü mesai saatleri ve OpeningHour/ClosingHour sabitleri.
    [GeneratedRegex(@"\b(OpeningHour|ClosingHour|WorkingDays|MesaiBaslangic|MesaiBitis)\b|""(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]""")]
    private static partial Regex WorkingHourAssumption();
}
