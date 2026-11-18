using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// `0015` QA BULGU-2 — kriter turu bir spec'e kabul kriterlerini eklediğinde, taslak
/// döneminden kalan "kabul kriterleri yazılmadı" notu kalmamalı. Kalırsa spec'i okuyan
/// bir sonraki oturum (Analist, QA) o cümleye güvenip yanlış iş açar.
///
/// <para>Kural dar ve ölçülebilir: kriter TAŞIYAN bir spec o cümleyi taşımaz. Kriteri olmayan
/// taslaklarda cümle doğrudur ve kapı onlara dokunmaz. `specs/` ve `specs/done/` ikisi de
/// taranır: kart kapanınca yolu değişir ve kapı onu kaybetmemeli.</para>
/// </summary>
public sealed class SpecDurumNotuTests
{
    private static readonly Regex KriterSatiri = new(@"^- \*\*AC-\d+", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex EskiTaslakNotu = new("kabul kriterleri yazılmadı", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact(DisplayName = "Spec kapısı: kabul kriteri taşıyan bir spec 'kabul kriterleri yazılmadı' demez")]
    public void Demez_KriterleriYazilmadi_KriterVarken()
    {
        string[] dizinler = [Path.Combine(SolutionLayout.Root, "specs"), Path.Combine(SolutionLayout.Root, "specs", "done")];
        List<string> ihlaller = [];

        foreach (string yol in dizinler.SelectMany(dizin => Directory.GetFiles(dizin, "*.md")))
        {
            if (Path.GetFileName(yol) == "TEMPLATE.md")
            {
                continue;
            }

            string metin = File.ReadAllText(yol);
            if (KriterSatiri.IsMatch(metin) && EskiTaslakNotu.IsMatch(metin))
            {
                ihlaller.Add(Path.GetRelativePath(SolutionLayout.Root, yol));
            }
        }

        Assert.True(dizinler.All(Directory.Exists), "specs/ veya specs/done/ bulunamadı — kapı hiçbir şey taramıyor olurdu.");
        Assert.Empty(ihlaller);
    }
}
