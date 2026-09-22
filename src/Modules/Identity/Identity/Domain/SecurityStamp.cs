namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Personelin oturumlarının ortak geçerlilik damgası. Oturum satırı kurulduğu andaki
/// damgayı saklar; damga değişince o personelin **tüm** oturumları bir sonraki istekte
/// geçersizleşir (R11, AC-22).
///
/// Neden ayrı bir damga: parolası sızmış personel parolasını değiştirdiğinde saldırganın
/// açık oturumu da düşmelidir. Aksi hâlde parola değiştirmek hiçbir şey çözmez — yenileme
/// hakkı yaşamaya devam eder.
///
/// Çıkış bunu KULLANMAZ: çıkış yalnız o cihazın oturumunu kapatır (R5, AC-19). İki ayrı
/// mekanizma, iki ayrı kriter.
/// </summary>
internal readonly record struct SecurityStamp(Guid Value)
{
    internal static SecurityStamp New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
