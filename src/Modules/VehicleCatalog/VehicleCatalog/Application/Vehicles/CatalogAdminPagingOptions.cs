using CarGallery.Modules.VehicleCatalog.Contracts.Admin;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Panelin sayfa boyutu (**C26**): varsayılan 25, üst sınır 100 ve ikisi de dağıtım
/// yapılandırmasından okunur.
///
/// <para>Vitrin tarafının 24'ü AYRI bir eksendir (`ShowcaseListingRequest.PageSize`);
/// iki yüzey aynı sayıyı paylaşmak zorunda değil — biri ziyaretçinin ızgarası, diğeri
/// personelin tablosu.</para>
/// </summary>
internal sealed record CatalogAdminPagingOptions(int PageSize, int MaxPageSize);

/// <summary>
/// Liste isteğinin normalleştirilmesi. Üst sınırı aşan `pageSize` üst sınıra KIRPILIR,
/// hata dönmez (AC-26, `docs/conventions.md` § 3): sayfa boyutu personelin yazdığı bir
/// şey değil, adres çubuğundan gelen bir ipucudur ve hata ekranı onun karşılığı olamaz.
/// </summary>
internal readonly record struct VehicleAdminListRequest(int Page, int PageSize, VehicleAdminListSort Sort)
{
    internal static VehicleAdminListRequest Normalize(
        VehicleAdminListFilter filter,
        CatalogAdminPagingOptions options)
    {
        int page = filter.Page < 1 ? 1 : filter.Page;

        int pageSize = filter.PageSize is null or < 1
            ? options.PageSize
            : Math.Min(filter.PageSize.Value, options.MaxPageSize);

        return new VehicleAdminListRequest(page, pageSize, filter.Sort);
    }

    internal int Skip => (Page - 1) * PageSize;
}
