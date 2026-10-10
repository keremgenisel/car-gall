using Amazon.S3;
using Amazon.S3.Model;
using CarGallery.Modules.Media.Application;
using System.Net;

namespace CarGallery.Modules.Media.Infrastructure;

/// <summary>
/// C3 — S3 uyumlu depolama. Üretimde R2, geliştirme ve testte LocalStack'in S3'ü.
///
/// <para><b>İstemci tek yerde kuruluyor</b> ve <c>ForcePathStyle</c> yapılandırmadan
/// geliyor: R2 sanal-host adreslemesi kullanıyor, yerel sağlayıcılar yol tabanlı. Yanlış
/// seçim "kova bulunamadı" olarak görünür ve kimlik hatası sanılır — ölçtüm.</para>
/// </summary>
internal sealed class S3ObjectStore : IObjectStore, IDisposable
{
    private readonly AmazonS3Client client;
    private readonly string bucket;

    internal S3ObjectStore(MediaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        client = new AmazonS3Client(
            options.StorageAccessKey,
            options.StorageSecretKey,
            new AmazonS3Config
            {
                ServiceURL = options.StorageServiceUrl.ToString(),
                ForcePathStyle = options.StorageForcePathStyle,
                AuthenticationRegion = options.StorageRegion,
            });

        bucket = options.StorageBucket;
    }

    public async Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken) =>
        await client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucket,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
            },
            cancellationToken);

    /// <summary>
    /// Nesne yoksa <c>null</c>; istisna fırlatmaz. Filigranın yokluğu BEKLENEN bir
    /// başarısızlıktır (AC-18) ve beklenen başarısızlık istisnayla taşınmaz.
    /// <c>NotFound</c> dışındaki hatalar yukarı gider: kimlik hatasını "dosya yok" diye
    /// okumak, yapılandırma hatasını bir tur hatası sanmak demekti.
    /// </summary>
    public async Task<Stream?> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            GetObjectResponse yanit = await client.GetObjectAsync(bucket, objectKey, cancellationToken);
            MemoryStream tampon = new();
            await yanit.ResponseStream.CopyToAsync(tampon, cancellationToken);
            yanit.Dispose();
            tampon.Position = 0;

            return tampon;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose() => client.Dispose();
}
