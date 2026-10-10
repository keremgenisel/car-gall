namespace CarGallery.Modules.Media.Application;

/// <summary>
/// Depolama portu. C3 "S3-uyumlu" diyor; <b>hangi</b> S3 uyumlu sağlayıcı olduğu
/// `Infrastructure`'ın bilgisidir ve bu arayüz onu bilmez.
///
/// <para>Port olmasının ölçülmüş bedeli var: üretim R2, geliştirme ve test LocalStack'in
/// S3'ü. Adaptörü değiştirmek tek sınıfı değiştirir; iş kuralı dokunulmadan kalır.</para>
/// </summary>
internal interface IObjectStore
{
    Task PutAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Nesne yoksa <c>null</c> döner — istisna fırlatmaz. Filigran kaynağının yokluğu
    /// BEKLENEN bir başarısızlıktır (AC-18) ve beklenen başarısızlık istisnayla taşınmaz
    /// (`docs/conventions.md` § 5).
    /// </summary>
    Task<Stream?> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
}
