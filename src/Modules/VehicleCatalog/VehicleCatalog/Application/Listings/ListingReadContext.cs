using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// `0012c` **C4**/**D46** — **aynı yanıtı veren** okumaların istek başına bir kez yapılıp
/// komuta verildiği bağlam.
///
/// <para>C4 ilk yazımıyla C2 ve C3 ile çelişiyordu ve bu ölçülerek bulundu: tekil komut
/// araç başına beşe kadar okuma yapıyor ve hepsi kilidin içinde. Yirmi beş satırlık bir
/// seçim ≈ 125 okuma demekti.</para>
///
/// <para><b>Burada olmayan okuma bilinçli:</b> medya hazırlığı (`ReadyAmongAsync`) kayıt
/// başına ve <b>kilidin altında</b> kalıyor. Onu da öne almak, `0012b` **D8**'in
/// *"agregatı kilitten SONRA oku"* kuralının önlediği bayat-okuma hatasını geri getirirdi:
/// fotoğrafı o saniye düşen bir araç yayına alınabilirdi. Dördü ise kilit boyunca
/// değişmeyen cevaplar — alan ayarları, şube adları ve kanonik model adları bir isteğin
/// içinde ikinci kez sorulduğunda aynı şeyi söyler.</para>
///
/// <para><b>Beşinci okuma kapak adresleri</b> (**D56**) ve o "aynı yanıtı veren" bir okuma
/// DEĞİL — araç başına ayrı bir cevap, ama <b>küme hâlinde sorulabiliyor</b>. Bir dış bulgu
/// onu ölçtü: yansıtma kapağı kayıt başına okuyordu, yani 25 satırlık bir toplu yayın 25
/// hazırlık + 25 kapak = <b>50</b> modüller arası çağrı yapıyordu. Kapak hazırlıktan
/// farklıdır ve öne alınabilir: tazelik gerektirmiyor, kopyalanmış bir kolonu besliyor ve
/// `MediaAssetReadyHandler` onu kendi yolundan tazeliyor (**D34**). Bayat bir kapak bir
/// sonraki `MediaAssetReady`'de düzelir; bayat bir "hazır" cevabı <b>yanlış bir yayın
/// kararı</b> üretir. Fark bu.</para>
/// </summary>
internal sealed record ListingReadContext(
    /// <summary>A2'nin zorunlu opsiyonel alanları. Kendi şemamız; yayın kapısı okuyor.</summary>
    IReadOnlySet<string> RequiredFieldKeys,
    /// <summary>Vitrin satırına kopyalanacak public alan anahtarları. Yansıtma okuyor.</summary>
    IReadOnlySet<string> PublicFieldKeys,
    /// <summary>
    /// Şube → (locale → ad). **Şube başına** bir giriş, araç başına değil: yirmi aracın
    /// hepsi aynı şubedeyse `Dealership` bir kez okunur.
    /// </summary>
    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> BranchNames,
    /// <summary>Model → kanonik Latin ad. İlk yayında adres üretimi için.</summary>
    IReadOnlyDictionary<Guid, string> CanonicalModelNames,
    /// <summary>
    /// Kapak varlığı → gösterim türevinin adresi (**D56**). Anahtar VARLIK kimliği, araç
    /// kimliği değil: iki araç aynı varlığı gösterirse `Media` bir kez sorulur.
    /// </summary>
    IReadOnlyDictionary<Guid, string> CoverUrls)
{
    /// <summary>
    /// Verilen araçlar için bağlamı kurar. Araç satırları **kendi şemamızdan** ve
    /// `AsNoTracking` ile okunuyor; bu okuma modüller arası değil ve kilidin dışında
    /// olması sakıncasız — yalnız hangi şubeleri ve hangi modelleri soracağımızı söylüyor.
    ///
    /// <para>Bulunamayan araç burada sessizce düşer; onun satırı komutta
    /// `vehicle.not_found` olur (**D27**).</para>
    /// </summary>
    internal static async Task<ListingReadContext> LoadAsync(
        VehicleCatalogDbContext context,
        IBranchQueries branches,
        IVehicleModelNameResolver modelNames,
        IMediaAssetQueries mediaAssets,
        IReadOnlyList<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(branches);
        ArgumentNullException.ThrowIfNull(modelNames);
        ArgumentNullException.ThrowIfNull(mediaAssets);

        List<VehicleId> kimlikler = [.. (vehicleIds ?? []).Select(id => new VehicleId(id))];

        // Medya listesi GÖLGE özellik üzerinden okunuyor: `CoverAssetId` alanın ilk
        // elemanını veren hesaplanmış bir özellik ve EF onu sunucuda çeviremez.
        var hedefler = await context.Vehicles
            .AsNoTracking()
            .Where(vehicle => kimlikler.Contains(vehicle.Id))
            .Select(vehicle => new
            {
                vehicle.BranchId,
                vehicle.VehicleModelId,
                MediaAssetIds = EF.Property<List<Guid>>(vehicle, "mediaAssetIds"),
            })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, IReadOnlyDictionary<string, string>> subeAdlari = new();

        foreach (Guid branchId in hedefler.Select(hedef => hedef.BranchId).Distinct())
        {
            subeAdlari[branchId] =
                await branches.FindNamesAsync(branchId, cancellationToken)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        List<Guid> modeller = [.. hedefler.Select(hedef => hedef.VehicleModelId).Distinct()];

        IReadOnlyDictionary<Guid, string> adlar = modeller.Count == 0
            ? new Dictionary<Guid, string>()
            : await modelNames.ResolveCanonicalNamesAsync(modeller, cancellationToken);

        // Kapak varlıkları TEK çağrıda: seçimdeki her aracın medya listesinin ilk elemanı
        // (`docs/domain.md`, Kapak Görseli) ve tekrarlar `FindManyAsync`'te elenir.
        List<Guid> kapakVarliklari =
        [
            .. hedefler
                .Select(hedef => hedef.MediaAssetIds.Count > 0 ? hedef.MediaAssetIds[0] : (Guid?)null)
                .OfType<Guid>()
                .Distinct()
        ];

        Dictionary<Guid, string> kapaklar = new();

        foreach (MediaAssetDto varlik in await mediaAssets.FindManyAsync(kapakVarliklari, cancellationToken))
        {
            string? adres = varlik.Derivatives
                .FirstOrDefault(derivative => derivative.Variant == ShowcaseProjectionWriter.DisplayVariant)?.Url;

            if (adres is not null)
            {
                kapaklar[varlik.Id] = adres;
            }
        }

        return new ListingReadContext(
            await AlanAnahtarlariAsync(context, zorunlu: true, cancellationToken),
            await AlanAnahtarlariAsync(context, zorunlu: false, cancellationToken),
            subeAdlari,
            adlar,
            kapaklar);
    }

    /// <summary>
    /// İki alan kümesi TEK sorgu deseninden çıkıyor. Zorunlu küme yayın kapısının,
    /// public küme yansıtmanın sorusu; ikisi de `vehicle_field_settings`'in aynı satırları
    /// üzerinde ve ayrı yazılsalardı "çekirdek alan hariç" koşulu iki yerde yaşardı.
    /// </summary>
    private static async Task<IReadOnlySet<string>> AlanAnahtarlariAsync(
        VehicleCatalogDbContext context,
        bool zorunlu,
        CancellationToken cancellationToken)
    {
        IQueryable<VehicleFieldSetting> sorgu = context.VehicleFieldSettings
            .AsNoTracking()
            .Where(setting => !setting.IsCore);

        sorgu = zorunlu
            ? sorgu.Where(setting => setting.FormState == FieldFormState.Required)
            : sorgu
                .Where(setting => setting.FormState != FieldFormState.Closed)
                .Where(setting => setting.PublicVisibility == FieldPublicVisibility.Visible);

        return new HashSet<string>(
            await sorgu.Select(setting => setting.FieldKey).ToListAsync(cancellationToken),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Bir şubenin ad haritası. Bağlamda yoksa boş döner — çağıran o zaman "adsız şube"
    /// davranışını görür ve bu, `FindNamesAsync`'in <c>null</c> cevabının zaten anlamı.
    /// </summary>
    internal IReadOnlyDictionary<string, string> BranchNamesOf(Guid branchId) =>
        BranchNames.TryGetValue(branchId, out IReadOnlyDictionary<string, string>? adlar)
            ? adlar
            : new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Kapak adresi. Varlık yoksa, henüz hazır değilse ya da araç kapaksızsa <c>null</c> —
    /// kayıt başına okuyan yolun <c>null</c> cevabıyla AYNI anlam (**D34**: kolon bir
    /// sonraki `MediaAssetReady`'de tazelenir).
    /// </summary>
    internal string? CoverUrlOf(Guid? coverAssetId) =>
        coverAssetId is Guid kimlik && CoverUrls.TryGetValue(kimlik, out string? adres) ? adres : null;
}
