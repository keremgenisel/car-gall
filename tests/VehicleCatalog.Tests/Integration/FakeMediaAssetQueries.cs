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

    /// <summary>`0026` D16 — logo türünde, hazır ve logo türevli bir varlık.</summary>
    internal Guid EkleLogo()
    {
        Guid kimlik = Guid.CreateVersion7();
        varliklar[kimlik] = new MediaAssetDto(
            kimlik,
            MediaProcessingStatus.Ready,
            null,
            new Dictionary<string, string>(),
            [
                new MediaDerivativeDto("logo-large", 1200, 300, $"https://cdn.test/{kimlik:D}/logo-large.webp"),
                new MediaDerivativeDto("logo-small", 320, 80, $"https://cdn.test/{kimlik:D}/logo-small.webp"),
            ],
            MediaAssetKind.Logo);
        return kimlik;
    }

    /// <summary>
    /// `0016b` — `Media`'nın gerçek kümesi gibi ÜÇ türevli varlık: kaynak <paramref name="en"/>×
    /// <paramref name="boy"/>, türevler en uzun kenarda 400 / 800 / min(kaynak, 1600).
    /// Hazır değilse türevsiz.
    /// </summary>
    internal Guid EkleTurevli(MediaProcessingStatus status, int en = 1200, int boy = 900)
    {
        Guid kimlik = Guid.CreateVersion7();
        varliklar[kimlik] = Turevli(kimlik, status, en, boy);
        return kimlik;
    }

    /// <summary>Varlığın durumunu değiştirir (olay işlenmiş gibi); türevleri buna göre kurulur.</summary>
    internal void Durumla(Guid kimlik, MediaProcessingStatus status, int en = 1200, int boy = 900) =>
        varliklar[kimlik] = Turevli(kimlik, status, en, boy);

    /// <summary>`EkleTurevli`'nin türev kümesi; olay testleri aynı kümeyi `MediaAssetReady` olarak yollar.</summary>
    internal static List<MediaDerivativeDto> Turevler(Guid kimlik, int en, int boy)
    {
        MediaDerivativeDto Turev(string ad, int uzun)
        {
            int sinir = Math.Min(uzun, Math.Max(en, boy));
            (int w, int h) = en >= boy ? (sinir, sinir * boy / en) : (sinir * en / boy, sinir);
            return new MediaDerivativeDto(ad, w, h, $"https://cdn.test/{kimlik:D}/{ad}.webp");
        }

        return [Turev("thumbnail", 400), Turev("display", 800), Turev("large", 1600)];
    }

    private static MediaAssetDto Turevli(Guid kimlik, MediaProcessingStatus status, int en, int boy) => new(
        kimlik,
        status,
        status == MediaProcessingStatus.Failed ? MediaFailureReason.Undecodable : null,
        new Dictionary<string, string> { ["tr"] = "Ön görünüm" },
        status == MediaProcessingStatus.Ready ? Turevler(kimlik, en, boy) : []);

    public Task<MediaAssetDto?> FindAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
        Task.FromResult(varliklar.TryGetValue(mediaAssetId, out MediaAssetDto? dto) ? dto : null);

    public Task<bool> AreAllReadyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(mediaAssetIds.Count > 0 && mediaAssetIds.All(kimlik =>
            varliklar.TryGetValue(kimlik, out MediaAssetDto? dto)
            && dto.Status == MediaProcessingStatus.Ready));

    /// <summary>
    /// `0012c` **AC-35** — medya hazırlığı **kayıt başına** okunuyor. Bu sayaç o kriteri
    /// ölçüyor: okuma bağlamına (**D46**) alınsaydı sayı 1'de kalırdı ve bayat bir
    /// "hazır" cevabı yayına alma kararını zehirlerdi.
    /// </summary>
    internal int HazirlikOkumaSayisi { get; private set; }

    public Task<IReadOnlySet<Guid>> ReadyAmongAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        HazirlikOkumaSayisi++;

        return Task.FromResult<IReadOnlySet<Guid>>(mediaAssetIds
            .Where(kimlik => varliklar.TryGetValue(kimlik, out MediaAssetDto? dto)
                && dto.Status == MediaProcessingStatus.Ready)
            .ToHashSet());
    }

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
