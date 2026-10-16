namespace CarGallery.Architecture.Tests;

/// <summary>
/// `docs/conventions.md` § 13 — bir üyenin üstünde **tek** bir doküman yorumu bulunur.
///
/// <para>Bu kapı bir süreç ölçümünün bedeli: aynı kusur — yeni bir üyeyi var olan bir
/// yorumla sahibi arasına eklemek — `0012a`, `0012b` ve `0012c`'de **dört kez** tekrarladı
/// ve her seferinde elle düzeltildi. Sonucu sessiz: yorum yanlış üyeyi anlatır, asıl üye
/// yorumsuz kalır ve derleyici hiçbir şey söylemez. Kusuru düzeltmek dönüşünü
/// engellemiyor; yalnızca kapı engelliyor.</para>
///
/// <para><b>Kapı `tests/` altını da tarıyor ve bu da ölçülmüş bir bedel:</b> ilk hâli
/// yalnız `SolutionLayout.SourceFiles()` çağırıyordu, yani yalnız `src/`. Kusur beşinci
/// kez `0013`'te döndü ve tam o klasörde — `tests/Identity.Tests` — oldu; kapı yeşil
/// kaldı, derleyici de söylemedi (`GenerateDocumentationFile=false`). Kural metin
/// kuralıdır: nerede yazıldığından bağımsız.</para>
/// </summary>
public sealed class YorumKapisiTests
{
    [Fact]
    public void Tasimaz_BirUye_BirdenFazlaDokumanYorumu()
    {
        List<string> violations = [];

        // Kapının kendi kapsamı da ölçülür: iki listeden biri boşalırsa kural yarısını
        // sessizce bırakır ve bunu hiç söylemezdi.
        IReadOnlyList<string> kaynak = SolutionLayout.SourceFiles();
        IReadOnlyList<string> testler = SolutionLayout.TestFiles();

        Assert.NotEmpty(kaynak);
        Assert.NotEmpty(testler);

        foreach (string file in kaynak.Concat(testler))
        {
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                if (!IsDocLine(lines[index]))
                {
                    continue;
                }

                int start = index;
                int summaries = 0;

                // Bir üyeye bağlı yorum "bloğu": ardışık `///` satırları ve bunları
                // yalnızca BOŞ satırlarla ayıran sonraki `///` satırları. Araya boş satır
                // koymak yorumu üyeden ayırmıyor — öksüz yorum tam böyle görünüyor.
                while (index < lines.Length)
                {
                    if (IsDocLine(lines[index]))
                    {
                        summaries += Occurrences(lines[index], "<summary>");
                        index++;
                        continue;
                    }

                    if (lines[index].Trim().Length == 0)
                    {
                        int lookahead = index;
                        while (lookahead < lines.Length && lines[lookahead].Trim().Length == 0)
                        {
                            lookahead++;
                        }

                        if (lookahead < lines.Length && IsDocLine(lines[lookahead]))
                        {
                            index = lookahead;
                            continue;
                        }
                    }

                    break;
                }

                if (summaries > 1)
                {
                    violations.Add(
                        $"{SolutionLayout.Relative(file)}:{start + 1}  tek üyenin üstünde {summaries} <summary>");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Bir üyenin üstünde tek doküman yorumu bulunur; ikinci bir <summary>, bir üyenin yorumunu "
                + $"öksüz bıraktığı anlamına gelir (docs/conventions.md § 13):{Environment.NewLine}"
                + string.Join(Environment.NewLine, violations));
    }

    private static bool IsDocLine(string line) =>
        line.TrimStart().StartsWith("///", StringComparison.Ordinal);

    private static int Occurrences(string line, string needle)
    {
        int count = 0;
        int position = line.IndexOf(needle, StringComparison.Ordinal);

        while (position >= 0)
        {
            count++;
            position = line.IndexOf(needle, position + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
