using CarGallery.Modules.Media.Application;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Belirli bir varyantın yazımını reddeden depo. AC-20'nin "kesilen koşu"sunu GERÇEK
/// kılıyor: ondan önceki türevler kovaya yazılmış oluyor ve ikinci turun üzerine yazacağı
/// bir durum kalıyor.
///
/// <para>Depo gerçek kalıyor; reddedilen yalnız tek bir anahtar — sahte bir depo
/// "S3'e yazıldı"yı kanıtlamazdı (C11).</para>
/// </summary>
internal sealed class ReddedenDepo(IObjectStore ic, string reddedilenVaryant) : IObjectStore
{
    public async Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (objectKey.EndsWith($"/{reddedilenVaryant}.webp", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Sonda: '{objectKey}' yazımı reddedildi.");
        }

        await ic.PutAsync(objectKey, content, contentType, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
        ic.OpenReadAsync(objectKey, cancellationToken);
}
