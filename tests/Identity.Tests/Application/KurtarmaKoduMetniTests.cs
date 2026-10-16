using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Application;

/// <summary>
/// Kurtarma kodunun METİN biçimi: üretim, normalize etme ve *"aramaya değer mi"* elemesi.
///
/// <para>Eleme bir dış bulgunun bedeli. Doğrulama yolu girilen kodu saklanan on özetin
/// hepsiyle karşılaştırmak zorunda (tuz hesaba özel) ve eleme yalnız BOŞ girdiyi
/// düşürüyordu: altı haneli her yanlış zamana bağlı kod on PBKDF2 hesabı ödetiyordu —
/// D2/D38'in ölçüp kabul ettiği maliyetin hiç gerekmediği yerde ödenmesi.</para>
///
/// <para>Testin ağırlık merkezi tersinde: elemenin hiçbir GEÇERLİ kodu düşürmediğini
/// ölçüyor. Hızlı ama yanlış bir eleme, kurtarma kodunu tamamen kullanılamaz kılardı ve
/// bunu yalnız kodunu üretemeyen personel fark ederdi — yani en kötü anda.</para>
/// </summary>
public sealed class KurtarmaKoduMetniTests
{
    /// <summary>Tek bir kodun şans eseri geçmesi ölçüm değildir.</summary>
    private const int Ornek = 200;

    [Fact(DisplayName = "0013/AC-36: Üretilen her kurtarma kodu elemeden geçer; eleme hiçbir geçerli kodu düşürmez")]
    public void Gecer_UretilenHerKod_Elemeden()
    {
        for (int sayac = 0; sayac < Ornek; sayac++)
        {
            string kod = RecoveryCodeText.Generate();

            Assert.True(
                RecoveryCodeText.IsWellFormed(RecoveryCodeText.Normalize(kod)),
                $"Üretilen kod elemeyi geçmedi: {kod}");
        }
    }

    /// <summary>
    /// Personel kodu elle yazıyor (D31: yalnız ekranda, bir kez): tireleri birebir
    /// yazmasını beklemek, tek kullanımlık bir kodu bir yazım biçimine feda etmek olurdu.
    /// </summary>
    [Theory(DisplayName = "0013/AC-36: Elle yazılmış biçimler normalize edildikten sonra elemeyi geçer")]
    [InlineData("abcd-efgh-jklm-npqr")]
    [InlineData("ABCD EFGH JKLM NPQR")]
    [InlineData("  abcdefghjklmnpqr  ")]
    [InlineData("ABCDEFGHJKLMNPQR")]
    public void Gecer_ElleYazilmisBicim(string yazilan)
    {
        Assert.True(RecoveryCodeText.IsWellFormed(RecoveryCodeText.Normalize(yazilan)));
    }

    /// <summary>
    /// Elemenin asıl işi: altı haneli bir zamana bağlı kod 16 simgelik bir koda ASLA eşit
    /// olamaz. `TimeBasedCode` kodun kaç hane olduğunu kendi sözleşmesinde tutuyor (C12),
    /// yani iki biçimin uyuşmazlığı bir tesadüf değil.
    /// </summary>
    [Fact(DisplayName = "0013/AC-36: Zamana bağlı kodun biçimi elemeden geçmez; kurtarma kodu olarak hiç aranmaz")]
    public void Gecmez_ZamanaBagliKodunBicimi()
    {
        string anlikKod = TimeBasedCode.Compute(
            TwoFactorSecret.New().ToBytes(),
            TimeBasedCode.StepAt(DateTimeOffset.UnixEpoch));

        Assert.False(RecoveryCodeText.IsWellFormed(RecoveryCodeText.Normalize(anlikKod)));
        Assert.False(RecoveryCodeText.IsWellFormed(RecoveryCodeText.Normalize("000000")));
    }

    /// <summary>
    /// Uzunluk tek başına yetmez: alfabe `0/O` ve `1/I` karışmasın diye seçildi ve o
    /// harfleri taşıyan bir girdi üretilmiş bir kod olamaz.
    /// </summary>
    [Theory(DisplayName = "0013/AC-36: Uzunluğu tutsa da alfabe dışı simge taşıyan girdi elemeden geçmez")]
    [InlineData("")]
    [InlineData("ABCDEFGHJKLMNPQ")]
    [InlineData("ABCDEFGHJKLMNPQRS")]
    [InlineData("0BCDEFGHJKLMNPQR")]
    [InlineData("1BCDEFGHJKLMNPQR")]
    [InlineData("IBCDEFGHJKLMNPQR")]
    [InlineData("OBCDEFGHJKLMNPQR")]
    public void Gecmez_BicimiTutmayanGirdi(string girdi)
    {
        Assert.False(RecoveryCodeText.IsWellFormed(RecoveryCodeText.Normalize(girdi)));
    }
}
