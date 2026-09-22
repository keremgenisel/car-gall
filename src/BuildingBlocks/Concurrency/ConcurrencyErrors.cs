namespace CarGallery.BuildingBlocks.Concurrency;

/// <summary>
/// Eşzamanlılık kaybının kararlı kodu (0007 R6).
///
/// Tek kod, modül başına değil: kaybın "alanı" gerçekten mekanizmadır ve hangi modülde
/// olduğu kullanıcıya hiçbir şey söylemez — tepkisi her yerde aynıdır, tekrar dene.
/// İki ayrı kod olsaydı, bugün hiçbir ekranın ulaşamadığı bir kodun da üç dilde çevirisi
/// yazılmak zorunda kalırdı; kullanılmayan çeviri anahtarı, ölü kodun katalog hâlidir.
///
/// **Kural gereği redden ayrıdır ve ayrı kalmalıdır** (0007/AC-12): kural gereği red
/// KALICIDIR — kullanıcıya tekrar denemesi söylenirse sonsuza kadar denemesi söylenmiş
/// olur. Eşzamanlılık kaybı geçicidir ve tekrar denemek gerçekten işe yarar.
///
/// Bu bir iş kuralı değil, bir mekanizma sonucudur; bu yüzden `BuildingBlocks`'ta
/// durabilir (Yasak Listesi 11).
/// </summary>
public static class ConcurrencyErrors
{
    /// <summary>docs/conventions.md § 5 — `&lt;alan&gt;.&lt;durum&gt;`, snake_case.</summary>
    public const string Lost = "concurrency.lost";
}
