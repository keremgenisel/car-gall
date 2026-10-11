using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec 0012a — AC-32, AC-33.
///
/// <para>Bu dilim <b>ilk kez</b> `VehicleCatalog`'dan `Media`'ya bir okuma açıyor ve
/// sınırın iki yönü ayrı ayrı sorulmak zorunda: `Media`'dan dışarı ok çıkmaz (AC-32) ve
/// ona doğru gelen ok yalnız sözleşme üzerinden, <b>tek atlamayla</b> gelir (AC-33).</para>
///
/// <para><b>`MedyaSinirTests`'in yerine geçmiyor.</b> O, `0011`'in kriterlerini koruyor
/// ve `Media`'nın bağımsızlığını proje referansı + şema taramasıyla ölçüyor. Buradaki
/// iki test `0012a`'nın donmuş kriterlerine bağlı ve ikisi de <b>daha dar</b> bir şey
/// ölçüyor: fotoğrafın hangi araca bağlı olduğunun `Media`'da TUTULMADIĞI (C2, M2) ve
/// gelen okun zincire dönüşemediği. Etiketsiz bir jenerik test, kriter → test
/// eşlemesinde görünmezdi (ADR `0008` / K8'in aynı gerekçesi).</para>
/// </summary>
public sealed partial class AracMedyaSiniriTests
{
    /// <summary>
    /// `Media`'nın kaynağında araç kimliği GEÇMEZ. Ölçüm kod satırlarında yapılıyor;
    /// yorumlarda `VehicleCatalog` adı on iki yerde geçiyor ve her biri kuralın
    /// GEREKÇESİ — gerekçeyi ihlal saymak, gerekçeyi silmeye davet olurdu.
    ///
    /// <para><b>Neden "senkron çağrı yapmaz"ın yanına bu da sorulmak zorunda:</b> bir
    /// modül başka modülü hiç çağırmadan da onun verisini tutabilir. <c>MediaAsset</c>'e
    /// bir <c>VehicleId</c> kolonu eklemek hiçbir referans, hiçbir <c>using</c> ve
    /// hiçbir şema adı üretmez — `MedyaSinirTests`'in iki yarısı da yeşil kalırdı. O
    /// kolon ise **M2**'yi (medya sahipsizdir) ve **C2**'yi siler, sıralı listeyi ikinci
    /// bir yerde tutar ve biri bayatlar.</para>
    /// </summary>
    [Fact(DisplayName = "0012a/AC-32: `Media` hiçbir modüle senkron çağrı yapmaz; bir fotoğrafın hangi araca bağlı olduğu `Media`'da tutulmaz")]
    public void Tutmaz_Media_FotografinHangiAracaBagliOldugunu()
    {
        string[] mediaFiles = [.. ModuleSourceFiles("Media")];

        // Tarama BOŞ geçmemeli: hiç dosya bulamayan bir tarama da "hepsi temiz" der.
        Assert.NotEmpty(mediaFiles);

        List<string> violations = [];

        foreach (string file in mediaFiles)
        {
            // Birinci yarı — araç kimliğini TUTAN ya da adını anan kod.
            violations.AddRange(CodeLines(file, VehicleBinding()));

            // İkinci yarı — başka modülün sözleşmesine `using`. İzinli tek yüzey
            // `Identity.Contracts`: yönetim kökü ve oturum politikası sabitleri oradan
            // geliyor ve bu bir VERİ bağımlılığı değil, adres sabiti (`0011` AC-47'nin
            // aynı istisnası).
            foreach (string line in CodeLines(file, ForeignContractUsing()))
            {
                if (!line.Contains("Identity.Contracts", StringComparison.Ordinal))
                {
                    violations.Add(line);
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Medya SAHİPSİZDİR (C2, M2) ve `Media`'dan dışarı ok çıkmaz (C3):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Gelen okun üç koşulu birlikte sorulmak zorunda, çünkü her biri ayrı bir yoldan
    /// delinir:
    ///
    /// <list type="number">
    /// <item><b>Proje referansı</b> yalnız `Media.Contracts`'a — iç projeye referans
    /// `MediaAsset`'in kendisine erişim demekti (Yasak Listesi 7).</item>
    /// <item><b>Kaynakta `using`</b> yalnız `Media.Contracts.*` ve yalnız
    /// <c>Application/</c> katmanında: `Domain` dış dünyayı bilmez
    /// (`docs/conventions.md` § 4) ve `Endpoints` başka modülün sözleşmesini doğrudan
    /// tüketirse katman atlanmış olur (§ 2.B).</item>
    /// <item><b>Tek atlama</b>: `Media.Contracts` başka bir modülün sözleşmesine
    /// bağlanmaz. Bağlanırsa `VehicleCatalog → Media → X` zinciri doğar ve tek atlama
    /// kuralı, hiçbir satırı değişmeden ihlal edilmiş olur (Yasak Listesi 13).</item>
    /// </list>
    /// </summary>
    [Fact(DisplayName = "0012a/AC-33: `VehicleCatalog`'un `Media`'ya erişimi yalnız `Media.Contracts` üzerinden ve tek atlamadır")]
    public void Erisir_VehicleCatalog_MediayaYalnizSozlesmeUzerindenVeTekAtlamayla()
    {
        List<string> violations = [];

        // 1 — proje referansı.
        foreach (string project in SolutionLayout.ProjectFiles()
            .Where(file => SolutionLayout.ModuleNameOf(file) == "VehicleCatalog"))
        {
            violations.AddRange(SolutionLayout.ProjectReferences(project)
                .Where(reference => SolutionLayout.ModuleNameOf(reference) == "Media"
                    && !SolutionLayout.IsContractsProject(reference))
                .Select(reference =>
                    $"iç projeye referans: {SolutionLayout.Relative(project)} → {SolutionLayout.Relative(reference)}"));
        }

        // 2 — kaynakta `using` ve hangi KATMANDA.
        int applicationUsings = 0;

        foreach (string file in ModuleSourceFiles("VehicleCatalog"))
        {
            foreach (string line in CodeLines(file, MediaUsing()))
            {
                string relative = SolutionLayout.Relative(file);

                if (!line.Contains("Media.Contracts", StringComparison.Ordinal))
                {
                    violations.Add($"sözleşme dışı erişim: {line}");
                    continue;
                }

                if (!relative.Contains("/VehicleCatalog/Application/", StringComparison.Ordinal))
                {
                    violations.Add($"katman atlandı (§ 2.B): {line}");
                    continue;
                }

                applicationUsings++;
            }
        }

        // 3 — tek atlama: sözleşme başka modülün sözleşmesine bağlanmıyor.
        string mediaContracts = SolutionLayout.ProjectFiles().Single(file =>
            Path.GetFileName(file) == "Media.Contracts.csproj");

        violations.AddRange(SolutionLayout.ProjectReferences(mediaContracts)
            .Where(SolutionLayout.IsModuleProject)
            .Where(reference => SolutionLayout.ModuleNameOf(reference) != "Media")
            .Select(reference =>
                $"ikinci atlama: Media.Contracts → {SolutionLayout.Relative(reference)}"));

        Assert.True(
            violations.Count == 0,
            "`VehicleCatalog → Media` yalnız okuma, yalnız sözleşme ve tek atlamadır "
            + $"(C3, `docs/architecture.md` § 2.A):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");

        // Tarama boş geçmemeli: `using` hiç bulunmazsa üç koşul da anlamsız biçimde yeşil.
        Assert.True(
            applicationUsings > 0,
            "`VehicleCatalog` hiçbir yerde `Media.Contracts`'ı tüketmiyor; tarama anlamsız.");
    }

    /// <summary>
    /// AC-13'ün İKİNCİ yarısı: `VehicleCatalog` `alt` metninin KOPYASINI tutmaz.
    ///
    /// <para><b>Bu test bir yanlış atıfın bedeliydi.</b> `AltMetniKaliciligiTests`'in doc
    /// comment'i bu yarının "`MedyaSinirTests` içinde" olduğunu yazıyordu; PR tablosunu
    /// kurarken ölçtüm ve <b>öyle bir test yoktu</b>. Yazılı bir atıf, testin var
    /// olduğunu garanti etmiyor.</para>
    ///
    /// <para><b>Nerede arandığı önemli:</b> metnin yanıt DTO'sundan GEÇMESİ meşrudur ve
    /// AC-24 bunu zaten istiyor — sorgu `Media`'nın `alt` metnini birleştiriyor. Yasak
    /// olan SAKLAMAK: agregatta bir alan, şemada bir kolon, migration'da bir satır.
    /// Tarama bu yüzden yalnız `Domain` ve `Infrastructure` katmanlarına bakıyor;
    /// `Application` ve `Contracts`'taki geçişi ihlal saymak, AC-24'ü yasaklamak olurdu.</para>
    /// </summary>
    [Fact(DisplayName = "0012a/AC-13: `alt` metni `Media`'da saklanır; `VehicleCatalog` metnin kopyasını tutmaz")]
    public void Tutmaz_VehicleCatalog_AltMetninKopyasini()
    {
        string[] saklamaKatmanlari =
        [
            .. ModuleSourceFiles("VehicleCatalog").Where(file =>
            {
                string relative = SolutionLayout.Relative(file);

                return relative.Contains("/VehicleCatalog/Domain/", StringComparison.Ordinal)
                    || relative.Contains("/VehicleCatalog/Infrastructure/", StringComparison.Ordinal);
            })
        ];

        Assert.NotEmpty(saklamaKatmanlari);

        List<string> violations = [];

        foreach (string file in saklamaKatmanlari)
        {
            violations.AddRange(CodeLines(file, AltTextStorage()));
        }

        Assert.True(
            violations.Count == 0,
            "`alt` metni `Media`'nın verisidir; `VehicleCatalog` kopyasını tutmaz "
            + $"(C5, D3, Yasak Listesi 1 ve 10):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");

        // Tarama anlamsız olmasın: metin GERÇEKTEN geçiyor, yalnız saklanmıyor.
        string sorgu = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/VehicleCatalog/VehicleCatalog/Application/Vehicles/VehicleMediaQueries.cs"));

        Assert.Contains("AltText", sorgu, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ModuleSourceFiles(string module) =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.ModuleNameOf(file) == module);

    /// <summary>
    /// Yorum satırlarını ATLAYAN tarama. `MedyaSinirTests.MatchingLines` ile aynı karar
    /// ve aynı gerekçe: bu testlerin birçoğu kuralın gerekçesini koda yazıyor.
    /// </summary>
    private static IEnumerable<string> CodeLines(string file, Regex pattern)
    {
        string relative = SolutionLayout.Relative(file);
        string[] lines = File.ReadAllLines(file);

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].TrimStart();

            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("///", StringComparison.Ordinal)
                || trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                continue;
            }

            if (pattern.IsMatch(lines[index]))
            {
                yield return $"{relative}:{index + 1}  {trimmed.Trim()}";
            }
        }
    }

    [GeneratedRegex(@"\bVehicleId\b|""vehicle_id""|\bVehicleCatalog\b")]
    private static partial Regex VehicleBinding();

    [GeneratedRegex(@"using\s+CarGallery\.Modules\.(?!Media\b)\w+\.Contracts")]
    private static partial Regex ForeignContractUsing();

    [GeneratedRegex(@"using\s+CarGallery\.Modules\.Media\b")]
    private static partial Regex MediaUsing();

    [GeneratedRegex(@"\bAltText\b|""alt_text""")]
    private static partial Regex AltTextStorage();
}
