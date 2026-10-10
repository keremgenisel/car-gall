using Amazon.S3;
using Amazon.S3.Model;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL ve gerçek S3 uyumlu depolama. SQLite in-memory ile EF testi yapılmaz
/// ve sahte bir depo ile depolama testi de yapılmaz: ikisinde de yeşil test yanlış güven
/// verir (`docs/testing.md` § 1).
///
/// <para><b>Depo LocalStack, MinIO değil</b> — ölçüldü: `minio/minio` imajı anonim olarak
/// çekilemiyor (`pull access denied ... repository does not exist`) ve bu bir Docker Hub
/// erişim sorunu değil (aynı makinede `alpine:3.20` indi, `quay.io/minio/minio` 401
/// döndü). C3'ün ÜRETİM kararı değişmedi (S3-uyumlu, R2/MinIO); değişen test ikizidir.</para>
///
/// <para><b>İki konteyner açıyor</b> ve bedeli bilinçli: yükleme yolu hem veritabanına hem
/// depoya yazıyor ve birini sahteleyen test diğerinin sözleşmesini kanıtlamaz.</para>
/// </summary>
public sealed class MedyaOrtami : IAsyncLifetime
{
    internal const string Bucket = "cargallery-media-test";

    internal const string WatermarkKey = "private/brand/watermark.png";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    // İmaj etiketi AÇIKÇA sabitlenmiş. `TreatWarningsAsErrors` açık ve parametresiz
    // kurucu CS0618 veriyor — ama asıl gerekçe o değil: etiketlenmemiş bir imaj,
    // sağlayıcı yeni bir sürüm yayınladığı gün testi sessizce başka bir şeye karşı
    // koşturur. Postgres kabı da aynı sebeple `postgres:17-alpine` yazıyor.
    private readonly LocalStackContainer depo =
        new LocalStackBuilder("localstack/localstack:3.8.1").Build();

    private AmazonS3Client? istemci;

    internal string ConnectionString => postgres.GetConnectionString();

    internal string StorageServiceUrl => depo.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), depo.StartAsync());

        await using MediaDbContext context = CreateContext();
        await context.Database.MigrateAsync();

        istemci = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = StorageServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });

        await istemci.PutBucketAsync(new PutBucketRequest { BucketName = Bucket });
    }

    public async Task DisposeAsync()
    {
        istemci?.Dispose();
        await postgres.DisposeAsync();
        await depo.DisposeAsync();
    }

    /// <param name="interceptor">
    /// AC-23'ü ölçen testler kendi dinleyicisini veriyor: "aynı transaction'da" iddiası
    /// ancak `SaveChanges` çağrılarına bakılarak kanıtlanabilir.
    /// </param>
    internal MediaDbContext CreateContext(IInterceptor? interceptor = null)
    {
        DbContextOptionsBuilder<MediaDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", MediaDbContext.Schema));

        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new MediaDbContext(builder.Options);
    }

    internal MediaSetup Setup(
        long maximumByteSize = 15_728_640,
        long maximumPixelCount = 50_000_000,
        string watermarkObjectKey = WatermarkKey,
        IReadOnlyList<string>? allowedImageFormats = null) =>
        new(
            ConnectionString,
            StorageServiceUrl,
            Bucket,
            StorageAccessKey: "test",
            StorageSecretKey: "test",
            StorageRegion: "us-east-1",
            StorageForcePathStyle: true,
            PublicBaseUrl: $"https://cdn.ornek.test/{Bucket}/",
            WatermarkObjectKey: watermarkObjectKey,
            MaximumByteSize: maximumByteSize,
            MaximumPixelCount: maximumPixelCount,
            AllowedImageFormats: allowedImageFormats ?? ["jpeg", "png", "webp"]);

    internal MediaOptions Options(
        long maximumByteSize = 15_728_640,
        long maximumPixelCount = 50_000_000,
        string watermarkObjectKey = WatermarkKey,
        IReadOnlyList<string>? allowedImageFormats = null)
    {
        Result<MediaOptions> options = MediaOptions.Create(
            Setup(maximumByteSize, maximumPixelCount, watermarkObjectKey, allowedImageFormats));

        Assert.True(options.IsSuccess, $"Test yapılandırması geçersiz: {options.Error.Code}");

        return options.Value;
    }

    internal IObjectStore Store(MediaOptions options) => new S3ObjectStore(options);

    /// <summary>Filigran kaynağını kovaya koyar. Koymayan test AC-18'i sınıyor demektir.</summary>
    internal async Task FiligraniYerlestirAsync(byte[] content, string key = WatermarkKey)
    {
        using MemoryStream govde = new(content, writable: false);

        await istemci!.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = govde,
            ContentType = "image/png",
        });
    }

    internal async Task<byte[]?> NesneOkuAsync(string key)
    {
        try
        {
            using GetObjectResponse yanit = await istemci!.GetObjectAsync(Bucket, key);
            using MemoryStream tampon = new();
            await yanit.ResponseStream.CopyToAsync(tampon);

            return tampon.ToArray();
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Paylaşılan seed'e bağımlı test yazılmaz; her test kendi verisini kurar
    /// (`docs/testing.md` § 4).
    /// </summary>
    internal async Task SifirlaAsync()
    {
        await using MediaDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {MediaDbContext.Schema}.assets CASCADE");
        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {MediaDbContext.Schema}.outbox_messages");
    }
}

[CollectionDefinition(Name)]
public sealed class MedyaOrtamiKoleksiyonu : ICollectionFixture<MedyaOrtami>
{
    public const string Name = "medya-ortami";
}
