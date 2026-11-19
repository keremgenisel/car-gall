using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec 0009 — AC-25, AC-29, AC-30, AC-31.
///
/// <para>Bu testlerin hepsi aynı şeyi koruyor: <b>sistemden dışarı posta gönderen tek
/// yer `Notifications`'tır</b> (C1, Yasak Listesi 17). Kural kod gözden geçirmesiyle
/// korunamaz — bir modülde tek bir <c>SmtpClient</c> satırı, bildirimlerin iki ayrı
/// yerden gitmesi ve hiçbirinin geçmişinin tutulmaması demektir.</para>
/// </summary>
public sealed partial class BildirimSinirTests
{
    private const string Module = "Notifications";

    /// <summary>
    /// Diğer modüllerin şemaları. Elle sayılıyor çünkü testin sorduğu soru "hangi şemalar
    /// var" değil, "bu modül onlardan birine dokunuyor mu" — ve yeni bir modül eklendiğinde
    /// bu listeye eklenmemesi, testin sessizce zayıflaması olurdu. Listeyi kısa tutan şey
    /// modül sayısı, testin kendisi değil.
    /// </summary>
    private static readonly string[] OtherSchemas =
        ["vehicle_catalog", "vehicle_reference", "dealership", "identity", "leads"];

    [Fact(DisplayName = "0009/AC-29: Mimari test: `Notifications` dışındaki hiçbir modülde e-posta gönderen çağrı bulunmaz")]
    public void Bulunmaz_EpostaGonderenCagri_DigerModullerde()
    {
        List<string> violations = [];

        foreach (string file in SolutionLayout.SourceFiles())
        {
            if (SolutionLayout.ModuleNameOf(file) == Module)
            {
                continue;
            }

            violations.AddRange(MatchingLines(file, EmailSendingCall()));
        }

        // Paket referansı da bir ihlaldir: MailKit'i referans alan bir modül, o çağrıyı
        // yazmaya bir satır uzaklıktadır ve kuralın kapısı ancak burada kapanır.
        foreach (string project in SolutionLayout.ProjectFiles())
        {
            if (SolutionLayout.ModuleNameOf(project) == Module)
            {
                continue;
            }

            if (File.ReadAllText(project).Contains("MailKit", StringComparison.Ordinal))
            {
                violations.Add($"{SolutionLayout.Relative(project)}: MailKit paket referansı");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Sistemden dışarı posta gönderen tek modül Notifications'tır (Yasak Listesi 17):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0009/AC-30: Mimari test: `Notifications` başka bir modülün şemasına dokunmaz")]
    public void Dokunmaz_BaskaModulunSemasina()
    {
        List<string> violations = [];

        foreach (string file in NotificationsFiles())
        {
            string relative = SolutionLayout.Relative(file);
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string schema in OtherSchemas)
                {
                    if (SchemaReference(schema).IsMatch(lines[index]))
                    {
                        violations.Add($"{relative}:{index + 1}  '{schema}'");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Modül kendi şemasının dışına çıkmaz (Yasak Listesi 1):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact(DisplayName = "0009/AC-31: Mimari test: `Notifications`'ın `Leads` ve `Dealership`'e erişimi yalnız `X.Contracts` üzerinden ve tek atlamadır; `Notifications → Leads → VehicleCatalog` zinciri bulunmaz")]
    public void TekAtlama_YalnizSozlesmeUzerinden()
    {
        List<string> violations = [];

        string project = SolutionLayout.ProjectFiles().Single(path =>
            Path.GetFileName(path) == "Notifications.csproj" && !SolutionLayout.IsContractsProject(path));

        // Birinci yarı: yalnız X.Contracts (Yasak Listesi 7).
        foreach (string reference in SolutionLayout.ProjectReferences(project))
        {
            if (!SolutionLayout.IsModuleProject(reference)
                || SolutionLayout.ModuleNameOf(reference) == Module
                || SolutionLayout.IsContractsProject(reference))
            {
                continue;
            }

            violations.Add($"iç projeye referans: {SolutionLayout.Relative(reference)}");
        }

        // İkinci yarı — asıl kural: TEK ATLAMA (Yasak Listesi 13). `Notifications`'ın
        // çağırdığı sorguların KENDİLERİ başka bir modülün sözleşmesine uzanırsa zincir
        // `Notifications → Leads → VehicleCatalog` olur ve bir modülün düşmesi üçünü
        // birden düşürür. Birinci yarı bunu göremez: her atlama tek tek kuraldışı değil.
        foreach (string file in ConsumedQueryImplementations())
        {
            string ownModule = SolutionLayout.ModuleNameOf(file);

            foreach (Match match in ForeignContractUsing().Matches(File.ReadAllText(file)))
            {
                string referencedModule = match.Groups["module"].Value;

                if (referencedModule != ownModule)
                {
                    violations.Add($"{SolutionLayout.Relative(file)} → {referencedModule}.Contracts (ikinci atlama)");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Erişim yalnız X.Contracts üzerinden ve tek atlamadır (Yasak Listesi 7, 13):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// AC-25 — gönderim yolundaki log çağrıları alıcı adını, adresini veya telefonunu
    /// PARAMETRE OLARAK almaz.
    ///
    /// <para>Taranan şey gövde değil <b>çağrı yüzeyi</b>: kişisel veri bir kez log
    /// parametresine girdiğinde artık uygulamanın kontrolünde değildir — log hedefi
    /// nereye yazarsa oraya gider ve geçmişten silinmesi de mümkün olmaz. AC-24 kaydı
    /// korur, bu kriter logu.</para>
    /// </summary>
    [Fact(DisplayName = "0009/AC-25: Gönderim yolundaki log çağrıları alıcı adını, adresini veya telefonunu parametre olarak almaz; bunu çağrı yüzeyini tarayan bir test doğrular")]
    public void AlmazParametreOlarak_KisiselVeri_LogCagrilari()
    {
        List<string> violations = [];

        foreach (string file in NotificationsFiles())
        {
            string text = File.ReadAllText(file);

            foreach (string call in LogCalls(text))
            {
                Match banned = PersonalDataToken().Match(call);

                if (banned.Success)
                {
                    violations.Add($"{SolutionLayout.Relative(file)}: '{banned.Value}' → {Ozet(call)}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Kişisel veri log parametresine girmez (docs/conventions.md § 12):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// `0018` AC-83 — zincir bekçisinin taradığı arayüzler ARTIK SABİT DEĞİL: başka modüllerin
    /// `Contracts`'ında tanımlı ve `Notifications`'ın kodunda adı geçen her arayüz. Eski hâli üç
    /// adı elle sayıyordu ve yorumu "liste kendiliğinden değişir" diyordu; yeni bir arayüz
    /// eklendiğinde değişmiyor ve kapı yeşil kalıyordu (`0018` Context, QA B2).
    /// </summary>
    [Fact(DisplayName = "0018/AC-83: Zincir testi (`BildirimSinirTests`) `Notifications`'ın tükettiği **bütün** sorgu arayüzlerini tarar — yeni ilan özeti okuması, model adı okuması ve `IGalleryDisplayNameQueries` dâhil — ve gerçekleştirimlerinden biri başka bir modülün `Contracts`'ına uzandığında kırmızı verir (negatif kanıt DoD'da)")]
    public void Tarar_TuketilenButunArayuzleri()
    {
        IReadOnlyList<string> consumed = ConsumedInterfaces();

        // Kapının boşalmadığı ayrıca sınanır: sıfır arayüzle koşan tarama yeşildir.
        foreach (string expected in new[]
        {
            "ILeadNotificationQueries",
            "IBranchContactQueries",
            "IListingSlugQueries",
            "IListingSummaryQueries",
            "IVehicleModelQueries",
            "IGalleryDisplayNameQueries",
        })
        {
            Assert.Contains(expected, consumed);
        }

        List<string> implementations = [.. ConsumedQueryImplementations()];

        Assert.True(
            implementations.Count >= 5,
            $"Gerçekleştirim bulunamadı; tarama boş koşuyor: {string.Join(", ", implementations)}");

        List<string> violations = [];

        foreach (string file in implementations)
        {
            string ownModule = SolutionLayout.ModuleNameOf(file);

            foreach (Match match in ForeignContractUsing().Matches(File.ReadAllText(file)))
            {
                if (match.Groups["module"].Value != ownModule)
                {
                    violations.Add($"{SolutionLayout.Relative(file)} → {match.Groups["module"].Value}.Contracts (ikinci atlama)");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Notifications'ın okuduğu yüzeyler başka bir modüle uzanmaz (Yasak Listesi 13, 0018 C12):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// `0018` AC-90 — abonelik, teyit, çıkış, dönem ve gönderim yolundaki log çağrıları e-posta
    /// adresini, teyit anahtarını ve çıkış bağlantısını PARAMETRE OLARAK almaz (`0009` AC-25'in
    /// çağrı yüzeyi taraması; yasak kümeye anahtar ve bağlantı eklendi).
    /// </summary>
    [Fact(DisplayName = "0018/AC-90: Abonelik, teyit, çıkış, dönem ve gönderim yolundaki log çağrıları e-posta adresini, teyit anahtarını ve çıkış bağlantısını parametre olarak almaz — çağrı yüzeyini tarayan testle (`0009` AC-25 deseni)")]
    public void AlmazParametreOlarak_AdresAnahtarVeBaglanti()
    {
        List<string> violations = [];
        int calls = 0;

        foreach (string file in NotificationsFiles())
        {
            foreach (string call in LogCalls(File.ReadAllText(file)))
            {
                calls++;
                Match banned = LinkOrTokenToken().Match(call);

                if (banned.Success)
                {
                    violations.Add($"{SolutionLayout.Relative(file)}: '{banned.Value}' → {Ozet(call)}");
                }
            }
        }

        Assert.True(calls >= 10, $"yalnız {calls} log çağrısı tarandı; tarama boş koşuyor olabilir");
        Assert.True(
            violations.Count == 0,
            $"Adres, anahtar ve bağlantı log parametresine girmez (0018 C5, D25):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// `0018` AC-99 · C12 — `Notifications` yaprak kalır: olay YAYINLAMAZ (outbox'ı yok, yazıcı
    /// kullanmaz) ve hiçbir modül onun sözleşmesine bağlanmaz (yalnız Host kurulum ayarları).
    /// </summary>
    [Fact(DisplayName = "0018/AC-99: Yeni migration'lar yalnız `notifications` şemasına dokunur; mimari testler (modül sınırı, `Notifications`'ın olay yayınlamaması) yeşildir")]
    public void YayinlamazOlay_VeKimseBaglanmaz()
    {
        List<string> violations = [];

        foreach (string file in NotificationsFiles())
        {
            foreach (string line in SolutionLayout.CodeLines(file))
            {
                if (line.Contains("IOutboxDbContext", StringComparison.Ordinal)
                    || line.Contains("OutboxWriter<", StringComparison.Ordinal)
                    || line.Contains("OutboxMessages", StringComparison.Ordinal))
                {
                    violations.Add($"{SolutionLayout.Relative(file)}: {line.Trim()}");
                }
            }
        }

        foreach (string project in SolutionLayout.ProjectFiles())
        {
            if (!SolutionLayout.IsModuleProject(project) || SolutionLayout.ModuleNameOf(project) == Module)
            {
                continue;
            }

            if (SolutionLayout.ProjectReferences(project).Any(reference =>
                    Path.GetFileName(reference).StartsWith("Notifications", StringComparison.Ordinal)))
            {
                violations.Add($"{SolutionLayout.Relative(project)} → Notifications");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Notifications olay yayınlamaz ve hiçbir modül ona bağlanmaz (0009 C11, 0018 C12):{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> NotificationsFiles() =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.ModuleNameOf(file) == Module);

    /// <summary>
    /// Başka modüllerin `Contracts`'ında tanımlı ve `Notifications`'ın kodunda adı geçen
    /// arayüzler. `Notifications`'ın kendi sözleşmesi ve `BuildingBlocks` sayılmaz.
    /// </summary>
    private static IReadOnlyList<string> ConsumedInterfaces()
    {
        HashSet<string> declared = new(StringComparer.Ordinal);

        foreach (string file in SolutionLayout.SourceFiles())
        {
            if (SolutionLayout.ModuleNameOf(file) == Module
                || !SolutionLayout.Relative(file).Contains(".Contracts/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match match in PublicInterfaceDeclaration().Matches(File.ReadAllText(file)))
            {
                declared.Add(match.Groups["name"].Value);
            }
        }

        string notifications = string.Join('\n', NotificationsFiles().SelectMany(SolutionLayout.CodeLines));

        return [.. declared
            .Where(name => Regex.IsMatch(notifications, $@"\b{Regex.Escape(name)}\b", RegexOptions.None, TimeSpan.FromSeconds(2)))
            .Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// `Notifications`'ın tükettiği sorguların GERÇEKLEŞTİRİMLERİ — `0018` AC-83 ile arayüz
    /// listesi de koddan türetilir (<see cref="ConsumedInterfaces"/>).
    /// </summary>
    private static IEnumerable<string> ConsumedQueryImplementations()
    {
        IReadOnlyList<string> interfaces = ConsumedInterfaces();

        return SolutionLayout.SourceFiles().Where(file =>
            SolutionLayout.ModuleNameOf(file) != Module
            && !SolutionLayout.Relative(file).Contains(".Contracts/", StringComparison.Ordinal)
            && interfaces.Any(name =>
                ImplementationDeclaration(File.ReadAllText(file), name)));
    }

    /// <summary>
    /// Şema ADINI değil, şemaya ERİŞİMİ arar. İki biçim var: SQL'de nitelenmiş tablo adı
    /// (<c>leads.leads</c>) ve EF'in şema argümanı (tırnak içinde şema adı).
    ///
    /// <para>Kaba bir "leads." aramasının üç yanlış pozitifi vardı ve üçü de öğreticiydi:
    /// <c>leads.FindAsync(...)</c> bir DEĞİŞKENDİR, şema değil; seed migration'ının
    /// İngilizce şablon metninde geçen "reached the dealership." ise düpedüz düzyazı.
    /// Tablo adının küçük harfle başlaması bu üçünü de ayırıyor — gürültülü bir mimari
    /// test, kapattığından daha çok şey açar.</para>
    ///
    /// <para>Desen `SolutionLayout`'ta yaşıyor: aynı kuralı `0011`/AC-35 de koruyor ve
    /// ikinci kopya yazıldığında daha zayıf çıktı (bir sonda yakaladı). Ortak desen tek
    /// bir yerde yaşar; modüller onu çağırır (`0007`/AC-9'un aynı ilkesi).</para>
    /// </summary>
    private static Regex SchemaReference(string schema) => SolutionLayout.SchemaReference(schema);

    /// <summary>
    /// Arayüzü GERÇEKLEŞTİREN sınıf mı? Taban listesi sınıf bildiriminde, birincil kurucunun
    /// parantezinden SONRA gelir. Eski desen (<c>(:|,)\s*IAd</c>) yalnız üç arayüzle yetiyordu;
    /// `0018` ile liste koddan türetilince kurucu parametresi olarak arayüzü TÜKETEN sınıfları
    /// (<c>VehicleWriteRules(…, IVehicleModelQueries models)</c>) da gerçekleştirim sanıyordu.
    /// </summary>
    private static bool ImplementationDeclaration(string text, string interfaceName)
    {
        foreach (Match declaration in ClassDeclaration().Matches(text))
        {
            int index = declaration.Index + declaration.Length;

            if (index < text.Length && text[index] == '(')
            {
                int depth = 0;

                for (; index < text.Length; index++)
                {
                    depth += text[index] switch { '(' => 1, ')' => -1, _ => 0 };

                    if (depth == 0)
                    {
                        index++;
                        break;
                    }
                }
            }

            int end = text.IndexOfAny(['{', ';'], index);
            string tail = text[index..(end < 0 ? text.Length : end)];

            if (tail.TrimStart().StartsWith(':')
                && Regex.IsMatch(tail, $@"\b{Regex.Escape(interfaceName)}\b", RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Log çağrısının TAMAMINI çıkarır. Satır satır taramak yetmez: çağrılar çok satırlı
    /// ve kişisel veri tam olarak o alt satırlardan birinde olurdu.
    /// </summary>
    private static IEnumerable<string> LogCalls(string text)
    {
        foreach (Match start in LogCallStart().Matches(text))
        {
            int depth = 0;
            int index = start.Index + start.Length - 1;

            for (; index < text.Length; index++)
            {
                if (text[index] == '(')
                {
                    depth++;
                }
                else if (text[index] == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        break;
                    }
                }
            }

            yield return text[start.Index..Math.Min(index + 1, text.Length)];
        }
    }

    private static string Ozet(string call) =>
        call.Length <= 120 ? call : call[..120] + "…";

    private static IEnumerable<string> MatchingLines(string file, Regex pattern)
    {
        string relative = SolutionLayout.Relative(file);
        string[] lines = File.ReadAllLines(file);

        for (int index = 0; index < lines.Length; index++)
        {
            if (lines[index].TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (pattern.IsMatch(lines[index]))
            {
                yield return $"{relative}:{index + 1}  {lines[index].Trim()}";
            }
        }
    }

    [GeneratedRegex(@"\b(?:MailKit|MimeKit|MimeMessage|SmtpClient|System\.Net\.Mail|SendMailAsync)\b")]
    private static partial Regex EmailSendingCall();

    [GeneratedRegex(@"^\s*using\s+CarGallery\.Modules\.(?<module>\w+)\.Contracts", RegexOptions.Multiline)]
    private static partial Regex ForeignContractUsing();

    [GeneratedRegex(@"\blogger\s*\.\s*Log\w*\s*\(")]
    private static partial Regex LogCallStart();

    [GeneratedRegex(@"\bclass\s+\w+(?:<[^>]*>)?")]
    private static partial Regex ClassDeclaration();

    [GeneratedRegex(@"\bpublic\s+interface\s+(?<name>I\w+)")]
    private static partial Regex PublicInterfaceDeclaration();

    // `0018` — adres, teyit anahtarı ve bağlantı: alan adı, değişken adı ya da yer tutucu
    // olarak. Gönderenin ve kurulumun adresi de yasak (`PersonalDataToken`'ın gerekçesi).
    [GeneratedRegex(@"\b(?:\w*(?<![cC]ancellation|stopping)Token|token|\w*Link\w*|\w*Baglanti\w*|\w*Email\w*|\w*Address\w*|Recipient\w*|\w*Hash\w*|hash)\b")]
    private static partial Regex LinkOrTokenToken();

    // Gönderenin adresi de yasak: galerinin kendi adresi olsa bile bir log satırına
    // yazılmasının hiçbir faydası yok ve kuralın istisnası onu aşındırırdı.
    [GeneratedRegex(@"\b(?:Recipient|Email|Phone|FullName|\w*Address)\b")]
    private static partial Regex PersonalDataToken();
}
