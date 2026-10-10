using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec 0011 — AC-17, AC-35.
///
/// <para>Hepsi aynı şeyi koruyor: <b>`Media` bağımsızdır</b> (C5/D6). Ondan dışarı ok
/// çıkmaz — hiçbir modülü senkron çağırmaz, hiçbir modülün verisini okumaz ve hiçbir
/// modülün deposuna yazmaz. <b>Ona doğru gelen ok meşrudur:</b> § 1'in bağımlılık
/// haritası `VehicleCatalog ──► Media` diyor ve § 2.A onu adıyla sayıyor.</para>
///
/// <para>Kural kod gözden geçirmesiyle korunamaz: filigranı `Dealership`'ten okuyan tek
/// bir satır, bugün meşru görünen bir kolaylık ve yarın `Media → Dealership →` zinciridir
/// (Yasak Listesi 13).</para>
/// </summary>
public sealed partial class MedyaSinirTests
{
    private const string Module = "Media";

    /// <summary>
    /// Diğer modüllerin şemaları. Elle sayılıyor çünkü testin sorduğu soru "hangi şemalar
    /// var" değil, "bu modül onlardan birine dokunuyor mu" — `BildirimSinirTests`'in aynı
    /// gerekçesi.
    /// </summary>
    private static readonly string[] OtherSchemas =
        ["vehicle_catalog", "vehicle_reference", "dealership", "identity", "leads", "notifications"];

    [Fact(DisplayName = "0011/AC-35: `Media`'dan başka bir modüle referans çıkmaz; `Media` başka modülün tablosuna veya dosya deposuna erişmez")]
    public void Cikmaz_Referans_MediadanBaskaModule()
    {
        List<string> violations = [];

        // Birinci yarı — PROJE referansı. İzinli tek yabancı yüzey `Identity.Contracts`:
        // yönetim kökü ve oturum politikası sabitleri oradan geliyor (`AdminSurface`) ve
        // kökü yeniden yazmak 0006/C18'in rota TARAYAN testinin medya ucunu hiç görmemesi
        // demekti. Bu bir VERİ bağımlılığı değil, adres sabiti.
        foreach (string project in SolutionLayout.ProjectFiles())
        {
            if (SolutionLayout.ModuleNameOf(project) != Module)
            {
                continue;
            }

            foreach (string reference in SolutionLayout.ProjectReferences(project))
            {
                if (!SolutionLayout.IsModuleProject(reference)
                    || SolutionLayout.ModuleNameOf(reference) == Module)
                {
                    continue;
                }

                if (!SolutionLayout.IsContractsProject(reference))
                {
                    violations.Add($"iç projeye referans: {SolutionLayout.Relative(reference)}");
                    continue;
                }

                if (SolutionLayout.ModuleNameOf(reference) != "Identity")
                {
                    violations.Add($"izinsiz sözleşme referansı: {SolutionLayout.Relative(reference)}");
                }
            }
        }

        // İkinci yarı — KAYNAKTA başka modülün şemasına dokunan satır. Referans olmadan da
        // ham SQL ile dokunulabilir ve birinci yarı onu göremez (Yasak Listesi 1).
        foreach (string file in MediaSourceFiles())
        {
            foreach (string schema in OtherSchemas)
            {
                violations.AddRange(MatchingLines(file, SolutionLayout.SchemaReference(schema)));
            }
        }

        Assert.True(
            violations.Count == 0,
            "`Media` bağımsızdır: ondan dışarı ok çıkmaz (C5/D6, Yasak Listesi 1, 4, 7):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0011/AC-17: Filigran kaynağı yapılandırmadan gelir; `Media` bunun için başka bir modülü çağırmaz")]
    public void Gelir_FiligranKaynagi_Yapilandirmadan()
    {
        List<string> violations = [];

        // D4 — filigran kaynağı yapılandırmanın bir alanıdır. Alan kaybolursa kriterin
        // dayanağı da kaybolur; önce VAR olduğu doğrulanıyor.
        string setup = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/Media/Media.Contracts/Setup/MediaSetup.cs"));

        Assert.Contains("WatermarkObjectKey", setup, StringComparison.Ordinal);

        // Asıl kural: filigran YOLUNDA başka bir modülün sözleşmesi okunmuyor.
        // `Dealership.Contracts`'tan logo çözmek C5'i ve bağımlılık haritasını bozar —
        // ve D4'ün elediği alternatif tam olarak buydu.
        foreach (string file in MediaSourceFiles())
        {
            foreach (Match match in ForeignContractUsing().Matches(File.ReadAllText(file)))
            {
                string referenced = match.Groups["module"].Value;

                if (referenced is not ("Media" or "Identity"))
                {
                    violations.Add($"{SolutionLayout.Relative(file)} → {referenced}.Contracts");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Filigran kaynağı yapılandırmadan gelir; `Media` başka modülü çağırmaz (D4, C5):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// D6'nın diğer yarısı: `Media` hiçbir modülün OLAYINI tüketmiyor. Tüketseydi inbox
    /// tablosu gerekirdi ve `MediaDbContext` onu taşımıyor — kural ile şema birbirini
    /// doğruluyor.
    /// </summary>
    [Fact]
    public void Tuketmez_Media_BaskaModulunOlayini()
    {
        List<string> violations = [];

        foreach (string file in MediaSourceFiles())
        {
            violations.AddRange(MatchingLines(file, EventHandlerDeclaration()));
        }

        Assert.True(
            violations.Count == 0,
            "`Media` konuşur, dinlemez: olay tüketicisi yoktur ve bu yüzden inbox tablosu "
            + $"da yoktur (D6):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// D16 — orijinalin erişim yolu hiçbir yanıt DTO'sunda bulunmaz. Gizlenmiyor, hiç
    /// bulunmuyor (Yasak Listesi 41'in aynı mantığı): ham dosya konum bilgisi taşıyor ve
    /// R5'in garantisi dışarı çıkanı temizlemek.
    /// </summary>
    [Fact]
    public void Bulunmaz_OrijinalinYolu_SozlesmeDtolarinda()
    {
        string contracts = Path.Combine(SolutionLayout.SourceRoot, "Modules/Media/Media.Contracts");

        List<string> violations = [];

        foreach (string file in Directory.EnumerateFiles(contracts, "*.cs", SearchOption.AllDirectories))
        {
            if (SolutionLayout.Relative(file).Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            violations.AddRange(MatchingLines(file, OriginalKeyReference()));
        }

        Assert.True(
            violations.Count == 0,
            "Orijinalin yolu sözleşme yüzeyinde bulunmaz (D16, R5):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Yasak Listesi 16 — bir modül başka modülün outbox tablosuna YAZMAZ.
    ///
    /// <para><b>Bu test bir kırılmanın bedeliydi.</b> `Media` ilk yazımda
    /// <c>AddScoped&lt;IOutboxWriter, OutboxWriter&lt;MediaDbContext&gt;&gt;()</c> diyordu.
    /// <c>IOutboxWriter</c> non-generic bir arayüz ve dört modül onu kendi
    /// <c>DbContext</c>'iyle kaydediyor; DI'da <b>son kayıt kazanıyor</b>. <c>AddMedia</c>
    /// Host'ta <c>AddLeads</c>'ten sonra çağrıldığı için `Leads`'in yazıcısı bir anda
    /// `Media`'nın oldu: <c>LeadCaptured</c> olayı <c>media</c> şemasının outbox'ına
    /// yazıldı, `Leads`'in dağıtıcısı kendi tablosunda hiçbir şey bulamadı ve bildirim
    /// zinciri sessizce durdu — <c>TalepBildirimiZinciriTests</c> 30 saniye bekleyip
    /// <b>0</b> kayıt gördü.</para>
    ///
    /// <para><b>Kapı bilinçli olarak YALNIZ `Media`'ya bakıyor.</b> Aynı tuzak bugün
    /// `Dealership` ve `Identity`'de de duruyor (ölçüldü: temiz <c>origin/main</c>'de
    /// <c>IOutboxWriter</c> → <c>OutboxWriter&lt;LeadsDbContext&gt;</c>, üç kayıt). Onları
    /// da kapsayan bir kapı ilk koşuda kırmızı verir ve düzeltmesi bu spec'in kapsamı
    /// dışında — ADR `0008`/K8 tam olarak bu durumu tartışıyor: susturulmuş bir kapı,
    /// olmayan kapıdan kötüdür. Geniş ihlal PR'da açık konu olarak raporlandı.</para>
    /// </summary>
    [Fact]
    public void Kaydetmez_Media_PaylasilanOutboxYazicisini()
    {
        string modul = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot, "Modules/Media/Media/MediaModule.cs"));

        List<string> violations = [];

        foreach (string satir in modul.Split('\n'))
        {
            string kirpilmis = satir.TrimStart();

            if (kirpilmis.StartsWith("//", StringComparison.Ordinal)
                || kirpilmis.StartsWith("///", StringComparison.Ordinal))
            {
                continue;
            }

            if (SharedOutboxWriterRegistration().IsMatch(satir))
            {
                violations.Add(kirpilmis);
            }
        }

        Assert.True(
            violations.Count == 0,
            "`Media` outbox yazıcısını paylaşılan `IOutboxWriter` arayüzü olarak kaydetmez: "
            + "son kayıt kazanır ve başka modülün olayı `media` şemasına yazılır "
            + $"(Yasak Listesi 16):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> MediaSourceFiles() =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.ModuleNameOf(file) == Module);

    private static IEnumerable<string> MatchingLines(string file, Regex pattern)
    {
        string relative = SolutionLayout.Relative(file);
        string[] lines = File.ReadAllLines(file);

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            // Yorum satırları atlanıyor: bu testlerin birçoğu kuralın GEREKÇESİNİ koda
            // yazıyor ve gerekçede geçen bir modül adı ihlal değildir.
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("*", StringComparison.Ordinal))
            {
                continue;
            }

            if (pattern.IsMatch(line))
            {
                yield return $"{relative}:{index + 1}  {line.Trim()}";
            }
        }
    }

    [GeneratedRegex(@"using\s+CarGallery\.Modules\.(?<module>\w+)\.Contracts")]
    private static partial Regex ForeignContractUsing();

    [GeneratedRegex(@"\bIIntegrationEventHandler<|\bIdempotentIntegrationEventHandler<|\bIInboxDbContext\b")]
    private static partial Regex EventHandlerDeclaration();

    [GeneratedRegex(@"\bOriginal(ObjectKey|Url|Path)\b")]
    private static partial Regex OriginalKeyReference();

    [GeneratedRegex(@"Add\w*<\s*IOutboxWriter\s*,")]
    private static partial Regex SharedOutboxWriterRegistration();
}
