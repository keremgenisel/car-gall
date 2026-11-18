using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Projection;

/// <summary>
/// Vitrin okuma modelini **aynı işlemde** güncelleyen tek yer (**R15**, **D12**).
///
/// <para><b>Karar `ShowcaseListingProjector`'da kalıyor</b> — o saf ve test edilebilir;
/// burası yalnızca kararın gerektirdiği veriyi toplayıp satırı yazıyor ya da siliyor.
/// **C12**: görünürlük kararını veren tek yer yansıtmadır. Kopyalanmış kolonları besleyen
/// olay işleyicileri (kapak URL'i, şube adları) bu kararı vermez ve bu yüzden
/// dışarıdadır (**D34**).</para>
///
/// <para><b>İki modülden senkron okuma yapıyor</b> ve ikisi de § 2.A'nın saydığı meşru
/// okumalar: şube adları `Dealership`, kapak görselinin adresi `Media`. <b>Bedel
/// açıkça kabul edildi</b> (**D12**): `Dealership` erişilemezken yayına alma düşer.</para>
///
/// <para>Şube adı <b>locale → ad haritası</b> olarak okunuyor (**D40**): okuma modelinin
/// kolonu bir sözlük ve tek dille yazmak aracı diğer dillerde şubesiz gösterirdi.</para>
/// </summary>
internal sealed class ShowcaseProjectionWriter(
    VehicleCatalogDbContext context,
    IBranchQueries branches,
    IMediaAssetQueries mediaAssets)
{
    /// <summary>
    /// Aracın vitrin satırını yazar, günceller ya da siler. Çağıran bunu
    /// <c>SaveChanges</c>'ten ÖNCE çağırır; yazma ile yansıtma aynı transaction'a girer.
    ///
    /// <para><c>read</c> `0012c` **D46**'nın ön okuma bağlamı: verildiğinde şube adları,
    /// public alan anahtarları ve kapak adresi (**D56**) oradan gelir, verilmediğinde
    /// buradan okunur. İkinci yol
    /// <b>kaldırılmadı</b> çünkü yansıtmanın dört çağıranı var ve üçü tek kayıt üzerinde
    /// çalışıyor; hepsini bağlam kurmaya zorlamak `0012c`'nin işini üç komuda
    /// genişletirdi. Toplu yol bağlamı verir ve 25 satır için `Dealership`'i şube başına
    /// bir kez okur.</para>
    /// </summary>
    internal async Task ProjectAsync(
        Vehicle vehicle,
        Listing listing,
        ListingReadContext? read,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(listing);

        ShowcaseListing? mevcut = await context.ShowcaseListings
            .Include(row => row.OptionalValues)
            .FirstOrDefaultAsync(row => row.ListingId == listing.Id.Value, cancellationToken);

        // `0016b` D25 — kapak ve galeri AYNI varlık okumasından (aşağıdaki `VarliklarAsync`).
        IReadOnlyDictionary<Guid, MediaAssetDto> varliklar = listing.Slug is null
            ? new Dictionary<Guid, MediaAssetDto>()
            : await VarliklarAsync(vehicle.MediaAssetIds, read, cancellationToken);

        ShowcaseListing? hedef = listing.Slug is null
            // Adresi olmayan ilan hiç yayınlanmamıştır; yansıtmanın sorusu bile doğmaz.
            ? null
            : ShowcaseListingProjector.Project(
                vehicle,
                listing,
                listing.Slug.Value,
                read is null
                    ? await SubeAdlariAsync(vehicle.BranchId, cancellationToken)
                    : read.BranchNamesOf(vehicle.BranchId),
                KapakAdresi(vehicle.CoverAssetId, varliklar),
                read?.PublicFieldKeys ?? await PublicAlanlarAsync(cancellationToken));

        await GaleriyiEsitleAsync(
            listing.Id.Value,
            hedef is null ? [] : vehicle.MediaAssetIds,
            varliklar,
            cancellationToken);

        if (hedef is null)
        {
            if (mevcut is not null)
            {
                context.ShowcaseListings.Remove(mevcut);
            }

            return;
        }

        if (mevcut is null)
        {
            context.ShowcaseListings.Add(hedef);
            return;
        }

        // Satır VAR: anahtar ve değişmezler korunuyor, gerisi üzerine yazılıyor. Silip
        // yeniden eklemek `ListingId`'yi anahtar olarak kullanan bir tabloda aynı turda
        // iki işlem demekti ve EF'in izleyicisi onu çakışma olarak görürdü.
        Guncelle(mevcut, hedef);
    }

    /// <summary>Aracın ilanını bulur ve yansıtır; ilan yoksa sessizce çıkar.</summary>
    internal async Task ProjectAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        Listing? listing = await context.Listings
            .FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, cancellationToken);

        if (listing is not null)
        {
            await ProjectAsync(vehicle, listing, read: null, cancellationToken);
        }
    }

    /// <summary>
    /// Var olan satırı yansıtmanın ürettiği satıra eşitler.
    ///
    /// <para><b>Kolonlar ELLE SAYILMIYOR ve bu bir dış bulgunun bedeliydi.</b> İlk yazımda
    /// on beş kolonu tek tek kopyalıyordum ve <b>beşini atlamıştım</b>: gövde tipi, yakıt,
    /// vites, çekiş ve donanım paketi. Beşi de `Vehicle.Specification`'dan geliyor,
    /// `ChangeCoreDetails` o nesnenin tamamını değiştiriyor ve vitrin satırına yazan tek
    /// yer burası (C12) — yani "sedan" diye kaydedilip "suv" olarak düzeltilen araç
    /// ziyaretçinin gövde tipi filtresinde <b>kalıcı olarak</b> sedan kalıyordu.</para>
    ///
    /// <para>Yansıma ile kopyalamak altıncı kolonda aynı hatayı imkânsız kılıyor:
    /// `ShowcaseListing`'e bir alan eklendiğinde kopyalanacaklar listesine de eklenmiş
    /// olur. <b>Dışarıda kalan üçü değişmez</b> ve adıyla yazılı: satırın anahtarı
    /// (`ListingId`), aracın kimliği ve ilanın ADRESİ (**D13** — ilk yayında üretilir,
    /// bir daha değişmez).</para>
    /// </summary>
    private static void Guncelle(ShowcaseListing mevcut, ShowcaseListing hedef)
    {
        foreach (System.Reflection.PropertyInfo ozellik in KopyalanacakOzellikler)
        {
            ozellik.SetValue(mevcut, ozellik.GetValue(hedef));
        }
    }

    /// <summary>
    /// Değişmezler: satırın anahtarı, aracın kimliği ve ilanın adresi. **Test tarafından
    /// okunuyor**: listeye dördüncü bir ad girdiği gün o kolon sessizce kopyalanmaz olur
    /// ve kapı bunu yakalamak zorunda.
    /// </summary>
    internal static readonly string[] Degismezler =
    [
        nameof(ShowcaseListing.ListingId),
        nameof(ShowcaseListing.VehicleId),
        nameof(ShowcaseListing.Slug),
    ];

    internal static readonly System.Reflection.PropertyInfo[] KopyalanacakOzellikler =
    [
        .. typeof(ShowcaseListing)
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(ozellik => ozellik.CanWrite)
            .Where(ozellik => !Degismezler.Contains(ozellik.Name))
    ];

    /// <summary>
    /// Kapak görselinin türev adı. `Media`'nın `MediaVariant.Display` sabiti o modülün
    /// `internal`'ı (Yasak Listesi 6); sınırın bu yanında dize olarak tekrarlanıyor ve
    /// tekrar BİLİNÇLİ (Yasak Listesi 10). `MediaAssetReadyHandler` aynı sabiti aynı
    /// gerekçeyle taşıyor.
    ///
    /// <para><c>internal</c>: `0012c` **D56**'dan sonra kapak adresini ön okuma bağlamı da
    /// çözüyor ve türev adı MODÜL İÇİNDE iki yerde yaşamamalı — iki kopya, birinin sessizce
    /// başka bir türeve kayması demekti.</para>
    /// </summary>
    internal const string DisplayVariant = "display";

    private async Task<IReadOnlyDictionary<string, string>> SubeAdlariAsync(
        Guid branchId,
        CancellationToken cancellationToken) =>
        await branches.FindNamesAsync(branchId, cancellationToken)
        ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Kapak görselinin GÖSTERİM türevi. Varlık yoksa ya da henüz hazır değilse
    /// <c>null</c> döner — `MediaAssetReady` geldiğinde kolon o yoldan tazelenir
    /// (`0011` AC-36, **D34**). Varlıklar <c>VarliklarAsync</c>'ten gelir.
    ///
    /// <para><b>Bağlam varsa oradan</b> (**D56**): toplu yol kapak adreslerini istek başına
    /// bir kez, tek `FindManyAsync` ile okuyor.</para>
    ///
    /// <para><b>Bağlamda hazır DEĞİLSE kayıt başına okumaya düşüyor</b> ve bu bir ölçümün sonucu:
    /// `MediaAssetReady` yalnız VAR OLAN vitrin satırlarını tazeliyor
    /// (`MediaAssetReadyHandler`, `ExecuteUpdate`). Bağlam kurulurken henüz hazır olmayan
    /// bir kapak, satır doğduktan sonra hiçbir olayla tazelenmez — 25 satırlık bir toplu
    /// yayın saniyeler sürüyor ve "fotoğrafları yükle, hemen yayına al" sırası tam bu
    /// pencereye düşüyor. D56'nın bayat-kapak kabulü <b>var olan</b> bir satır için geçerli;
    /// yeni doğan satır için kapağı atlamak, aracı vitrine <b>görselsiz</b> koymak olurdu.
    /// Bedel yalnızca çözülemeyen kapak başına bir okuma.</para>
    /// </summary>
    private static string? KapakAdresi(Guid? coverAssetId, IReadOnlyDictionary<Guid, MediaAssetDto> varliklar) =>
        coverAssetId is Guid kimlik && varliklar.TryGetValue(kimlik, out MediaAssetDto? varlik)
            ? varlik.Derivatives.FirstOrDefault(derivative => derivative.Variant == DisplayVariant)?.Url
            : null;

    /// <summary>
    /// `0016b` D25 — aracın medya listesindeki varlıklar. Bağlam verildiyse oradan; bağlamda
    /// <b>hazır olmayan</b> varlıklar kayıt başına yeniden okunur — kapağın yukarıdaki
    /// gerekçesi galerinin her yuvası için de geçerli: bağlam kurulduktan sonra hazır olan
    /// bir görsel, satır doğmadan gelen `MediaAssetReady` yüzünden hiç tazelenmezdi.
    /// Bağlam yoksa liste için tek bir `FindManyAsync`.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, MediaAssetDto>> VarliklarAsync(
        IReadOnlyList<Guid> kimlikler,
        ListingReadContext? read,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, MediaAssetDto> sonuc = new();

        foreach (Guid kimlik in kimlikler)
        {
            if (read is not null
                && read.MediaAssets.TryGetValue(kimlik, out MediaAssetDto? bilinen)
                && bilinen.Status == MediaProcessingStatus.Ready)
            {
                sonuc[kimlik] = bilinen;
            }
        }

        List<Guid> okunacak = [.. kimlikler.Where(kimlik => !sonuc.ContainsKey(kimlik)).Distinct()];

        if (okunacak.Count > 0)
        {
            foreach (MediaAssetDto varlik in await mediaAssets.FindManyAsync(okunacak, cancellationToken))
            {
                sonuc[varlik.Id] = varlik;
            }
        }

        return sonuc;
    }

    /// <summary>
    /// `0016b` D25 — galeri yuvalarını aracın medya sırasına eşitler: konum başına bir satır,
    /// varlığın o anki durumu ve hazırsa türevleri. Vitrinden çıkan ilanın yuvaları silinir.
    ///
    /// <para>Satırlar konumla anahtarlı ve YERİNDE güncelleniyor: aynı işlemde silip aynı
    /// anahtarla yeniden eklemek EF'in izleyicisinde çakışma olurdu (yukarıdaki
    /// <c>Guncelle</c>'nin gerekçesiyle aynı).</para>
    /// </summary>
    private async Task GaleriyiEsitleAsync(
        Guid listingId,
        IReadOnlyList<Guid> sira,
        IReadOnlyDictionary<Guid, MediaAssetDto> varliklar,
        CancellationToken cancellationToken)
    {
        List<ShowcaseListingMedia> mevcut = await context.ShowcaseListingMedia
            .Where(item => item.ListingId == listingId)
            .ToListAsync(cancellationToken);

        for (int konum = 0; konum < sira.Count; konum += 1)
        {
            (ShowcaseMediaStatus durum, List<ShowcaseMediaVariant> turevler) =
                Yuva(varliklar.TryGetValue(sira[konum], out MediaAssetDto? varlik) ? varlik : null);

            ShowcaseListingMedia? satir = mevcut.Find(item => item.Position == konum);

            if (satir is null)
            {
                context.ShowcaseListingMedia.Add(new ShowcaseListingMedia
                {
                    ListingId = listingId,
                    Position = konum,
                    MediaAssetId = sira[konum],
                    Status = durum,
                    Variants = turevler,
                });
            }
            else
            {
                satir.MediaAssetId = sira[konum];
                satir.Status = durum;
                satir.Variants = turevler;
            }
        }

        context.ShowcaseListingMedia.RemoveRange(mevcut.Where(item => item.Position >= sira.Count));
    }

    /// <summary>
    /// Varlığın vitrin durumu. `Media`'da bulunmayan varlık `işleniyor` sayılır: galeride
    /// görünmez ve varlık hazır olursa `MediaAssetReady` yuvayı günceller.
    /// </summary>
    internal static (ShowcaseMediaStatus Durum, List<ShowcaseMediaVariant> Turevler) Yuva(MediaAssetDto? varlik) =>
        varlik?.Status switch
        {
            MediaProcessingStatus.Ready => (
                ShowcaseMediaStatus.Ready,
                [.. varlik.Derivatives.Select(turev => new ShowcaseMediaVariant(turev.Variant, turev.Url, turev.Width, turev.Height))]),
            MediaProcessingStatus.Failed => (ShowcaseMediaStatus.Failed, []),
            _ => (ShowcaseMediaStatus.Processing, []),
        };

    private async Task<IReadOnlySet<string>> PublicAlanlarAsync(CancellationToken cancellationToken) =>
        new HashSet<string>(
            await context.VehicleFieldSettings
                .AsNoTracking()
                .Where(setting => !setting.IsCore && setting.FormState != FieldFormState.Closed)
                .Where(setting => setting.PublicVisibility == FieldPublicVisibility.Visible)
                .Select(setting => setting.FieldKey)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);
}
