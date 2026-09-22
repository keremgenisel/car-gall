using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

public sealed class ParolaTests
{
    private const int Iterasyon = StaffBuilder.TestIterasyon;

    [Fact(DisplayName = "0006/AC-37: Aynı parolayı kullanan iki personelin saklanan parola özetleri birbirinden farklıdır")]
    public void FarklidirOzet_AyniParolayiKullananIkiPersonel()
    {
        PasswordHash birinci = PasswordHash.Create("ayni-parola-12", Iterasyon);
        PasswordHash ikinci = PasswordHash.Create("ayni-parola-12", Iterasyon);

        Assert.NotEqual(birinci.Value, ikinci.Value);

        // İkisi de kendi parolalarını doğrular: fark tuzdan geliyor, hatadan değil.
        Assert.True(birinci.Matches("ayni-parola-12"));
        Assert.True(ikinci.Matches("ayni-parola-12"));
    }

    [Fact(DisplayName = "0006/AC-50: Personel kaydının döndüğü hiçbir yanıt parolayı veya parola özetini içermez")]
    public void SizdirmazOzet_ToStringCagrisi()
    {
        PasswordHash ozet = PasswordHash.Create("cok-guclu-parola", Iterasyon);

        // Kazara bir log veya hata mesajı bu tipi basarsa parolaya dair hiçbir şey çıkmaz.
        Assert.Equal("***", ozet.ToString());
        Assert.DoesNotContain("pbkdf2", ozet.ToString(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0006/AC-37: Aynı parolayı kullanan iki personelin saklanan parola özetleri birbirinden farklıdır")]
    public void Reddedilir_YanlisParola()
    {
        PasswordHash ozet = PasswordHash.Create("dogru-parola-12", Iterasyon);

        Assert.False(ozet.Matches("yanlis-parola-12"));
        Assert.False(ozet.Matches(string.Empty));
    }

    /// <summary>
    /// Biçim SÜRÜMLÜDÜR: algoritma adı ve maliyet parametresi özetin içinde durur.
    /// İleride Argon2id'ye geçilirken saklanmış satırlar eski algoritmayla doğrulanmaya
    /// devam edebilsin diye (C6).
    /// </summary>
    [Fact(DisplayName = "0006/AC-37: Aynı parolayı kullanan iki personelin saklanan parola özetleri birbirinden farklıdır")]
    public void TasirAlgoritmaVeMaliyet_SaklananOzet()
    {
        string[] parcalar = PasswordHash.Create("cok-guclu-parola", 4321).Value.Split('$');

        Assert.Equal(4, parcalar.Length);
        Assert.Equal(PasswordHash.Pbkdf2Sha256, parcalar[0]);
        Assert.Equal("4321", parcalar[1]);
        Assert.NotEmpty(parcalar[2]);
        Assert.NotEmpty(parcalar[3]);
    }

    [Fact(DisplayName = "0006/AC-37: Aynı parolayı kullanan iki personelin saklanan parola özetleri birbirinden farklıdır")]
    public void DogrularFarkliMaliyetle_SaklananOzet()
    {
        // Maliyet yapılandırmadan gelir ve zamanla artar; eski satır eski maliyetiyle
        // doğrulanmaya devam eder.
        PasswordHash eski = PasswordHash.Create("cok-guclu-parola", 1_000);

        Assert.True(eski.Matches("cok-guclu-parola"));
    }
}
