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

    private static IEnumerable<string> NotificationsFiles() =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.ModuleNameOf(file) == Module);

    /// <summary>
    /// `Notifications`'ın tükettiği üç sorgunun GERÇEKLEŞTİRİMLERİ. Arayüz adından
    /// bulunuyor: sözleşme yüzeyi değişirse bu liste de kendiliğinden değişir ve testin
    /// elle güncellenmesi gereken bir adımı kalmaz.
    /// </summary>
    private static IEnumerable<string> ConsumedQueryImplementations()
    {
        string[] interfaces = ["ILeadNotificationQueries", "IBranchContactQueries", "IListingSlugQueries"];

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

    private static bool ImplementationDeclaration(string text, string interfaceName) =>
        Regex.IsMatch(
            text,
            $@"(?::|,)\s*{Regex.Escape(interfaceName)}\b",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));

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

    // Gönderenin adresi de yasak: galerinin kendi adresi olsa bile bir log satırına
    // yazılmasının hiçbir faydası yok ve kuralın istisnası onu aşındırırdı.
    [GeneratedRegex(@"\b(?:Recipient|Email|Phone|FullName|\w*Address)\b")]
    private static partial Regex PersonalDataToken();
}
