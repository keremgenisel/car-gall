using CarGallery.Modules.Media.Contracts.Assets;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `Media`'nın okuma sözleşmesinin sahtesi. **Sahtelenen şey SÖZLEŞME, iç tip değil**:
/// modül testi başka modülün iç tipine erişemez (`docs/architecture.md` § 5/6) ve
/// `Media.Contracts` bu modülün referans verebildiği tek yüzey.
///
/// <para><b>Gerçek `Media`'ya karşı kanıt `tests/Host.Tests`'te</b> — `0011`'in dersi
/// buydu: *"sahte bir komşu, kabloyu test etmez."* Buradaki sahte, birleştirmenin ve
/// sıranın `VehicleCatalog` tarafındaki davranışını ölçüyor.</para>
/// </summary>
internal sealed class FakeMediaAssetQueries : IMediaAssetQueries
{
    private readonly Dictionary<Guid, MediaAssetDto> varliklar = [];

    /// <summary>Kaç kez toplu okuma yapıldı. N+1'i ölçen test bunu sayıyor (D10).</summary>
    internal int TopluOkumaSayisi { get; private set; }

    internal Guid Ekle(MediaProcessingStatus status, string? altTr = null)
    {
        Guid kimlik = Guid.CreateVersion7();

        varliklar[kimlik] = new MediaAssetDto(
            kimlik,
            status,
            status == MediaProcessingStatus.Failed ? MediaFailureReason.WatermarkUnavailable : null,
            altTr is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["tr"] = altTr },
            status == MediaProcessingStatus.Ready
                ? [new MediaDerivativeDto("display", 800, 533, $"https://cdn.test/{kimlik:D}/display.webp")]
                : []);

        return kimlik;
    }

    public Task<MediaAssetDto?> FindAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
        Task.FromResult(varliklar.TryGetValue(mediaAssetId, out MediaAssetDto? dto) ? dto : null);

    public Task<bool> AreAllReadyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(mediaAssetIds.Count > 0 && mediaAssetIds.All(kimlik =>
            varliklar.TryGetValue(kimlik, out MediaAssetDto? dto)
            && dto.Status == MediaProcessingStatus.Ready));

    public Task<IReadOnlySet<Guid>> ReadyAmongAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<Guid>>(mediaAssetIds
            .Where(kimlik => varliklar.TryGetValue(kimlik, out MediaAssetDto? dto)
                && dto.Status == MediaProcessingStatus.Ready)
            .ToHashSet());

    public Task<IReadOnlyList<MediaAssetDto>> FindManyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        TopluOkumaSayisi++;

        return Task.FromResult<IReadOnlyList<MediaAssetDto>>(
            [.. mediaAssetIds
                .Where(varliklar.ContainsKey)
                .Select(kimlik => varliklar[kimlik])]);
    }
}
