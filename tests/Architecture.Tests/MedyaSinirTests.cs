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

    [Fact(DisplayName = "0011/AC-47: `Media` hiçbir modüle senkron çağrı yapmaz ve başka modülün tablosuna veya dosya deposuna erişmez")]
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

    [Fact(DisplayName = "0011/AC-22: Filigran kaynağı yapılandırmadan bir dosya yolu olarak gelir; `Media` bunun için başka bir modülü çağırmaz")]
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
    [Fact(DisplayName = "0011/AC-35: `Media`'nın olayı başka bir modülün outbox tablosuna yazılmaz")]
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


    /// <summary>
    /// AC-34'ün ETİKETLİ testi. `MigrationSchemaTests.DokunurYalnizKendiSemasina_…` tüm
    /// modülleri tarıyor ve `media` migration'ını da kapsıyor — ama jenerik olduğu için
    /// `DisplayName` taşımıyor ve etiket taramasıyla kurulan eşlemede görünmüyordu.
    ///
    /// <para>Bu test jenerik olanın yerine geçmiyor, onu `0011`'in kriterine BAĞLIYOR ve
    /// ayrıca daha dar bir şey ölçüyor: `media` migration'ında <b>yalnız</b> `media`
    /// şeması geçiyor ve dosya gerçekten VAR. Jenerik test, hiç migration bulamadığında
    /// da yeşil kalırdı.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-46: `media` migration'ı yalnızca `media` şemasına dokunur")]
    public void Dokunur_MediaMigrationi_YalnizMediaSemasina()
    {
        string[] migrations =
        [
            .. SolutionLayout.SourceFiles()
                .Where(file => SolutionLayout.Relative(file)
                    .StartsWith("src/Modules/Media/Media/Infrastructure/Migrations/", StringComparison.Ordinal))
        ];

        // Tarama BOŞ geçmemeli: hiç migration bulamayan bir tarama da "hepsi temiz" der.
        Assert.NotEmpty(migrations);

        List<string> violations = [];
        int mediaReferences = 0;

        foreach (string file in migrations)
        {
            foreach (Match match in SchemaLiteral().Matches(File.ReadAllText(file)))
            {
                string schema = match.Groups["schema"].Value;

                if (schema == "media")
                {
                    mediaReferences++;
                    continue;
                }

                violations.Add($"{SolutionLayout.Relative(file)}: '{schema}'");
            }
        }

        Assert.True(
            violations.Count == 0,
            "`media` migration'ı yalnız kendi şemasına dokunabilir (C9, Yasak Listesi 3):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");

        Assert.True(mediaReferences > 0, "Migration hiç şema adı geçirmiyor; tarama anlamsız.");
    }

    /// <summary>
    /// AC-36 — alıcı dağıtıcının sözleşmesini uyguluyor VE DI'ya kayıtlı.
    ///
    /// <para><b>İkisi birlikte sorulmak zorunda.</b> `0011`'in CLARIFY turu ölçtü:
    /// handler yazılmıştı ama arayüzü uygulamıyordu, DI'ya kayıtlı değildi ve tek çağıranı
    /// elle <c>vehicleId</c> veren bir testti. `0003`'ün kaçan hatası birebir buydu —
    /// <i>"test edilmiş bir alıcı, yayıncısı olmadan hiçbir şey kanıtlamaz."</i> Yalnız
    /// arayüzü sormak, kayıtsız bir alıcıda yeşil kalırdı.</para>
    ///
    /// <para>Duyurunun GERÇEKTEN dağıtıcı üzerinden ulaştığının kanıtı `tests/Host.Tests`
    /// (AC-37); bu test yalnız kablonun takılı olduğunu söylüyor.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-36: `MediaAssetReadyHandler` `IIntegrationEventHandler<MediaAssetReady>`'i uygular ve DI'ya kayıtlıdır")]
    public void Uygular_Alici_DagiticininSozlesmesini()
    {
        string handler = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/VehicleCatalog/VehicleCatalog/Application/EventHandlers/MediaAssetReadyHandler.cs"));

        Assert.Contains(
            "IdempotentIntegrationEventHandler<MediaAssetReady, VehicleCatalogDbContext>",
            handler,
            StringComparison.Ordinal);

        // `vehicleId` parametresi KALKTI: o değer olayın gövdesinde yok ve R10 gereği
        // `Media`'da da yok.
        //
        // Tarama YORUMLARI atlıyor ve bu bir hatanın bedeliydi: handler'ın doc comment'i
        // eski imzayı TARİF ediyor ve dosyayı tek dize olarak okuyan ilk hâlim onu ihlal
        // sandı. Gerekçeyi bozmak yerine taramayı daralttım — `MatchingLines`'ın aynı
        // kararı.
        string[] kodSatirlari =
        [
            .. handler.Split('\n')
                .Where(satir => !satir.TrimStart().StartsWith("//", StringComparison.Ordinal)
                    && !satir.TrimStart().StartsWith("///", StringComparison.Ordinal))
        ];

        Assert.DoesNotContain(
            "Guid vehicleId",
            string.Join('\n', kodSatirlari),
            StringComparison.Ordinal);

        string modul = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/VehicleCatalog/VehicleCatalog/VehicleCatalogModule.cs"));

        Assert.Contains(
            "IIntegrationEventDispatchTarget, MediaAssetReadyHandler",
            modul,
            StringComparison.Ordinal);
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

    [GeneratedRegex("schema:\\s*\"(?<schema>[a-z_]+)\"")]
    private static partial Regex SchemaLiteral();
}
