using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

internal sealed class ReferenceLabelResolver(IReferenceTermQueries queries, IBodyTypeQueries bodyTypes)
    : IReferenceLabelResolver
{
    public async Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        Locale locale,
        CancellationToken cancellationToken)
    {
        Dictionary<(string, string), string> sonuc = new(
            await queries.ResolveLabelsAsync(
                [.. terms.Where(term => term.Kind != "BodyType")],
                locale.Code,
                cancellationToken));

        // Kasa tipi 0001'de kendi yolunda yaşıyor; iki kaynağı burada birleştiriyoruz.
        // Birleştirme ayrı bir spec'e önerilecek — 0002'nin kapsamı değil.
        (string Kind, string Slug)? bodyType = terms.FirstOrDefault(term => term.Kind == "BodyType");

        if (bodyType is { } aranan)
        {
            IReadOnlyList<BodyTypeDto> hepsi = await bodyTypes.GetAllAsync(locale.Code, cancellationToken);
            BodyTypeDto? eslesen = hepsi.FirstOrDefault(item => item.Slug == aranan.Slug);

            if (eslesen is not null)
            {
                sonuc[("BodyType", aranan.Slug)] = eslesen.Label;
            }
        }

        return sonuc;
    }
}
