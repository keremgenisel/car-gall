using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

internal sealed record SocialLink(string Platform, string Url);

/// <summary>
/// Kurulumun ait olduğu tek işletme (docs/domain.md). **Tekil agregat**: kurulumda tam
/// olarak bir galeri profili vardır (R1, AC-1).
///
/// Tekillik kimliğin sabitlenmesiyle sağlanıyor ve veritabanında bir kısıtla dayatılıyor —
/// "uygulama ikincisini yazmaz" demek yetmez, çünkü onu çiğneyeni hiçbir test yakalamaz.
/// `TenantId` benzeri bir alan YOKTUR: kurulum = galeri (C11).
/// </summary>
internal sealed class Gallery : Entity<Guid>
{
    /// <summary>Tek satırın sabit kimliği; şema bu değeri bir CHECK kısıtıyla zorunlu kılar.</summary>
    internal static readonly Guid SingletonId = Guid.Parse("00000000-0000-7000-8000-00000000da01");

    private readonly List<SocialLink> socialLinks = [];

    private Gallery(string commercialName)
        : base(SingletonId) => CommercialName = commercialName;

    internal string CommercialName { get; private set; }

    /// <summary>D7 — yalnızca referans; görselin kendisi `Media` spec'ine aittir.</summary>
    internal Guid? LogoMediaAssetId { get; private set; }

    internal string? TaxNumber { get; private set; }

    internal string? TradeRegistryNumber { get; private set; }

    internal IReadOnlyList<SocialLink> SocialLinks => socialLinks;

    internal static Result<Gallery> Create(string commercialName)
    {
        if (string.IsNullOrWhiteSpace(commercialName))
        {
            return Result.Failure<Gallery>(new Error("gallery.commercial_name_required"));
        }

        return Result.Success(new Gallery(commercialName.Trim()));
    }

    internal void SetLogo(Guid mediaAssetId) => LogoMediaAssetId = mediaAssetId;

    internal void SetRegistration(string? taxNumber, string? tradeRegistryNumber)
    {
        TaxNumber = taxNumber;
        TradeRegistryNumber = tradeRegistryNumber;
    }

    internal void AddSocialLink(SocialLink link)
    {
        if (!socialLinks.Any(item => item.Platform == link.Platform))
        {
            socialLinks.Add(link);
        }
    }
}
