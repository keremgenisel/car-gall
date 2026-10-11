using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012b` — duyurular (**D11**, **D14**, **D30**), gerçek PostgreSQL üzerinde.
///
/// <para><b>Neden outbox TABLOSU sayılıyor, kod okunmuyor:</b> `0006`'nın ikinci kaçan
/// hatasının dersi birebir buydu — belirti "yanlış tabloya yazılıyor" sanılmıştı, gerçek
/// "hiç yazılmıyor"du. Bu aileden bir şüphede tabloyu say.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class DuyuruTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-33: Her durum geçişi bir duyuru üretir")]
    public async Task Uretir_Duyuru_HerGeciste()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya);
        await UygulaAsync(aracId, ListingAction.Reserve, medya);
        await UygulaAsync(aracId, ListingAction.Unreserve, medya);
        await UygulaAsync(aracId, ListingAction.MarkSold, medya);
        await UygulaAsync(aracId, ListingAction.Archive, medya);

        IReadOnlyList<string> tipler = await TiplerAsync();

        // SIRA İDDİA EDİLMİYOR ve bu bilinçli: `docs/architecture.md` § 2.C "sıra garantisi
        // yoktur" diyor ve outbox satırlarının kimliği `Guid.CreateVersion7()` — aynı
        // milisaniyede üretilen iki kimliğin sırası rastgele kuyruğa bağlı. İlk yazımda
        // sıralı iddia ettim ve test aralıklı kırmızı verdi.
        Assert.Equal(
            new[]
            {
                Tip<VehiclePublished>(),
                Tip<VehicleReserved>(),
                Tip<VehicleReturnedToShowcase>(),
                Tip<VehicleSold>(),
                Tip<VehicleArchived>(),
            }.Order(),
            tipler.Order());
    }

    /// <summary>
    /// **R11**/**D11** — adlar `docs/architecture.md` § 1'deki sekiz adla BİREBİR aynı.
    /// Liste belgeden okunuyor: elle yazılmış bir kopya, belge değişince sessizce yalan
    /// söylerdi.
    /// </summary>
    [Fact(DisplayName = "0012b/AC-34: Üretilen olay adları `docs/architecture.md` § 1'deki sekiz adla **birebir** aynıdır")]
    public void Ayni_OlayAdlari_BelgedekiyleBirebir()
    {
        IReadOnlySet<string> belgedeki = BelgedekiOlayAdlari();

        IReadOnlyList<string> uretilen =
        [
            .. typeof(VehiclePublished).Assembly
                .GetTypes()
                .Where(type => type.Namespace == typeof(VehiclePublished).Namespace)
                .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type))
                .Select(type => type.Name)
                .Order()
        ];

        Assert.Equal(8, uretilen.Count);
        Assert.Equal(belgedeki.Order(), uretilen);
    }

    [Fact(DisplayName = "0012b/AC-35: Olay gövdesi **yeni durumu ayrı bir alan olarak taşımaz**; yedi olay beş alan taşır, `VehiclePublished` ek olarak ilk yayın zamanını taşır (**D31**)")]
    public async Task Tasimaz_DurumAlani_OlayGovdesinde()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya);

        OutboxMessage mesaj = await TekMesajAsync();
        JsonElement govde = JsonDocument.Parse(mesaj.Payload).RootElement;

        // D30 — durumu olayın TİPİ söylüyor. Ayrı bir `status` alanı, tip ile alanın
        // ayrışabileceği ikinci bir gerçek kaynağı açardı.
        Assert.False(govde.TryGetProperty("Status", out _));
        Assert.False(govde.TryGetProperty("status", out _));

        Assert.Equal(aracId, govde.GetProperty("VehicleId").GetGuid());
        Assert.NotEqual(Guid.Empty, govde.GetProperty("EventId").GetGuid());
        Assert.StartsWith("toyota-corolla-", govde.GetProperty("Slug").GetString(), StringComparison.Ordinal);

        // D31 — tüketicinin "bu ilk yayın değil" diyebilmesi için ilk yayın zamanı.
        Assert.Equal(Simdi, govde.GetProperty("PublishedAt").GetDateTimeOffset());
    }

    [Fact(DisplayName = "0012b/AC-36: Duyuru iş verisiyle **aynı transaction'da** outbox'a yazılır; yazma başarısız olursa duyuru da yazılmaz")]
    public async Task Yazilmaz_Duyuru_GecisReddedilince()
    {
        FakeMediaAssetQueries medya = new();
        Guid aracId = await AracEkleAsync(medya.Ekle(MediaProcessingStatus.Processing));

        // Yayın REDDEDİLİYOR (hazır medya yok): ne durum değişiyor ne de duyuru yazılıyor.
        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya, basariBekle: false);

        Assert.True(sonuc.IsFailure);
        Assert.Empty(await TiplerAsync());
    }

    [Fact(DisplayName = "0012b/AC-40: Yeniden yayınlanan ilan yine duyurulur ve duyuru **ilk yayın zamanını** taşır")]
    public async Task Tasir_IlkYayinZamanini_YenidenYayinda()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya);
        await UygulaAsync(aracId, ListingAction.Unpublish, medya);
        await UygulaAsync(aracId, ListingAction.Publish, medya, an: Simdi.AddDays(30));

        IReadOnlyList<OutboxMessage> mesajlar = await MesajlarAsync();

        Assert.Equal(3, mesajlar.Count);

        // İKİ yayın duyurusu var ve ikisi de aynı ilk yayın zamanını taşıyor; hangisinin
        // önce yazıldığı iddia edilmiyor (§ 2.C: sıra garantisi yoktur).
        JsonElement ikinciYayin = mesajlar
            .Where(mesaj => mesaj.Type == Tip<VehiclePublished>())
            .Select(mesaj => JsonDocument.Parse(mesaj.Payload).RootElement)
            .Last();

        // Olay YİNE yayıldı (tüketici tazelemek zorunda) ama taşıdığı zaman İLK yayının
        // zamanı — ilan vitrinde yerini değiştirmiyor.
        Assert.Equal(Simdi, ikinciYayin.GetProperty("PublishedAt").GetDateTimeOffset());
    }

    [Fact(DisplayName = "0012b/AC-38: Fiyat ve şube değişikliği araç düzenleme komutundan duyurulur")]
    public async Task Duyurulur_FiyatVeSube_DuzenlemeKomutundan()
    {
        Guid aracId = await AracEkleAsync(null);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            FixedClock saat = new(Simdi);
            Vehicle arac = await context.Vehicles.SingleAsync(item => item.Id == new VehicleId(aracId));

            arac.ChangePrice(ListingPrice.OnRequest(), Simdi);
            arac.AssignBranch(Guid.CreateVersion7());

            await KatalogYazicilari.Birim(context, saat).SaveChangesAsync(CancellationToken.None);
        }

        IReadOnlyList<string> tipler = await TiplerAsync();

        Assert.Equal(
            new[] { Tip<VehiclePriceChanged>(), Tip<VehicleRelocated>() }.Order(),
            tipler.Order());
    }

    private static string Tip<TEvent>() => typeof(TEvent).FullName!;

    private static IReadOnlySet<string> BelgedekiOlayAdlari()
    {
        // Kök YUKARI YÜRÜNEREK bulunuyor, sabit sayıda `..` ile değil: sabit sayı derleme
        // çıktısının derinliğine bağlıdır ve değiştiğinde test eksik adı değil kendi
        // yolunu bildirir (`docs/testing.md` § 3).
        DirectoryInfo? dizin = new(AppContext.BaseDirectory);

        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "docs", "architecture.md")))
        {
            dizin = dizin.Parent;
        }

        Assert.NotNull(dizin);

        string belge = File.ReadAllText(Path.Combine(dizin.FullName, "docs", "architecture.md"));
        int baslangic = belge.IndexOf("VehicleCatalog → Vehicle", StringComparison.Ordinal);

        Assert.True(baslangic > 0, "§ 1'in olay akışı bloğu bulunamadı.");

        string blok = belge[baslangic..belge.IndexOf("Dealership     →", baslangic, StringComparison.Ordinal)];

        // `VehicleCatalog` ELENİYOR: bloğun ilk kelimesi yayıncı modülün adı ve aynı
        // desene uyuyor. İlk yazımda dokuz ad buldum ve dokuzuncusu modülün kendisiydi.
        HashSet<string> adlar = [.. System.Text.RegularExpressions.Regex
            .Matches(blok, @"\bVehicle[A-Z][A-Za-z]*\b")
            .Select(match => match.Value)
            .Where(ad => ad != "VehicleCatalog")];

        Assert.Equal(8, adlar.Count);

        return adlar;
    }

    private async Task<Guid> AracEkleAsync(Guid? medya)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"DUYURU{Interlocked.Increment(ref sayac):D11}")
            .Build();

        if (medya is not null)
        {
            arac.AttachMediaAsset(medya.Value);
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
        context.Vehicles.Add(arac);
        context.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
        await context.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task<Result> UygulaAsync(
        Guid aracId,
        ListingAction eylem,
        FakeMediaAssetQueries medya,
        bool basariBekle = true,
        DateTimeOffset? an = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(an ?? Simdi);

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            medya,
            new FakeModelNameResolver(),
            InvariantGuardOptions.Default,
            saat);

        Result sonuc = await komut.ExecuteAsync(aracId, eylem, CancellationToken.None);

        if (basariBekle)
        {
            Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);
        }

        return sonuc;
    }

    private async Task<IReadOnlyList<OutboxMessage>> MesajlarAsync()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.OutboxMessages.AsNoTracking()
            .OrderBy(message => message.Id)
            .ToListAsync();
    }

    private async Task<OutboxMessage> TekMesajAsync() => Assert.Single(await MesajlarAsync());

    private async Task<IReadOnlyList<string>> TiplerAsync() =>
        [.. (await MesajlarAsync()).Select(message => message.Type)];
}
