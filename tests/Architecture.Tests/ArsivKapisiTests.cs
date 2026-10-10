using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// Kapı: <c>specs/done/</c> altına taşınan bir spec'te açık DoD kutusu, boş SCORECARD
/// hücresi veya eksik şablon maddesi bulunmaz (ADR <c>0008</c> / K8).
///
/// <para><b>Hangi boşluğu kapatıyor.</b> Bu depoda <c>specs/</c> altını okuyan HİÇBİR CI
/// kapısı yoktu (ölçüldü: <c>grep -rn specs .github/workflows/ scripts/</c> → sıfır
/// eşleşme). O boşluktan üç sınıf hata geçti ve hiçbiri görülmedi: <c>0010a</c>'nın
/// kapanış ölçümleri PR #36'da commit'e hiç girmedi ve BEŞ GÜN kayıp durdu; <c>0008</c>
/// ile <c>0009</c> "Tamamlandı" damgasıyla ama AÇIK merge kutusuyla duruyor; zorunlu dış
/// gözden geçirme maddesi <c>0007</c> ile <c>0008</c>'de hiç yok. Ayrıntısı:
/// <c>specs/done/SCORECARD-ANALIZ-002.md</c> § S4.</para>
///
/// <para><b>KAPI DÜZELTMEZ, BİLDİRİR.</b> Tarihli bir kaydı otomatik düzelten bir kapı,
/// bu projenin "kapanmış kayıt yeniden yazılmaz" ilkesini ihlal ederdi — 001'in kendi
/// metni bunu yazıyor ve PR #42 de aynı ilkeyi uyguladı.</para>
///
/// <para><b>Madde varlığı İŞARETLEYİCİYLE ölçülür, metinle değil.</b> İlk tasarım
/// maddenin metninde anahtar ifade arıyordu ve ÖLÇÜLDÜĞÜNDE beş kartta yanlış pozitif
/// verdi: "Arayüz kriteri <b>yok</b>; görsel kanıt bu spec için uygulanabilir değildir"
/// diyen bir madde `docs/evidence` yolunu hiç anmaz. Serbest metinde ifade aramak,
/// maddeyi <i>silmekle</i> <i>yeniden yazmayı</i> ayırt edemez — oysa K4'ün yasakladığı
/// şey yalnızca silmek. Bu yüzden şablonun her DoD maddesi bir
/// <c>&lt;!-- DoD:Tn --&gt;</c> işaretleyicisi taşır ve kapı onu arar. İşaretleyici
/// listesi <b>şablondan okunur</b>: şablona madde eklendiği anda kapı onu da arar,
/// güncellenmesi gereken ikinci bir liste yok.</para>
///
/// <para><b>Kesim tarihi ve neden gerekli.</b> Kapı yalnız <see cref="KesimdenOnceKapananlar"/>
/// listesinde OLMAYAN spec'lere kırmızı verir. O listedeki on iki kart 2026-10-10'dan önce,
/// ADR 0008'in kuralları yürürlüğe girmeden kapandı; işaretleyicileri de yok ve
/// eklenemez (düzeltmek yasak). Kapı onlara kırmızı verseydi ilk koşuda on iki kırmızı
/// çıkar ve kapı ya kaldırılır ya susturulurdu. <b>Susturulmuş kapı, olmayan kapıdan daha
/// kötüdür — var sanılır.</b> Muaf kartlarda yanlış pozitifi olmayan iki denetim yine de
/// koşar ve sonuç test çıktısına YAZILIR
/// (<see cref="Bildirir_MuafKartlarinKapanmamisKayitlarini"/>).</para>
///
/// <para><b>Liste kural olarak KÜÇÜLÜR; büyümesinin tek meşru hâli yazılıdır.</b> Bir ad
/// eklemek, kapıyı bir spec için kapatmak demektir ve diff'te görünür.
/// <see cref="Canlidir_MuafiyetListesi"/> listedeki her adın gerçekten var olduğunu sınar;
/// bir spec yeniden adlandırılırsa liste sessizce çürümez.</para>
///
/// <para><b>Tek meşru büyüme: şablon, kart kapandıktan SONRA madde kazandığında.</b>
/// İşaretleyici listesi şablondan canlı okunduğu için, şablona bugün bir
/// <c>DoD:T14</c> eklemek kesimden sonra kapanmış kartları da kırmızıya çevirir — sondayla
/// ölçüldü (çıkış kodu 1). O kırmızıyı söndürmenin iki yolu vardı ve ikisi de yasaktı:
/// kapanmış kartı düzenlemek (tarihli kayıt) ve bu listeyi büyütmek. İkincisi artık
/// yasak değil: kartın adı listeye eklenir ve <b>satırın yanına gerekçe yazılır</b> —
/// hangi işaretleyici, hangi tarihte eklendi. Gerekçesiz bir ad, muafiyetin sessizce
/// genişlemesidir (ADR 0008 / K8).</para>
/// </summary>
public sealed class ArsivKapisiTests(ITestOutputHelper cikti)
{
    /// <summary>
    /// 2026-10-10'dan ÖNCE kapanan spec'ler. ADR 0008'in kuralları bu kartlara geriye dönük
    /// uygulanmaz ve kartları DÜZELTİLMEZ — tarihli kayıttır. Liste kural olarak küçülür;
    /// büyümesinin tek meşru hâli, şablonun kart kapandıktan SONRA madde kazanması ve
    /// eklenen satırın gerekçesini taşımasıdır (sınıf özetine bkz.).
    /// </summary>
    private static readonly string[] KesimdenOnceKapananlar =
    [
        "0001-vitrin-ilan-listesi-ve-kasa-tipi-daraltma.md",
        "0002-arac-detay.md",
        "0003-galeri-profili-ve-subeler.md",
        "0005-talep-olusturma.md",
        "0006-personel-kimligi-ve-oturum.md",
        "0007-eszamanli-yazma-ve-invariant-korumasi.md",
        "0008-personel-yonetimi-ekrani-eylemler.md",
        "0009-talep-bildirimi.md",
        "0010a-panel-arac-kaydi.md",
        "0010b-panel-arac-duzenleme-ve-liste.md",
        "frontend-001-uygulama-kabugu-ve-standarda-uyum.md",
        "frontend-002-gorsel-dil-ve-tema.md",
    ];

    private static string ArsivDizini => Path.Combine(SolutionLayout.Root, "specs", "done");

    private static string SablonYolu => Path.Combine(SolutionLayout.Root, "specs", "TEMPLATE.md");

    [Fact(DisplayName = "Arşiv kapısı: kesimden sonra kapanan spec'lerde açık kutu, boş ölçüm veya eksik şablon maddesi yok")]
    public void Temizdir_KesimdenSonraKapananSpecler()
    {
        IReadOnlyList<string> isaretleyiciler = SablonunIsaretleyicileri();
        IReadOnlyList<string> denetlenenler = [.. ArsivdekiSpecler().Where(yol => !Muaf(yol))];

        List<string> bulgular = [];
        foreach (string yol in denetlenenler)
        {
            bulgular.AddRange(KapanmamisKayitlar(yol));
            bulgular.AddRange(EksikMaddeler(yol, isaretleyiciler));
        }

        cikti.WriteLine(
            $"Kesimden sonra kapanan spec: {denetlenenler.Count} · "
            + $"muaf: {KesimdenOnceKapananlar.Length} · "
            + $"şablon maddesi: {isaretleyiciler.Count}.");

        Assert.True(
            bulgular.Count == 0,
            "Arşivlenmiş spec'lerde kapanmamış kayıt var:" + Environment.NewLine
            + string.Join(Environment.NewLine, bulgular));
    }

    [Fact(DisplayName = "Arşiv kapısı: şablonun her DoD maddesi bir DoD:Tn işaretleyicisi taşıyor")]
    public void Isaretlidir_SablonunHerDoDMaddesi()
    {
        string dod = BolumMetni(File.ReadAllText(SablonYolu), "Definition of Done");

        int maddeSayisi = Regex.Matches(dod, @"^- \[[ x]\]", RegexOptions.Multiline).Count;
        IReadOnlyList<string> isaretleyiciler = SablonunIsaretleyicileri();

        Assert.True(maddeSayisi > 0, "`specs/TEMPLATE.md` § Definition of Done okunamadı.");

        // İşaretleyicisi olmayan bir madde, kapının HİÇ aramadığı maddedir: kapı sessizce
        // küçülür ve bunu hiçbir şey söylemez. Eşitlik bu yüzden ayrı bir iddia.
        Assert.Equal(maddeSayisi, isaretleyiciler.Count);
        Assert.Equal(isaretleyiciler.Count, isaretleyiciler.Distinct().Count());
    }

    [Fact(DisplayName = "Arşiv kapısı: muafiyet listesindeki her ad gerçekten arşivde duruyor")]
    public void Canlidir_MuafiyetListesi()
    {
        IReadOnlyList<string> arsivdekiAdlar = [.. ArsivdekiSpecler().Select(yol => Path.GetFileName(yol)!)];

        string[] bulunmayan = [.. KesimdenOnceKapananlar.Where(ad => !arsivdekiAdlar.Contains(ad))];

        Assert.True(
            bulunmayan.Length == 0,
            "Muafiyet listesi çürüdü — şu adlar `specs/done/` altında yok: "
            + string.Join(", ", bulunmayan)
            + ". Spec yeniden adlandırıldıysa liste de güncellenir; muafiyet sessizce "
            + "genişlemesin diye bu ayrıca sınanıyor.");

        // Kapının HİÇ dosya görmediği bir dünyada her iddiası boş yere yeşil kalırdı.
        Assert.NotEmpty(arsivdekiAdlar);
    }

    [Fact(DisplayName = "Arşiv kapısı: muaf kartların kapanmamış kayıtları test çıktısına yazılır (kırmızı vermez)")]
    public void Bildirir_MuafKartlarinKapanmamisKayitlarini()
    {
        StringBuilder rapor = new();
        int toplam = 0;

        // Muaf kartlarda YALNIZ işaretleyiciden bağımsız iki denetim koşar — açık kutu ve
        // boş ölçüm. Eksik madde denetimi koşmaz: o kartlarda işaretleyici hiç yok ve
        // hepsini "eksik" diye bildirmek 156 satır gürültü üretirdi.
        foreach (string yol in ArsivdekiSpecler().Where(Muaf))
        {
            foreach (string bulgu in KapanmamisKayitlar(yol))
            {
                toplam++;
                rapor.AppendLine("  " + bulgu);
            }
        }

        cikti.WriteLine(
            toplam == 0
                ? "Muaf kartlarda kapanmamış kayıt yok."
                : $"Muaf kartlarda {toplam} kapanmamış kayıt — DÜZELTİLMEZ, tarihli kayıttır "
                  + "(ADR 0008 / K8):" + Environment.NewLine + rapor.ToString().TrimEnd());

        // Bu test bilerek iddiasızdır: işi bildirmek, kırmızı vermek değil.
        Assert.True(true);
    }

    /// <summary>
    /// Açık DoD kutusu, EKSİK SCORECARD bölümü ve boş SCORECARD değer hücresi — üçünün de
    /// yanlış pozitifi yok.
    /// </summary>
    private static IReadOnlyList<string> KapanmamisKayitlar(string yol)
    {
        string ad = Path.GetFileName(yol);
        string metin = File.ReadAllText(yol);
        List<string> bulgular = [];

        int acikKutu = Regex.Matches(metin, @"^- \[ \]", RegexOptions.Multiline).Count;
        if (acikKutu > 0)
        {
            bulgular.Add($"{ad}: {acikKutu} açık DoD kutusu — arşivlenmiş spec'te kutu açık kalmaz.");
        }

        // SCORECARD'ın TAMAMEN yokluğu, bir hücresinin boş olmasından daha kötüdür — ve kapı
        // onu GÖRMÜYORDU: `BolumMetni` başlığı bulamazsa boş dize döner, `BosOlcumler` de boş
        // dizeden hiç bulgu üretmez. Sondayla ölçüldü: 13 işaretleyicisi tam, bütün kutuları
        // `[x]`, ama § SCORECARD bölümü HİÇ olmayan bir kart kapıdan yeşil geçiyordu.
        // `specs/done/README.md` net: "SCORECARD boş bırakılmış spec taşınmaz."
        string scorecard = BolumMetni(metin, "SCORECARD");

        if (!Regex.IsMatch(scorecard, @"^\|\s*\*\*", RegexOptions.Multiline))
        {
            bulgular.Add(
                $"{ad}: § SCORECARD bölümü yok ya da hiç ölçüt satırı taşımıyor — boş "
                + "bırakılmış SCORECARD'la spec arşive taşınmaz.");
        }
        else
        {
            foreach (string olcut in BosOlcumler(scorecard))
            {
                bulgular.Add($"{ad}: SCORECARD'da `{olcut}` ölçümünün değer hücresi BOŞ.");
            }
        }

        return bulgular;
    }

    private static IReadOnlyList<string> EksikMaddeler(string yol, IReadOnlyList<string> isaretleyiciler)
    {
        string dod = BolumMetni(File.ReadAllText(yol), "Definition of Done");

        string[] eksik = [.. isaretleyiciler.Where(im => !dod.Contains(im, StringComparison.Ordinal))];

        return eksik.Length == 0
            ? []
            : [
                $"{Path.GetFileName(yol)}: şablonun {eksik.Length} DoD maddesi yok "
                + $"({string.Join(", ", eksik)}) — madde silinemez, uygulanmıyorsa \"yok\" "
                + "diye cevaplanır ve işaretleyicisi kalır."
            ];
    }

    /// <summary>
    /// Şablonun DoD maddelerindeki <c>DoD:Tn</c> işaretleyicileri. Liste burada SABİT
    /// DEĞİL: şablondan okunur, böylece şablona eklenen madde kapıya da eklenir.
    /// </summary>
    private static IReadOnlyList<string> SablonunIsaretleyicileri()
    {
        string dod = BolumMetni(File.ReadAllText(SablonYolu), "Definition of Done");

        return
        [
            .. Regex.Matches(dod, @"DoD:T\d+").Select(eslesme => eslesme.Value)
        ];
    }

    /// <summary>
    /// SCORECARD tablosundaki <c>| **Ölçüt** | değer | not |</c> satırlarından değer hücresi
    /// boş olanların adını verir. Boş hücre bir ortalamada da bir <c>grep</c>'te de sessizce
    /// kaybolur; <c>frontend-002</c>'nin <c>Kaçan hata</c> hücresi tam böyle kayboldu.
    /// </summary>
    private static IEnumerable<string> BosOlcumler(string scorecard)
    {
        foreach (Match satir in Regex.Matches(
            scorecard, @"^\|\s*\*\*(?<olcut>[^|*][^|]*?)\*\*\s*\|(?<deger>[^|]*)\|", RegexOptions.Multiline))
        {
            if (satir.Groups["deger"].Value.Trim().Length == 0)
            {
                yield return satir.Groups["olcut"].Value.Trim();
            }
        }
    }

    /// <summary>Verilen <c>## Başlık</c>'tan sonraki bölümün metni; bir sonraki <c>## </c> başlığında durur.</summary>
    private static string BolumMetni(string metin, string baslik)
    {
        Match eslesme = Regex.Match(
            metin,
            @"^## " + Regex.Escape(baslik) + @"\s*$(?<govde>.*?)(?=^## |\Z)",
            RegexOptions.Multiline | RegexOptions.Singleline);

        return eslesme.Success ? eslesme.Groups["govde"].Value : string.Empty;
    }

    private static bool Muaf(string yol) => KesimdenOnceKapananlar.Contains(Path.GetFileName(yol));

    private static IReadOnlyList<string> ArsivdekiSpecler() =>
        Directory.Exists(ArsivDizini)
            ? [.. Directory.EnumerateFiles(ArsivDizini, "*.md")
                .Where(yol => !Path.GetFileName(yol).StartsWith("README", StringComparison.Ordinal))
                .Where(yol => !Path.GetFileName(yol).StartsWith("SCORECARD-ANALIZ", StringComparison.Ordinal))
                .Order()]
            : [];
}
