namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0028` **D7** · **P2** — Maps Embed API anahtarı, kurulum yapılandırmasından bir kez okunur
/// (<c>GoogleMaps:EmbedApiKey</c>, `docs/deploy.md`). Boş ya da yalnız boşluk = yok: gömme adresi
/// kurulmaz (AC-28). Anahtar sır değildir ama yalnız gömme adresinde bulunur; bu sınıf onu hiçbir
/// metne (<see cref="ToString"/>, günlük) çevirmez (PM şartı (a), AC-83).
/// </summary>
internal sealed class DealershipMapOptions(string? embedApiKey)
{
    internal string? EmbedApiKey { get; } = string.IsNullOrWhiteSpace(embedApiKey) ? null : embedApiKey.Trim();

    public override string ToString() => nameof(DealershipMapOptions);
}
