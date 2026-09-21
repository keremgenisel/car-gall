using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Detail;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>Referans etiketleri VehicleReference'a aittir; bu testlerde o sınırın ötesi sahtelenir.</summary>
internal sealed class FakeReferenceLabelResolver : IReferenceLabelResolver
{
    private static readonly Dictionary<(string, string), Dictionary<string, string>> Etiketler = new()
    {
        [("BodyType", "sedan")] = new() { ["tr"] = "Sedan", ["ar"] = "سيدان", ["en"] = "Sedan" },
        [("FuelType", "diesel")] = new() { ["tr"] = "Dizel", ["ar"] = "ديزل", ["en"] = "Diesel" },
        [("TransmissionType", "automatic")] = new() { ["tr"] = "Otomatik", ["ar"] = "أوتوماتيك", ["en"] = "Automatic" },
        [("DrivetrainType", "front-wheel-drive")] = new() { ["tr"] = "Önden çekiş", ["ar"] = "دفع أمامي", ["en"] = "Front-wheel drive" },
        [("TrimLevel", "comfort")] = new() { ["tr"] = "Comfort", ["ar"] = "كومفورت", ["en"] = "Comfort" },
    };

    public Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        Locale locale,
        CancellationToken cancellationToken)
    {
        Dictionary<(string, string), string> sonuc = [];

        foreach ((string kind, string slug) in terms)
        {
            if (Etiketler.TryGetValue((kind, slug), out Dictionary<string, string>? diller)
                && diller.TryGetValue(locale.Code, out string? etiket))
            {
                sonuc[(kind, slug)] = etiket;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<(string Kind, string Slug), string>>(sonuc);
    }
}
