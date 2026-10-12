using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec `0012c` — **C2**: toplu eylem tekil komutların ÜZERİNE kurulur ve durum geçişi
/// mantığını tekrarlamaz.
///
/// <para>Bu kural kod gözden geçirmesiyle korunamaz. Toplu bir yolun "hızlı" hâli her
/// zaman caziptir: agregatı atlayıp `ExecuteUpdate` ile yirmi satırı tek sorguda
/// güncellemek. O gün A3'ün geçiş tablosu iki yerde yaşamaya başlar ve ikisi ayrı ayrı
/// yanlış olabilir — `0010b`'nin bilinen tuzağı 3.</para>
/// </summary>
public sealed partial class TopluEylemSiniriTests
{
    private const string TopluKomut = "ListingBulkCommands.cs";

    [Fact(DisplayName = "0012c/AC-20: Toplu eylem durum geçişi mantığını **tekrarlamaz**; karar agregatta kalır")]
    public void Tekrarlamaz_GecisMantigini()
    {
        string kaynak = KomutKaynagi();

        // Agregatın geçiş metotları toplu komutta ÇAĞRILMAZ: tekil komut çağırır.
        MatchCollection ihlaller = AggregateTransition().Matches(kaynak);

        Assert.True(
            ihlaller.Count == 0,
            $"{TopluKomut} agregatın geçiş metodunu doğrudan çağırıyor (C2): "
            + string.Join(", ", ihlaller.Select(ihlal => ihlal.Value)));

        // Toplu yol satır başına TEKİL komutlardan geçiyor; ikisi de adıyla aranıyor
        // çünkü yalnız birini çağıran bir toplu yol, öne çıkarmayı ya da geçişleri
        // kendi içinde yeniden yazmış demektir.
        Assert.Contains("transitions.ExecuteAsync", kaynak, StringComparison.Ordinal);
        Assert.Contains("features.ExecuteAsync", kaynak, StringComparison.Ordinal);
    }

    /// <summary>
    /// Toplu yol, satır başına yazmayı toplu bir SQL'e çevirmez: `ExecuteUpdate` yirmi
    /// satırı tek sorguda değiştirir ve agregatın kapısını tamamen atlar. İşlem sınırının
    /// ilan başına olması (**C3**/**D3**) da o gün sessizce kaybolur.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-20: Toplu eylem toplu SQL ile yazmaz; işlem sınırı ilan başına kalır")]
    public void Yazmaz_TopluSql_Ile()
    {
        string kaynak = KomutKaynagi();

        Assert.DoesNotContain("ExecuteUpdate", kaynak, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteDelete", kaynak, StringComparison.Ordinal);
    }

    private static string KomutKaynagi()
    {
        string? yol = SolutionLayout.SourceFiles()
            .FirstOrDefault(file => Path.GetFileName(file) == TopluKomut);

        Assert.NotNull(yol);

        // Yorumlar ELENİYOR: gerekçeyi anlatan bir yorumda geçen `ExecuteUpdate` sözcüğü
        // kapıyı kırmızıya döndürmemeli.
        return string.Join(
            Environment.NewLine,
            File.ReadAllLines(yol).Where(satir =>
            {
                string kirpilmis = satir.TrimStart();

                return !kirpilmis.StartsWith("//", StringComparison.Ordinal)
                    && !kirpilmis.StartsWith("///", StringComparison.Ordinal)
                    && !kirpilmis.StartsWith("*", StringComparison.Ordinal);
            }));
    }

    /// <summary>
    /// Agregatın geçiş metotları. `listing.Publish(` gibi bir çağrı toplu komutta
    /// bulunursa karar oraya sızmış demektir.
    /// </summary>
    [GeneratedRegex(@"\.(Publish|Unpublish|Reserve|ReturnToShowcase|MarkSold|Archive|Feature|Unfeature)\s*\(")]
    private static partial Regex AggregateTransition();
}
