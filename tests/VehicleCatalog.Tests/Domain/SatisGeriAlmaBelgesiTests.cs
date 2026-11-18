namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// `0023` **C3** — kural belgede ve kodda AYNI şeyi söyler. Kart A3/A4'ü değiştirdi ve panelin
/// "geri al düğmesi kuralı delerdi" yorumunu geçersiz kıldı; ikisi de hiçbir derleyicinin
/// görmediği metin ve ancak okunarak doğrulanabilir.
///
/// <para>Belge ve panel dosyası <b>kök yukarı yürünerek</b> bulunuyor, sabit sayıda `..` ile
/// değil (`docs/testing.md` § 3, `DuyuruTests`'in deseni).</para>
/// </summary>
public sealed class SatisGeriAlmaBelgesiTests
{
    [Fact(DisplayName = "0023/AC-34: `docs/domain.md` A3 geçiş tablosu ve A4 bu kartın davranışını anlatır; `ListingActionBar.tsx`'teki \"A4 — geri al düğmesi kuralı delerdi\" yorumu kaldırılır")]
    public void Anlatir_BelgeVePanel_GeriAlmayi()
    {
        string kok = Kok();
        string domain = File.ReadAllText(Path.Combine(kok, "docs", "domain.md"));
        string panel = File.ReadAllText(Path.Combine(
            kok, "frontend", "src", "features", "catalog", "admin", "ListingActionBar.tsx"));

        string a3 = Bolum(domain, "**A3.**", "**A4.**");
        string a4 = Bolum(domain, "**A4.**", "**A5.**");

        Assert.Contains("Satıldı  → Arşiv | Yayında* | Rezerve* | Taslak*", a3, StringComparison.Ordinal);
        Assert.Contains("Yalnız \"satışı geri al\" geçişiyle ve yalnız Yöneticiye", a3, StringComparison.Ordinal);
        Assert.Contains("ticari olarak terminaldir", a4, StringComparison.Ordinal);
        Assert.Contains("gerekçeli geri almasıyla", a4, StringComparison.Ordinal);
        Assert.Contains("**gerçekten** geri alınırsa yine **yeni bir araç kaydı**", a4, StringComparison.Ordinal);
        Assert.DoesNotContain("kuralı delerdi", panel, StringComparison.Ordinal);
    }

    private static string Bolum(string belge, string baslangic, string bitis)
    {
        int ilk = belge.IndexOf(baslangic, StringComparison.Ordinal);
        int son = belge.IndexOf(bitis, ilk + 1, StringComparison.Ordinal);

        Assert.True(ilk >= 0 && son > ilk, $"{baslangic} bölümü bulunamadı.");

        return belge[ilk..son];
    }

    private static string Kok()
    {
        DirectoryInfo? dizin = new(AppContext.BaseDirectory);

        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "docs", "domain.md")))
        {
            dizin = dizin.Parent;
        }

        Assert.NotNull(dizin);

        return dizin.FullName;
    }
}
