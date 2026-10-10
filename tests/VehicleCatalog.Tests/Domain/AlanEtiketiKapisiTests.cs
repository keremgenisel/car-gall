using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// Kapı: kayıt defterindeki her opsiyonel alanın, var olan HER çeviri kataloğunda etiketi
/// vardır.
///
/// <para><b>Hangi hatayı tutuyor.</b> Form etiketi `vehicle.field.&lt;anahtar&gt;`
/// anahtarından geliyor (`OptionalFieldInput.tsx`). Ham anahtar ziyaretçiye
/// gösterilmediği için çevirisi olmayan anahtarda `t()` BOŞ DİZE dönüyor ve ekran
/// ETİKETSİZ bir giriş kutusu basıyor — sessizce. `VehicleFieldDefinitions`'a alan
/// ekleyip çeviri eklemeyi unutmak bunu üretmeye yeter.</para>
///
/// <para><b>Neden mevcut çeviri kapısı yetmiyor.</b>
/// `frontend/scripts/check-locale-keys.mjs` katalogları birbirine karşı ölçüyor: üçünde
/// de eksik olan bir anahtar üçünde de eksik olduğu için YEŞİL geçer. Eksikliğin ölçüsü
/// başka bir yerde — kayıt defterinde — yaşıyor.</para>
///
/// <para><b>Neden burada, Node betiğinde değil.</b> Betik anahtarları C# kaynağından
/// düzenli ifadeyle çıkarmak zorunda kalırdı; `VehicleFieldKeys` kendi dosyasında değil,
/// `VehicleFormSettings.cs`'in içinde yaşıyor ve ifade ilgisiz sabitleri de yakalardı.
/// Burada kayıt defterinin KENDİSİ okunuyor — ara bir temsil yok.</para>
///
/// <para><b>Hiçbir dil VARSAYILMIYOR.</b> `BuildingBlocks/Locale` etkin dil listesini
/// dağıtım yapılandırmasına bırakıyor, bu yüzden diller sabit yazılmaz: var olan katalog
/// dosyaları taranır — `check-locale-keys.mjs`'in yaptığının aynısı. O kapı katalogların
/// birbirine eşit olduğunu zaten garanti ediyor; buradaki iş anahtarın HER birinde
/// bulunması.</para>
///
/// <para><b>Modül sınırı.</b> Bu test `frontend/` altındaki bir dosyayı okuyor. Emsali
/// var: `Host.Tests/KurulumTests` repo kökünden `src/Host/appsettings.json` okuyor. Yeni
/// olan tek şey dosyanın cephe tarafında olması ve nedeni kuralın kendisinin iki tarafı
/// birden bağlaması — bir yanı kodda, bir yanı katalogta olan bir kuralı tek taraftan
/// ölçmek mümkün değil.</para>
/// </summary>
public sealed class AlanEtiketiKapisiTests
{
    private const string AnahtarOneki = "vehicle.field.";

    public static TheoryData<string, string> DefterdekiAlanlar()
    {
        TheoryData<string, string> veri = new();

        foreach (VehicleFieldDefinition tanim in VehicleFieldDefinitions.All)
        {
            foreach (string katalog in KatalogDosyalari())
            {
                veri.Add(tanim.FieldKey, katalog);
            }
        }

        return veri;
    }

    [Theory(DisplayName = "Alan etiketi kapısı: kayıt defterindeki her opsiyonel alanın her çeviri kataloğunda etiketi vardır")]
    [MemberData(nameof(DefterdekiAlanlar))]
    public void IcerirEtiketi_DefterdekiHerAlanIcin(string alanAnahtari, string katalogAdi)
    {
        Dictionary<string, string> katalog = Katalog(katalogAdi);
        string cevirAnahtari = AnahtarOneki + alanAnahtari;

        Assert.True(
            katalog.TryGetValue(cevirAnahtari, out string? etiket),
            $"`{cevirAnahtari}` anahtarı {katalogAdi} kataloğunda yok. Kayıt defterine "
            + "(`VehicleFieldDefinitions`) alan eklendiğinde çevirisi de eklenir; eklenmezse "
            + "form etiketsiz bir giriş kutusu basar.");

        Assert.False(
            string.IsNullOrWhiteSpace(etiket),
            $"`{cevirAnahtari}` {katalogAdi} kataloğunda BOŞ. Boş etiket, eksik anahtarla aynı "
            + "ekranı üretir.");
    }

    /// <summary>
    /// İki tarafın da boşalması kapıyı sessizce açardı: `Theory` sıfır vakayla da yeşildir.
    /// Bu testin ölçtüğü şey kapının KENDİSİNİN çalıştığıdır.
    /// </summary>
    [Fact(DisplayName = "Alan etiketi kapısı: kayıt defteri de katalog listesi de boşsa kapı sessizce açılmaz")]
    public void IcerirVaka_Kapi()
    {
        Assert.NotEmpty(VehicleFieldDefinitions.All);
        Assert.NotEmpty(KatalogDosyalari());
    }

    private static List<string> KatalogDosyalari() =>
        [.. Directory.EnumerateFiles(LocaleKlasoru.Value, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];

    private static Dictionary<string, string> Katalog(string katalogAdi)
    {
        string yol = Path.Combine(LocaleKlasoru.Value, $"{katalogAdi}.json");

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(yol))
            ?? throw new InvalidOperationException($"{yol} okunamadı.");
    }

    /// <summary>
    /// Kök, sabit sayıda `..` ile değil YUKARI YÜRÜNEREK bulunuyor: sabit sayı derleme
    /// çıktısının derinliğine bağlıdır ve değiştiğinde test eksik çeviriyi değil kendi
    /// yolunu bildirir.
    /// </summary>
    private static readonly Lazy<string> LocaleKlasoru = new(() =>
    {
        const string Goreceli = "frontend/src/shared/i18n/locales";

        for (DirectoryInfo? dizin = new(AppContext.BaseDirectory); dizin is not null; dizin = dizin.Parent)
        {
            string aday = Path.Combine(dizin.FullName, Goreceli);

            if (Directory.Exists(aday))
            {
                return aday;
            }
        }

        throw new DirectoryNotFoundException(
            $"`{Goreceli}` {AppContext.BaseDirectory} ve üstündeki hiçbir dizinde bulunamadı.");
    });
}
