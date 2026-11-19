using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Bülten gövdesinin okuduğu üç başka modülün yüzeyi — bu test projesinin sınırının ötesinde,
/// sahte (docs/testing.md § 1). Sahteler GERÇEK sözleşmenin biçimini taşır: model adı
/// locale'e göre (`IVehicleModelQueries.ResolveNamesAsync`, marka + model), şube adı locale'e
/// göre, galeri adı locale'e göre ya da <c>null</c>.
/// </summary>
internal sealed class SahteModelAdlari : IVehicleModelQueries
{
    internal Dictionary<(Guid Id, string Locale), string> Adlar { get; } = new()
    {
        [(SahteAdlar.Corolla, "tr")] = "Toyota Corolla",
        [(SahteAdlar.Corolla, "en")] = "Toyota Corolla",
        [(SahteAdlar.Corolla, "ar")] = "تويوتا كورولا",
    };

    public Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(vehicleModelIds
            .Where(id => Adlar.ContainsKey((id, locale)))
            .ToDictionary(id => id, id => Adlar[(id, locale)]));

    public Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(IReadOnlyCollection<Guid> vehicleModelIds, CancellationToken cancellationToken) =>
        throw new NotSupportedException("bülten kanonik adı okumaz");

    public Task<IReadOnlyDictionary<Guid, VehicleModelPartsDto>> ResolvePartsAsync(IReadOnlyCollection<Guid> vehicleModelIds, string locale, CancellationToken cancellationToken) =>
        throw new NotSupportedException("bülten ad parçalarını okumaz");

    public Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(string locale, CancellationToken cancellationToken) =>
        throw new NotSupportedException("bülten model listesini okumaz");

    public Task<bool?> FindSelectableAsync(Guid vehicleModelId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("bülten model seçilebilirliğini okumaz");
}

internal sealed class SahteSubeler : IBranchContactQueries
{
    internal Dictionary<(Guid Id, string Locale), string> Adlar { get; } = new()
    {
        [(SahteAdlar.Kadikoy, "tr")] = "Kadıköy Şubesi",
        [(SahteAdlar.Kadikoy, "en")] = "Kadikoy Branch",
        [(SahteAdlar.Kadikoy, "ar")] = "فرع كاديكوي",
    };

    public Task<BranchNotificationContactDto?> FindContactAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
        Task.FromResult(Adlar.TryGetValue((branchId, locale), out string? ad)
            ? new BranchNotificationContactDto(branchId, ad, "sube@ornek.com")
            : null);
}

internal sealed class SahteGaleri : IGalleryDisplayNameQueries
{
    internal Dictionary<string, string> Adlar { get; } = new()
    {
        ["tr"] = "Yıldız Otomotiv",
        ["en"] = "Yildiz Motors",
        ["ar"] = "يلدز للسيارات",
    };

    public Task<string?> FindDisplayNameAsync(string locale, CancellationToken cancellationToken) =>
        Task.FromResult(Adlar.TryGetValue(locale, out string? ad) ? ad : Adlar.GetValueOrDefault("tr"));
}

/// <summary>Bülten bestecisinin kurulumu: varsayılan sahtelerle ya da testin verdikleriyle.</summary>
internal static class Besteci
{
    internal static NewsletterMessageComposer Kur(
        NotificationsDbContext context,
        IClock saat,
        SahteIlanOzeti? ilanlar = null,
        NewsletterOptions? secenekler = null,
        SahteModelAdlari? modeller = null,
        SahteSubeler? subeler = null,
        SahteGaleri? galeri = null,
        Microsoft.Extensions.Logging.ILogger<TemplateResolver>? sablonLogu = null)
    {
        NewsletterOptions bulten = secenekler ?? Bulten.BultenSecenekleri();

        return new NewsletterMessageComposer(
            context,
            new TemplateResolver(context, sablonLogu ?? NullLogger<TemplateResolver>.Instance),
            ilanlar ?? new SahteIlanOzeti(),
            modeller ?? new SahteModelAdlari(),
            subeler ?? new SahteSubeler(),
            galeri ?? new SahteGaleri(),
            new UnsubscribeTokens(bulten),
            new ConfirmationTokens(bulten),
            bulten,
            Bulten.Secenekler());
    }
}
