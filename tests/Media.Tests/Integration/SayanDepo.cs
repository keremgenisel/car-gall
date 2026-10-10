using CarGallery.Modules.Media.Application;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Gerçek depoyu sarmalayıp okuma çağrılarını sayar. Depo **gerçek** kalıyor; sayılan şey
/// yalnızca kaç kez çağrıldığı — sahte bir depo "S3'e yazıldı"yı kanıtlamazdı (C11).
/// </summary>
internal sealed class SayanDepo(IObjectStore ic) : IObjectStore
{
    private readonly Dictionary<string, int> okumalar = new(StringComparer.Ordinal);

    internal int OkumaSayisi(string objectKey) =>
        okumalar.TryGetValue(objectKey, out int sayi) ? sayi : 0;

    public Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken) =>
        ic.PutAsync(objectKey, content, contentType, cancellationToken);

    public Task<Stream?> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        okumalar[objectKey] = OkumaSayisi(objectKey) + 1;

        return ic.OpenReadAsync(objectKey, cancellationToken);
    }
}
