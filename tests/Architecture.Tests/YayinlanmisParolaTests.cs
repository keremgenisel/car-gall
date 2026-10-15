using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// ADR 0011 — depoda SABİT bir kurulum parolası bulunmaz.
///
/// <para>Depo herkese açık. Sabit bir kurulum parolası, git geçmişinde sonsuza kadar
/// kalan ve parolası kamuya açık bir Yönetici hesabı üretebilen bir dizedir; üç tanesi
/// bu depoda bir süre durdu ve `IdentityModule` onları artık reddediyor. Bu kapı aynı
/// kusurun YENİDEN doğmasını engelliyor: değer koşuyu başlatandan gelir, depodan
/// gelmez.</para>
/// </summary>
public sealed partial class YayinlanmisParolaTests
{
    [Fact]
    public void Tasimaz_Depo_SabitKurulumParolasi()
    {
        List<string> violations = [];

        foreach (string file in DenetlenenDosyalar())
        {
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                Match match = ParolaAtamasi().Match(lines[index]);
                if (!match.Success)
                {
                    continue;
                }

                string value = match.Groups["deger"].Value.Trim().Trim('"', '\'');

                // Değer bir DEĞİŞKENDEN geliyorsa sorun yok: `$VAR`, `${VAR}`, `$(komut)`,
                // `${{ env.VAR }}` ve TypeScript'in `process.env.X` biçimleri serbest.
                if (value.Length == 0
                    || value.StartsWith('$')
                    || value.Contains("${", StringComparison.Ordinal)
                    || value.Contains("process.env", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add($"{SolutionLayout.Relative(file)}:{index + 1}  {lines[index].Trim()}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Depoda sabit kurulum parolası bulunmaz; değer koşuyu başlatandan gelir "
                + $"(ADR 0011):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Kurulum parolasının geçtiği dosya türleri. `src/**/*.cs` BİLEREK dışarıda:
    /// `IdentityModule`'ün reddettiği eski dizelerin listesi orada yaşıyor ve kapı kendi
    /// savunmasını ihlal saymamalı.
    /// </summary>
    private static IEnumerable<string> DenetlenenDosyalar()
    {
        (string Klasor, string Desen, SearchOption Derinlik)[] kaynaklar =
        [
            ("scripts", "*.sh", SearchOption.TopDirectoryOnly),
            (Path.Combine(".github", "workflows"), "*.yml", SearchOption.TopDirectoryOnly),
            (Path.Combine("frontend", "e2e"), "*.ts", SearchOption.AllDirectories),
            ("src", "appsettings*.json", SearchOption.AllDirectories),
        ];

        foreach ((string klasor, string desen, SearchOption derinlik) in kaynaklar)
        {
            string tam = Path.Combine(SolutionLayout.Root, klasor);
            if (!Directory.Exists(tam))
            {
                continue;
            }

            foreach (string dosya in Directory.EnumerateFiles(tam, desen, derinlik).Order())
            {
                yield return dosya;
            }
        }
    }

    // `Identity__SeedAdmin__Password=...`, `Identity:SeedAdmin:Password": "..."`,
    // `SEED_ADMIN_PASSWORD: ...` ve `E2E_SEED_ADMIN_PASSWORD = ...` biçimlerini yakalar.
    [GeneratedRegex(
        @"(?:Identity__SeedAdmin__Password|Identity:SeedAdmin:Password|(?:E2E_)?SEED_ADMIN_PASSWORD)""?\s*[:=]\s*(?<deger>[^\\\r\n,]*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ParolaAtamasi();
}
