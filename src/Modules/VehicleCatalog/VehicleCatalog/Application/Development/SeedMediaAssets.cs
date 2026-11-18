using CarGallery.Modules.Media.Contracts.Assets;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Modules.VehicleCatalog.Application.Development;

/// <summary>
/// `0016b` — geliştirme tohumunun medya varlıkları (triyaj BULGU-3). Tohumun varlıkları
/// `Media`'da kayıtlı değil; eskiden kimlikleri rastgeleydi ve tohum ilanı panelde ya da
/// toplu eylemde yeniden yansıtılınca `FindManyAsync` boş dönüyor, her yuva `işleniyor`a
/// düşüyor ve galeri detaydan sessizce kayboluyordu.
///
/// <para>Şimdi kimlik DETERMİNİSTİK ve kendini tarif ediyor: işaret baytları, ilan sırası,
/// konum, durum, kare numarası ve en-boy oranı Guid'in içinde. <see cref="Describe"/> o
/// kimlikten `Media`'nın vereceği cevabı üretir; <see cref="SeedAwareMediaAssetQueries"/>
/// yalnız tohum bayrağı açıkken gerçek sorgunun önüne konur. Tohum dışındaki her kimlik
/// gerçek `Media`'ya gider.</para>
/// </summary>
internal static class SeedMediaAssets
{
    /// <summary>Tohum kimliğinin ilk dört baytı; gerçek UUIDv7'nin zaman damgasıyla çakışmaz.</summary>
    private static readonly byte[] Marker = [0x5E, 0xED, 0x00, 0x16];

    /// <summary>Tohum yuvasının kimliği; aynı girdiler her kurulumda aynı kimliği verir.</summary>
    internal static Guid Id(int listingIndex, int position, MediaProcessingStatus status, int frame, int aspectWidth, int aspectHeight)
    {
        byte[] bytes = new byte[16];
        Marker.CopyTo(bytes, 0);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), listingIndex);
        bytes[8] = checked((byte)position);
        bytes[9] = checked((byte)status);
        bytes[10] = checked((byte)frame);
        bytes[11] = checked((byte)aspectWidth);
        bytes[12] = checked((byte)aspectHeight);

        return new Guid(bytes);
    }

    /// <summary>Tohum kimliğiyse `Media`'nın vereceği varlık; değilse <c>null</c>.</summary>
    internal static MediaAssetDto? Describe(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes);

        if (!bytes[..4].SequenceEqual(Marker))
        {
            return null;
        }

        MediaProcessingStatus status = (MediaProcessingStatus)bytes[9];

        return new MediaAssetDto(
            id,
            status,
            status == MediaProcessingStatus.Failed ? MediaFailureReason.Undecodable : null,
            new Dictionary<string, string>(StringComparer.Ordinal),
            status == MediaProcessingStatus.Ready ? Derivatives(bytes[10], bytes[11], bytes[12]) : []);
    }

    /// <summary>
    /// `Media`'nın üç türevinin boyut kuralı (en uzun kenar 400 / 800 / kaynak ≤ 1600):
    /// 4:3 kaynak 1200×900 → `large` 1200×900 (1600 değil), `display` 800×600,
    /// `thumbnail` 400×300 — `0016b` AC-10/AC-60'ın sayıları.
    /// </summary>
    private static List<MediaDerivativeDto> Derivatives(int frame, int aspectWidth, int aspectHeight)
    {
        MediaDerivativeDto Turev(string ad, int uzun)
        {
            (int w, int h) = aspectWidth >= aspectHeight
                ? (uzun, uzun * aspectHeight / aspectWidth)
                : (uzun * aspectWidth / aspectHeight, uzun);

            return new MediaDerivativeDto(ad, w, h, $"/seed-media/frame-{frame}-{w}x{h}.jpg");
        }

        return [Turev("thumbnail", 400), Turev("display", 800), Turev("large", 1200)];
    }
}

/// <summary>
/// Tohum kimliklerini kendisi cevaplayan, gerisini gerçek `Media`'ya bırakan sorgu. YALNIZ
/// <c>Development:SeedShowcase</c> açıkken kaydedilir (`AddVehicleCatalogSeedMedia`).
/// </summary>
internal sealed class SeedAwareMediaAssetQueries(IMediaAssetQueries inner) : IMediaAssetQueries
{
    /// <summary>
    /// Kayıtlı <c>IMediaAssetQueries</c>'i bu sorguyla sarar; ömrü korunur. Burada, çünkü
    /// `Media.Contracts` yalnız <c>Application</c> katmanında kullanılır (`0012a` AC-33).
    /// </summary>
    internal static IServiceCollection Register(IServiceCollection services)
    {
        ServiceDescriptor asil = services.Last(descriptor => descriptor.ServiceType == typeof(IMediaAssetQueries));
        services.Remove(asil);
        services.Add(new ServiceDescriptor(
            typeof(IMediaAssetQueries),
            provider => new SeedAwareMediaAssetQueries(Asil(provider, asil)),
            asil.Lifetime));

        return services;
    }

    private static IMediaAssetQueries Asil(IServiceProvider provider, ServiceDescriptor descriptor) =>
        descriptor.ImplementationFactory is not null
            ? (IMediaAssetQueries)descriptor.ImplementationFactory(provider)
            : descriptor.ImplementationInstance as IMediaAssetQueries
              ?? (IMediaAssetQueries)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);

    /// <inheritdoc />
    public Task<MediaAssetDto?> FindAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
        SeedMediaAssets.Describe(mediaAssetId) is MediaAssetDto tohum
            ? Task.FromResult<MediaAssetDto?>(tohum)
            : inner.FindAsync(mediaAssetId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AreAllReadyAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        (List<MediaAssetDto> tohum, List<Guid> gercek) = Ayir(mediaAssetIds);

        return mediaAssetIds.Count > 0
            && tohum.All(varlik => varlik.Status == MediaProcessingStatus.Ready)
            && (gercek.Count == 0 || await inner.AreAllReadyAsync(gercek, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ReadyAmongAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        (List<MediaAssetDto> tohum, List<Guid> gercek) = Ayir(mediaAssetIds);
        HashSet<Guid> hazir = [.. tohum.Where(varlik => varlik.Status == MediaProcessingStatus.Ready).Select(varlik => varlik.Id)];

        if (gercek.Count > 0)
        {
            hazir.UnionWith(await inner.ReadyAmongAsync(gercek, cancellationToken));
        }

        return hazir;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MediaAssetDto>> FindManyAsync(IReadOnlyCollection<Guid> mediaAssetIds, CancellationToken cancellationToken)
    {
        (List<MediaAssetDto> tohum, List<Guid> gercek) = Ayir(mediaAssetIds);

        return gercek.Count == 0 ? tohum : [.. tohum, .. await inner.FindManyAsync(gercek, cancellationToken)];
    }

    private static (List<MediaAssetDto> Tohum, List<Guid> Gercek) Ayir(IReadOnlyCollection<Guid> kimlikler)
    {
        List<MediaAssetDto> tohum = [];
        List<Guid> gercek = [];

        foreach (Guid kimlik in kimlikler)
        {
            if (SeedMediaAssets.Describe(kimlik) is MediaAssetDto varlik)
            {
                tohum.Add(varlik);
            }
            else
            {
                gercek.Add(kimlik);
            }
        }

        return (tohum, gercek);
    }
}
