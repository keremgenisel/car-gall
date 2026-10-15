using System.Text;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

/// <summary>
/// Parametre kümesi DONDURULMUŞTUR (C12/D16) ve bu dosya onu **bağımsız bir gerçeğe
/// karşı** ölçer: vakalar RFC 6238 Ek B'nin yayınlanmış test vektörleridir. Kendi
/// uygulamasını kendi ürettiği kodla sınayan bir test hiçbir şey kanıtlamaz — kümenin
/// yanlışlığı yalnız *"doğru kod reddediliyor"* olarak görünür ve o an hiçbir birim
/// testi kırmızı vermez (D41'in otomatik yarısı).
///
/// Vektörlerin sırrı RFC'nin kendi sırrıdır: ASCII `"12345678901234567890"`. Yayınlanan
/// kodlar sekiz hanelidir; altı haneli karşılıkları aynı ikili değerin `10^6` modudur,
/// yani son altı hanesidir.
/// </summary>
public sealed class ZamanaBagliKodTests
{
    private const string RfcSirriBase32 = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private static readonly byte[] RfcSirri = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory(DisplayName = "0013/AC-2: `otpauth://` adresi dondurulmuş parametre kümesini taşır: `algorithm=SHA1`, `digits=6`, `period=30` ve yapılandırmadan gelen `issuer`")]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    [InlineData(20000000000L, "353130")]
    public void Uyar_Rfc6238EkBVektorlerine(long unixSaniye, string beklenen)
    {
        long adim = TimeBasedCode.StepAt(DateTimeOffset.FromUnixTimeSeconds(unixSaniye));

        Assert.Equal(beklenen, TimeBasedCode.Compute(RfcSirri, adim));
    }

    /// <summary>
    /// Base32 kodlaması da vektörle ölçülüyor: sırrı yanlış kodlayan bir uygulama kendi
    /// içinde tutarlı kalır ve kusuru yalnız gerçek bir doğrulama uygulaması gösterir.
    /// </summary>
    [Fact(DisplayName = "0013/AC-2: `otpauth://` adresi dondurulmuş parametre kümesini taşır: `algorithm=SHA1`, `digits=6`, `period=30` ve yapılandırmadan gelen `issuer`")]
    public void Kodlar_Base32_RfcSirrini()
    {
        TwoFactorSecret sir = TwoFactorSecret.FromBase32(RfcSirriBase32).Value;

        Assert.Equal(RfcSirri, sir.ToBytes());
    }

    [Fact(DisplayName = "0013/AC-20: İçinde bulunulan adımın kodu kabul edilir ve oturum açılır")]
    public void KabulEdilir_IcindeBulunulanAdim()
    {
        long adim = TimeBasedCode.StepAt(An);
        string kod = TimeBasedCode.Compute(RfcSirri, adim);

        Assert.True(TimeBasedCode.TryMatch(RfcSirri, kod, adim, lastAcceptedStep: 0, out long eslesen));
        Assert.Equal(adim, eslesen);
    }

    [Fact(DisplayName = "0013/AC-21: Yanlış kod `401` ve `identity.two_factor_invalid` ile reddedilir")]
    public void Reddedilir_YanlisKod()
    {
        long adim = TimeBasedCode.StepAt(An);

        Assert.False(TimeBasedCode.TryMatch(RfcSirri, "000000", adim, 0, out _));
        Assert.False(TimeBasedCode.TryMatch(RfcSirri, "12345", adim, 0, out _));
        Assert.False(TimeBasedCode.TryMatch(RfcSirri, null, adim, 0, out _));
    }

    [Fact(DisplayName = "0013/AC-22: Penceresi dolmuş kod reddedilir")]
    public void Reddedilir_PenceresiDolmusKod()
    {
        long adim = TimeBasedCode.StepAt(An);
        string kod = TimeBasedCode.Compute(RfcSirri, adim);

        // İki adım (60 sn) sonra aynı kod artık tolerans penceresinde değil.
        Assert.False(TimeBasedCode.TryMatch(RfcSirri, kod, adim + 2, 0, out _));
    }

    [Fact(DisplayName = "0013/AC-24: Saat kayması toleransı **±1 adımdır**: önceki ve sonraki adımın kodu kabul edilir, iki adım önceki kod reddedilir")]
    public void KabulEder_KomsuAdimlari_IkiAdimOncesiniEtmez()
    {
        long adim = TimeBasedCode.StepAt(An);

        Assert.True(TimeBasedCode.TryMatch(
            RfcSirri, TimeBasedCode.Compute(RfcSirri, adim - 1), adim, 0, out long onceki));
        Assert.Equal(adim - 1, onceki);

        Assert.True(TimeBasedCode.TryMatch(
            RfcSirri, TimeBasedCode.Compute(RfcSirri, adim + 1), adim, 0, out long sonraki));
        Assert.Equal(adim + 1, sonraki);

        Assert.False(TimeBasedCode.TryMatch(
            RfcSirri, TimeBasedCode.Compute(RfcSirri, adim - 2), adim, 0, out _));
    }

    /// <summary>
    /// D11 — toleransın açtığı 90 saniyelik pencereyi kapatan kural. Kabul edilen adıma
    /// EŞİT bir adım da reddedilir; "daha küçük" tek başına aynı kodun ikinci kez
    /// geçmesine izin verirdi.
    /// </summary>
    [Fact(DisplayName = "0013/AC-23: Kabul edilen bir kod **ikinci kez** kabul edilmez")]
    public void Reddedilir_YenidenOynatilanKod()
    {
        long adim = TimeBasedCode.StepAt(An);
        string kod = TimeBasedCode.Compute(RfcSirri, adim);

        Assert.True(TimeBasedCode.TryMatch(RfcSirri, kod, adim, 0, out long eslesen));
        Assert.False(TimeBasedCode.TryMatch(RfcSirri, kod, adim, eslesen, out _));

        // Aynı pencerede ÖNCEKİ adımın kodu da artık geçmez: kabul edilen adım ilerledi.
        Assert.False(TimeBasedCode.TryMatch(
            RfcSirri, TimeBasedCode.Compute(RfcSirri, adim - 1), adim, eslesen, out _));
    }

    private static readonly DateTimeOffset An = new(2026, 10, 15, 9, 30, 17, TimeSpan.Zero);
}
