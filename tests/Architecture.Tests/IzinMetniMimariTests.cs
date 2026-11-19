using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// `0019` — izin metni sürümünün sınırları: okuma sözleşmesinin yüzeyi (AC-38), `Leads` →
/// `Content` bağının biçimi (AC-45) ve tek atlaması (AC-46), migration'ların ham SQL'i dâhil
/// kendi şemasında kalması (AC-47) ve belgelerin koda karşı doğruluğu (AC-55, AC-56).
/// </summary>
public sealed partial class IzinMetniMimariTests
{
    private const string SozlesmeDosyasi = "src/Modules/Content/Content.Contracts/PrivacyNotice/PrivacyNoticeContracts.cs";

    [Fact(DisplayName = "0019/AC-38: Yayınlanmış sürümü düzenleyen ya da silen bir uç yoktur; IPrivacyNoticeQueries'in üyeleri yalnız FindPublishedAsync ve FindVersionAsync'tir")]
    public void Yoktur_DuzenleyenYaDaSilenYol()
    {
        string sozlesme = File.ReadAllText(Path.Combine(SolutionLayout.Root, SozlesmeDosyasi));
        Match arayuz = InterfaceBody().Match(sozlesme);
        Assert.True(arayuz.Success, "IPrivacyNoticeQueries bulunamadı");

        List<string> uyeler = [.. MemberName().Matches(arayuz.Groups["govde"].Value).Select(match => match.Groups["ad"].Value)];

        // Sürüm kümesine dokunan bir yazma çağrısı ya da sürüm adresli bir uç, `Content`'te
        // de başka yerde de yoktur. Migration (tablo + tetikleyici) yazar, uygulama yalnız ekler.
        Regex yazma = new(
            @"PrivacyNoticeVersions\s*\.\s*(Remove|RemoveRange|Update|UpdateRange|Attach|AttachRange|ExecuteDelete|ExecuteUpdate)\w*\s*\(|Map(Put|Patch|Delete|Post)\s*\(\s*""[^""]*(privacy-notice|version)",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));
        List<string> ihlaller =
        [
            .. SolutionLayout.SourceFiles()
                .Where(file => !SolutionLayout.Relative(file).Contains("/Migrations/", StringComparison.Ordinal))
                .SelectMany(file => SolutionLayout.CodeLines(file).Where(line => yazma.IsMatch(line)).Select(line => $"{SolutionLayout.Relative(file)}: {line.Trim()}")),
        ];

        Assert.Equal(["FindPublishedAsync", "FindVersionAsync"], uyeler);
        Assert.Empty(ihlaller);
    }

    [Fact(DisplayName = "0019/AC-45: Leads kaynağında content şemasına erişen kod yoktur ve Leads Content'e yalnız Content.Contracts projesi üzerinden bağlanır (0017 AC-129'un kapısı bu kartın değişikliğiyle yeşil kalır)")]
    public void Baglanir_YalnizSozlesmeyle_LeadsContente()
    {
        string leads = SolutionLayout.ProjectFiles().Single(file => SolutionLayout.Relative(file) == "src/Modules/Leads/Leads/Leads.csproj");
        List<string> contentReferanslari =
        [
            .. SolutionLayout.ProjectReferences(leads)
                .Where(reference => SolutionLayout.ModuleNameOf(reference) == "Content")
                .Select(SolutionLayout.Relative),
        ];

        Regex sema = SolutionLayout.SchemaReference("content");
        List<string> semaIhlali =
        [
            .. SolutionLayout.SourceFiles()
                .Where(file => SolutionLayout.Relative(file).StartsWith("src/Modules/Leads/", StringComparison.Ordinal))
                .SelectMany(file => SolutionLayout.CodeLines(file).Where(line => sema.IsMatch(line)).Select(line => $"{SolutionLayout.Relative(file)}: {line.Trim()}")),
        ];

        Assert.Equal(["src/Modules/Content/Content.Contracts/Content.Contracts.csproj"], contentReferanslari);
        Assert.Empty(semaIhlali);
    }

    [Fact(DisplayName = "0019/AC-46: IPrivacyNoticeQueries'i gerçekleyen Content dosyası başka bir modülün Contracts ad alanını kullanmaz (tek atlama — BildirimSinirTests.cs:114-131 deseni)")]
    public void KullanmazBaskaSozlesme_SurumOkumasi()
    {
        List<string> gerceklemeler =
        [
            .. SolutionLayout.SourceFiles()
                .Where(file => SolutionLayout.ModuleNameOf(file) == "Content"
                    && !SolutionLayout.Relative(file).Contains(".Contracts/", StringComparison.Ordinal)
                    && ImplementsPrivacyNoticeQueries().IsMatch(File.ReadAllText(file))),
        ];

        List<string> ikinciAtlama =
        [
            .. gerceklemeler.SelectMany(file => ForeignContractUsing().Matches(File.ReadAllText(file))
                .Select(match => match.Groups["module"].Value)
                .Where(module => module != "Content")
                .Select(module => $"{SolutionLayout.Relative(file)} → {module}.Contracts")),
        ];

        Assert.NotEmpty(gerceklemeler);
        Assert.Empty(ikinciAtlama);
    }

    /// <summary>
    /// D20 — var olan kapı (<see cref="MigrationSchemaTests"/>) yalnız EF'in <c>schema:</c>
    /// argümanını görür; tetikleyici ve başlangıç sürümü ham SQL'dir. Bu kapı bütün
    /// migration'lardaki NİTELİKLİ adları (<c>şema.tablo</c>) okur ve bilinen modül şemalarından
    /// biriyse kendi şemasıyla karşılaştırır. Bu kartın iki migration'ında en az bir nitelikli
    /// ad bulunması ayrıca sınanır: hiçbir şey bulmayan bir kapı sessizce açıktır.
    /// </summary>
    [Fact(DisplayName = "0019/AC-47: Content'in yeni migration'ı yalnız content, Leads'in yeni migration'ı yalnız leads şemasına dokunur; ham SQL (Sql(...)) içindeki nitelikli şema.tablo adları dâhil")]
    public void Dokunur_YalnizKendiSemasina_HamSqlDahil()
    {
        HashSet<string> semalar =
        [
            .. SolutionLayout.SourceFiles()
                .SelectMany(file => SchemaConstant().Matches(File.ReadAllText(file)).Select(match => match.Groups["schema"].Value)),
        ];

        List<string> ihlaller = [];
        Dictionary<string, int> kartinMigrationlari = new(StringComparer.Ordinal);

        foreach (string file in SolutionLayout.SourceFiles().Where(file => SolutionLayout.Relative(file).Contains("/Migrations/", StringComparison.Ordinal)))
        {
            string beklenen = string.Concat(SolutionLayout.ModuleNameOf(file).Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
            int sayi = 0;

            foreach (Match match in QualifiedName().Matches(File.ReadAllText(file)))
            {
                string sema = match.Groups["schema"].Value;

                if (!semalar.Contains(sema))
                {
                    continue;
                }

                sayi++;

                if (sema != beklenen)
                {
                    ihlaller.Add($"{SolutionLayout.Relative(file)}: '{match.Value}' (beklenen şema: '{beklenen}')");
                }
            }

            if (file.EndsWith("_PrivacyNoticeVersions.cs", StringComparison.Ordinal) || file.EndsWith("_LeadPrivacyNotice.cs", StringComparison.Ordinal))
            {
                kartinMigrationlari[Path.GetFileName(file)] = sayi;
            }
        }

        Assert.Contains("content", semalar);
        Assert.Contains("leads", semalar);
        Assert.Equal(2, kartinMigrationlari.Count);
        Assert.Contains(kartinMigrationlari, pair => pair.Key.EndsWith("_PrivacyNoticeVersions.cs", StringComparison.Ordinal) && pair.Value > 0);
        Assert.Empty(ihlaller);
    }

    [Fact(DisplayName = "0019/AC-55: docs/architecture.md § 1 bağımlılık haritasında Leads satırı VehicleCatalog, VehicleReference, Content'i gösterir (Dealership sapması kaldırılır), Dealership'in gelen listesi koddaki referanslarla (VehicleCatalog, Identity, Notifications) aynıdır, Content \"hiçbir modül onlara senkron bağımlı değil\" satırından çıkar ve § 7 Karar Defteri'nde bu kartın satırı vardır")]
    public void Esittir_HaritaKoda()
    {
        string mimari = File.ReadAllText(Path.Combine(SolutionLayout.Root, "docs", "architecture.md"));
        Match harita = MapBlock().Match(mimari);
        Assert.True(harita.Success, "§ 1 bağımlılık haritası bulunamadı");
        string[] satirlar = harita.Groups["harita"].Value.Split('\n');

        List<string> leadsGiden = Liste(satirlar.Single(line => Regex.IsMatch(line, @"^\s*Leads\s+──►", RegexOptions.None, TimeSpan.FromSeconds(2))), "──►");
        List<string> dealershipGelen = Liste(satirlar.Single(line => Regex.IsMatch(line, @"^\s*Dealership\s+◄", RegexOptions.None, TimeSpan.FromSeconds(2))), "◄────");
        int yaprak = Array.FindIndex(satirlar, line => line.Contains("YAPRAK", StringComparison.Ordinal));

        Assert.Equal(ModulReferanslari("Leads"), leadsGiden.Order(StringComparer.Ordinal));
        Assert.Equal(["Content", "VehicleCatalog", "VehicleReference"], leadsGiden.Order(StringComparer.Ordinal));
        Assert.Equal(GelenReferanslar("Dealership"), dealershipGelen.Order(StringComparer.Ordinal));
        Assert.DoesNotContain("Content", satirlar[yaprak + 1], StringComparison.Ordinal);
        Assert.Contains(
            mimari.Split('\n').SkipWhile(line => !line.StartsWith("## 7. Karar Defteri", StringComparison.Ordinal)),
            line => line.StartsWith("| İzin anındaki aydınlatma metni |", StringComparison.Ordinal) && line.Contains("`0019`", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0019/AC-56: docs/domain.md L8 iş kuralını ve Yayınlanmış Metin Sürümü terimini D12 ve D27'deki anlamıyla taşır")]
    public void Tasir_L8VeTerim()
    {
        string alan = File.ReadAllText(Path.Combine(SolutionLayout.Root, "docs", "domain.md"));
        string tekSatir = Regex.Replace(alan, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(2));

        Assert.Contains(
            "**L8.** Talep, bırakıldığı dilde ve kaydedildiği anda yayında olan KVKK aydınlatma metni sürümüne bağlanır; metin o dilde yayında değilse bu da kaydedilir. Bağ izinden bağımsızdır, sonradan değişmez ve anonimleştirmede korunur. Bu kural gelmeden önceki talepler 'kayıt tutulmuyordu' durumundadır.",
            tekSatir,
            StringComparison.Ordinal);
        Assert.Contains(
            "| **Yayınlanmış Metin Sürümü** | `PrivacyNoticeVersion` | KVKK aydınlatma metninin bir dilde yayına girdiği andaki değişmez kopyası (dil, başlık, gövde, kayda geçtiği an). Talep, bırakıldığı anda yayında olan sürüme bağlanır; sürüm silinmez ve düzenlenmez.",
            tekSatir,
            StringComparison.Ordinal);
    }

    private static List<string> Liste(string satir, string ok) =>
    [
        .. satir[(satir.IndexOf(ok, StringComparison.Ordinal) + ok.Length)..]
            .Split('(')[0]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
    ];

    private static List<string> ModulReferanslari(string modul) =>
    [
        .. SolutionLayout.ProjectReferences(Path.Combine(SolutionLayout.Root, "src", "Modules", modul, modul, $"{modul}.csproj"))
            .Where(reference => SolutionLayout.IsModuleProject(reference) && SolutionLayout.ModuleNameOf(reference) != modul)
            .Select(SolutionLayout.ModuleNameOf)
            .Order(StringComparer.Ordinal),
    ];

    private static List<string> GelenReferanslar(string modul) =>
    [
        .. SolutionLayout.ProjectFiles()
            .Where(project => SolutionLayout.IsModuleProject(project)
                && !SolutionLayout.IsContractsProject(project)
                && SolutionLayout.ModuleNameOf(project) != modul
                && SolutionLayout.ProjectReferences(project).Any(reference => SolutionLayout.ModuleNameOf(reference) == modul))
            .Select(SolutionLayout.ModuleNameOf)
            .Order(StringComparer.Ordinal),
    ];

    [GeneratedRegex(@"public interface IPrivacyNoticeQueries\s*\{(?<govde>.*?)\n\}", RegexOptions.Singleline)]
    private static partial Regex InterfaceBody();

    [GeneratedRegex(@"^\s*Task<[^\n]*?>\s+(?<ad>\w+)\s*\(", RegexOptions.Multiline)]
    private static partial Regex MemberName();

    [GeneratedRegex(@"(?::|,)\s*IPrivacyNoticeQueries\b")]
    private static partial Regex ImplementsPrivacyNoticeQueries();

    [GeneratedRegex(@"^\s*using\s+CarGallery\.Modules\.(?<module>\w+)\.Contracts", RegexOptions.Multiline)]
    private static partial Regex ForeignContractUsing();

    [GeneratedRegex(@"const string Schema = ""(?<schema>[a-z_]+)""")]
    private static partial Regex SchemaConstant();

    [GeneratedRegex(@"\b(?<schema>[a-z_]+)\.(?<table>[a-z_]{2,})\b")]
    private static partial Regex QualifiedName();

    [GeneratedRegex(@"### Bağımlılık haritası\s*```(?<harita>.*?)```", RegexOptions.Singleline)]
    private static partial Regex MapBlock();
}
