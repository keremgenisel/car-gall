namespace CarGallery.Architecture.Tests;

/// <summary>
/// `scripts/` altındaki her kabuk betiği ÇALIŞTIRILABİLİR olmalıdır.
///
/// <para>Bu kapı bir dış bulgunun bedeli: `scripts/dev-up.sh` bir commit'te çalıştırma
/// bitini kaybetti (`100755` → `100644`) ve `README.md`'nin yazdığı komut — bir sonraki
/// kişinin depodaki İLK komutu — <c>permission denied</c> ile düştü. Dosyanın içeriği
/// doğruydu; yalnız izni gitmişti, yani hiçbir derleyici ve hiçbir test bunu söylemedi.</para>
///
/// <para>İzin bilgisi git ağacında (`100755`) yaşıyor ve checkout onu dosya sistemine
/// uyguluyor; kapı bu yüzden dosyanın kendi modunu okuyor, `git` çağırmıyor. Windows'ta
/// Unix izin biti diye bir şey yok ve kapı orada ölçüm yapmıyor — depo CI'ı Linux,
/// geliştirme macOS, yani pratikte her koşuda ölçülüyor.</para>
/// </summary>
public sealed class BetikKapisiTests
{
    [Fact(DisplayName = "Mimari test: `scripts/` altındaki her kabuk betiği çalıştırılabilir")]
    public void Calistirilabilir_HerKabukBetigi()
    {
        string klasor = Path.Combine(SolutionLayout.Root, "scripts");

        Assert.True(Directory.Exists(klasor), $"`scripts/` bulunamadı; kapı yanlış yere bakıyor: {klasor}");

        List<string> betikler = [.. Directory.EnumerateFiles(klasor, "*.sh", SearchOption.AllDirectories).Order()];

        // Kapının kendisi de ölçülür: liste boşalırsa kural hiçbir şey sormadan yeşil
        // kalır ve bunu sessizce yapardı.
        Assert.True(betikler.Count >= 2, $"`scripts/` altında beklenenden az betik var ({betikler.Count}).");

        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Sorgu yerine döngü: `OperatingSystem.IsWindows()` koruması akış analiziyle
        // görülüyor, bir lambda'nın içinden görülmüyor (CA1416).
        List<string> ihlaller = [];

        foreach (string yol in betikler)
        {
            if (!File.GetUnixFileMode(yol).HasFlag(UnixFileMode.UserExecute))
            {
                ihlaller.Add(SolutionLayout.Relative(yol));
            }
        }

        Assert.True(
            ihlaller.Count == 0,
            "Dokümanda yazan komut çalışmalıdır; çalıştırma biti olmayan betik, sebebini kimsenin "
            + $"aramayacağı bir yerde düşer:{Environment.NewLine}{string.Join(Environment.NewLine, ihlaller)}"
            + $"{Environment.NewLine}Düzeltme: git update-index --chmod=+x <yol>");
    }
}
