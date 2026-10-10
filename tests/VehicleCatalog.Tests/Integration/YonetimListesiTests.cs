using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Application;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Yönetim listesi: sayfalama, iki sıralama ekseni ve iki gösterge (0010b E grubu).
///
/// <para>Gerçek PostgreSQL şart: sınanan şey EF'in ürettiği SORGU — sıralama kırıcısı,
/// `Skip/Take` ve durum eşlemesi. Sahte bir depo hiçbirini kanıtlamazdı.</para>
///
/// <para>Her test KENDİ araçlarını kuruyor ve onları kendi kimlikleriyle buluyor;
/// paylaşılan duruma bağımlı test yazılmaz (docs/testing.md § 4) ve koleksiyon
/// veritabanı testler arasında paylaşılıyor.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class YonetimListesiTests(ShowcaseDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale En = Locale.Create("en").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;

    [Fact]
    public async Task Esler_HerAracinKendiDurumunu()
    {
        Vehicle taslak = await KaydetAsync("DURUM");
        Vehicle yayinda = await KaydetAsync("DURUM", yayinla: true);

        IReadOnlyList<VehicleAdminListItemDto> liste = await TumSayfalarAsync();

        Assert.Equal(
            nameof(ListingStatus.Published),
            liste.Single(item => item.VehicleId == yayinda.Id.Value).ListingStatus);

        Assert.Equal(
            nameof(ListingStatus.Draft),
            liste.Single(item => item.VehicleId == taslak.Id.Value).ListingStatus);
    }

    /// <summary>Şase YÖNETİM yanıtında bulunur (`0010a` AC-19); vitrinde hiç bulunmaz (AC-18).</summary>
    [Fact]
    public async Task Tasir_SaseNumarasini()
    {
        Vehicle arac = await KaydetAsync("SASE");

        IReadOnlyList<VehicleAdminListItemDto> liste = await TumSayfalarAsync();

        Assert.Equal(arac.Vin.Value, liste.Single(item => item.VehicleId == arac.Id.Value).Vin);
    }

    [Fact(DisplayName = "0010b/AC-24: Liste araç tanımını, fiyatı, kilometreyi, yayın durumunu, eksik dil göstergesini, eksik bilgi göstergesini ve son güncelleme zamanını gösterir")]
    public async Task Gosterir_YediSutunu()
    {
        DateTimeOffset an = new(2026, 10, 10, 11, 0, 0, TimeSpan.Zero);
        Vehicle arac = await KaydetAsync("SUTUN", guncelleme: an, metin: [("tr", true), ("en", true)]);

        VehicleAdminListItemDto satir = (await TumSayfalarAsync()).Single(item => item.VehicleId == arac.Id.Value);

        Assert.Equal(2019, satir.ModelYear);
        Assert.Equal(125_000, satir.MileageKilometers);
        Assert.Equal(1_250_000m, satir.Price.Amount);
        Assert.Equal(2, satir.Price.MinorUnitExponent);
        Assert.Equal(nameof(ListingStatus.Draft), satir.ListingStatus);
        Assert.Equal(an, satir.UpdatedAt);

        // Arapça metin yok: eksik dil göstergesi onu söylüyor (AC-22).
        Assert.Equal(["ar"], satir.MissingLocales);
        Assert.False(satir.HasMissingRequiredInfo);
    }

    [Fact(DisplayName = "0010b/AC-21: Bir dil, başlık ve açıklamasının ikisi de doluysa tam sayılır; yalnız biri dolu olan dil eksik sayılır")]
    public async Task Eksiktir_YalnizBasligiOlanDil()
    {
        // Arapça'da yalnız BAŞLIK var; açıklama yok.
        Vehicle arac = await KaydetAsync("YARIM", metin: [("tr", true), ("en", true), ("ar", false)]);

        VehicleAdminListItemDto satir = (await TumSayfalarAsync()).Single(item => item.VehicleId == arac.Id.Value);

        Assert.Equal(["ar"], satir.MissingLocales);
    }

    [Fact(DisplayName = "0010b/AC-25: Liste yanıtı `PagedResult<T>` biçimindedir ve sayfa, sayfa boyutu ile toplam kayıt sayısını taşır")]
    public async Task Tasir_SayfalamaZarfini()
    {
        await KaydetAsync("ZARF");

        PagedResult<VehicleAdminListItemDto> sayfa = await Sorgu().ListAsync(Filtre(page: 1, pageSize: 5), CancellationToken.None);

        Assert.Equal(1, sayfa.Page);
        Assert.Equal(5, sayfa.PageSize);
        Assert.True(sayfa.TotalCount >= 1);
        Assert.Equal((int)Math.Ceiling(sayfa.TotalCount / 5.0), sayfa.TotalPages);
    }

    [Fact(DisplayName = "0010b/AC-26: Üst sınırı aşan bir `pageSize` isteği üst sınıra kırpılır ve hata dönmez (C26)")]
    public async Task Kirpilir_UstSiniriAsanSayfaBoyutu()
    {
        await KaydetAsync("KIRP");

        PagedResult<VehicleAdminListItemDto> sayfa =
            await Sorgu().ListAsync(Filtre(page: 1, pageSize: 5_000), CancellationToken.None);

        Assert.Equal(100, sayfa.PageSize);
    }

    [Fact(DisplayName = "0010b/AC-27: Yapılandırılan sayfa boyutunun bir fazlası kadar araç varken ikinci sayfa tam olarak bir araç gösterir")]
    public async Task Gosterir_IkinciSayfadaTamBirArac()
    {
        // Sayfa boyutu testte KÜÇÜK tutuluyor: 25 aracı kurmak testi yavaşlatır ve
        // ölçülen şey sayının kendisi değil, `Skip/Take` aritmetiği.
        await TemizleAsync();

        for (int i = 0; i < 4; i++)
        {
            await KaydetAsync($"SAY{i}");
        }

        PagedResult<VehicleAdminListItemDto> ikinci =
            await Sorgu().ListAsync(Filtre(page: 2, pageSize: 3), CancellationToken.None);

        Assert.Single(ikinci.Items);
        Assert.Equal(4, ikinci.TotalCount);
        Assert.Equal(2, ikinci.TotalPages);
    }

    [Fact(DisplayName = "0010b/AC-28: Ekleme ve güncelleme sıraları birbirinden farklı olan en az üç araçla, son güncellenen ve yeni eklenen sıralamaları farklı sıralar üretir")]
    public async Task Uretir_IkiSiralamaFarkliSira()
    {
        await TemizleAsync();

        // Ekleme sırası A → B → C; güncelleme sırası C → A → B.
        Vehicle a = await KaydetAsync("SIRA_A", guncelleme: new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero));
        Vehicle b = await KaydetAsync("SIRA_B", guncelleme: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        Vehicle c = await KaydetAsync("SIRA_C", guncelleme: new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero));

        Guid[] sonGuncellenen = await SiraAsync(VehicleAdminListSort.LastUpdated);
        Guid[] yeniEklenen = await SiraAsync(VehicleAdminListSort.RecentlyAdded);

        Assert.Equal([c.Id.Value, a.Id.Value, b.Id.Value], sonGuncellenen);
        Assert.Equal([c.Id.Value, b.Id.Value, a.Id.Value], yeniEklenen);
        Assert.NotEqual(sonGuncellenen, yeniEklenen);
    }

    /// <summary>
    /// Kırıcı olmadan aynı dakikada güncellenmiş iki araç kararsız sıralanır ve aynı araç
    /// iki sayfada görünebilir — `0001`'in SCORECARD'ında kayıtlı ders. Ölçüt BİRİNCİ ve
    /// İKİNCİ sayfanın BİRLİKTE alınması: çakışma yok, atlama yok.
    /// </summary>
    [Fact(DisplayName = "0010b/AC-29: Son güncelleme zamanı aynı olan araçlarda sıralama araç kimliğiyle kırılır; birinci ve ikinci sayfa birlikte hiçbir aracı tekrarlamaz ve hiçbirini atlamaz")]
    public async Task Kirar_EsitlikleriKimlikle()
    {
        await TemizleAsync();

        DateTimeOffset ayniAn = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        List<Guid> beklenen = [];

        for (int i = 0; i < 6; i++)
        {
            beklenen.Add((await KaydetAsync($"ESIT{i}", guncelleme: ayniAn)).Id.Value);
        }

        PagedResult<VehicleAdminListItemDto> birinci =
            await Sorgu().ListAsync(Filtre(page: 1, pageSize: 3), CancellationToken.None);

        // İki sayfa arasında bir satır GÜNCELLENİYOR — ama sıralama anahtarı DEĞİŞMİYOR.
        //
        // Bu adım olmadan test hiçbir şey kanıtlamıyor: ölçüldü, kırıcı kaldırıldığında
        // da yeşil kalıyordu, çünkü küçük bir tabloda PostgreSQL ardışık taramayı her
        // iki sorguda aynı sırayla döndürüyor. Güncelleme satırı heap'in sonuna taşıyor
        // ve sırasız bir sorguda ikinci sayfanın içeriği kayıyor: bir araç tekrarlanıyor,
        // bir diğeri atlanıyor. Senaryo uydurma da değil — başka bir danışman aynı anda
        // bir aracı düzenliyor.
        await KilometreDegistirAsync(birinci.Items[0].VehicleId);

        PagedResult<VehicleAdminListItemDto> ikinci =
            await Sorgu().ListAsync(Filtre(page: 2, pageSize: 3), CancellationToken.None);

        Guid[] birlikte = [.. birinci.Items.Select(item => item.VehicleId), .. ikinci.Items.Select(item => item.VehicleId)];

        Assert.Equal(6, birlikte.Length);
        Assert.Equal(6, birlikte.Distinct().Count());
        Assert.Equal([.. beklenen.OrderBy(id => id)], [.. birlikte.OrderBy(id => id)]);
    }

    /// <summary>
    /// Özet sorgusu `listings`'ten YALNIZ iki kolon çekiyor (BULGU-3). Önceki hâli tüm
    /// varlığı çekiyordu — durum, ağırlık, yayın zamanı dâhil — ve üstündeki yorum "dar
    /// projeksiyon" diyordu. Yorumla kod ayrışınca bir sonraki okuyucu oraya hiç bakmaz.
    ///
    /// <para>Ölçüm ÜRETİM YOLUNDA: `GetSummaryAsync`'in gerçekten çalıştırdığı SQL
    /// yakalanıyor. Testin kendi kurduğu bir sorguyu ölçmek yalnız EF'in çalıştığını
    /// gösterirdi, kodun o sorguyu kullandığını değil.</para>
    /// </summary>
    [Fact]
    public async Task Okur_OzetiYalnizIkiKolondan()
    {
        SorguYakalayici yakalayici = new();

        await using VehicleCatalogDbContext context = database.CreateContext(yakalayici);

        await new VehicleAdminQueries(
            context,
            new SahteModelSorgusu(),
            new ShowcaseLocaleOptions(Tr, [Tr, En, Ar]),
            new CatalogAdminPagingOptions(25, 100)).GetSummaryAsync(CancellationToken.None);

        string? ilanSorgusu = yakalayici.Sorgular
            .FirstOrDefault(sorgu => sorgu.Contains("listings", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(ilanSorgusu);
        Assert.Contains("title", ilanSorgusu, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("description", ilanSorgusu, StringComparison.OrdinalIgnoreCase);

        foreach (string kolon in new[] { "status", "showcase_sort_weight", "published_at" })
        {
            Assert.DoesNotContain(kolon, ilanSorgusu, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "0010b/AC-23: Liste, çeviri eksik ilan sayısını tüm araçlar üzerinden gösterir; gösterilen sayı o anki sayfanın içeriğine bağlı değildir")]
    public async Task Sayar_CeviriEksigiTumAraclarUzerinden()
    {
        await TemizleAsync();

        await KaydetAsync("OZET1", metin: [("tr", true), ("en", true), ("ar", true)]);
        await KaydetAsync("OZET2", metin: [("tr", true)]);
        await KaydetAsync("OZET3");

        VehicleAdminSummaryDto ozet = await Sorgu().GetSummaryAsync(CancellationToken.None);

        Assert.Equal(3, ozet.TotalCount);
        Assert.Equal(2, ozet.TranslationIncompleteCount);

        // Sayı SAYFADAN bağımsız: tek araçlık sayfa da aynı özeti verir.
        PagedResult<VehicleAdminListItemDto> sayfa =
            await Sorgu().ListAsync(Filtre(page: 2, pageSize: 1), CancellationToken.None);

        Assert.Single(sayfa.Items);
        Assert.Equal(2, (await Sorgu().GetSummaryAsync(CancellationToken.None)).TranslationIncompleteCount);
    }

    /// <summary>
    /// C21 — "eksik bilgi" SAKLANMIYOR, sorguda hesaplanıyor. Ayar değiştiğinde hiçbir
    /// yazma olmadan değişmesi gerekiyor; bu test ikisini de ölçüyor (AC-16).
    /// </summary>
    [Fact(DisplayName = "0010b/AC-16: Bir alan zorunlu yapıldığında, o alanı boş olan mevcut araçlar düzenlemeye kapanmaz ve listede \"eksik bilgi\" olarak işaretlenir")]
    public async Task Isaretler_EksikBilgiyi_AyarDegisinceYazmaOlmadan()
    {
        Vehicle arac = await KaydetAsync("EKSIK");

        VehicleAdminListItemDto once = (await TumSayfalarAsync()).Single(item => item.VehicleId == arac.Id.Value);

        Assert.False(once.HasMissingRequiredInfo);

        await AyarDegistirAsync(FieldFormState.Required);

        try
        {
            VehicleAdminListItemDto sonra = (await TumSayfalarAsync()).Single(item => item.VehicleId == arac.Id.Value);

            Assert.True(sonra.HasMissingRequiredInfo);

            // Araca HİÇBİR yazma olmadı: güncelleme zamanı değişmedi.
            Assert.Equal(once.UpdatedAt, sonra.UpdatedAt);
        }
        finally
        {
            await AyarDegistirAsync(FieldFormState.Optional);
        }
    }

    [Fact(DisplayName = "0010b/AC-14: Alan ayarlarında kapalı bir opsiyonel alan formda görünmez")]
    public async Task Gormez_KapaliAlani_FormAlanlariListesinde()
    {
        IReadOnlyList<VehicleAdminFieldDto> acik = await Sorgu().GetFormFieldsAsync(CancellationToken.None);

        Assert.Contains(acik, field => field.FieldKey == VehicleFieldKeys.DamageRecordAmount);
        Assert.Equal("Amount", acik.Single(field => field.FieldKey == VehicleFieldKeys.DamageRecordAmount).Kind);

        // Çekirdek alan form listesinde YOK: o agregatın kendi alanı.
        Assert.DoesNotContain(acik, field => field.FieldKey == VehicleFieldKeys.BodyType);

        await AyarDegistirAsync(FieldFormState.Closed);

        try
        {
            Assert.Empty(await Sorgu().GetFormFieldsAsync(CancellationToken.None));
        }
        finally
        {
            await AyarDegistirAsync(FieldFormState.Optional);
        }
    }

    [Fact(DisplayName = "0010b/AC-17: Kapatılan bir alanın daha önce girilmiş değeri silinmez; alan yeniden açıldığında eski değer görünür")]
    public async Task Gizler_KapaliAlanDegerini_SilmedenVeGeriVerir()
    {
        Vehicle arac = await KaydetAsync("GIZLI");

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            Vehicle izlenen = await yazma.Vehicles
                .Include(item => item.OptionalFieldValues)
                .SingleAsync(item => item.Id == arac.Id);

            izlenen.SetOptionalFieldValue(
                VehicleFieldKeys.DamageRecordAmount,
                OptionalFieldValue.OfAmount(Money.Create(9_900m, "TRY", 2).Value));

            await yazma.SaveChangesAsync();
        }

        await AyarDegistirAsync(FieldFormState.Closed);

        try
        {
            VehicleAdminDetailDto? kapali = await Sorgu().FindAsync(arac.Id.Value, CancellationToken.None);

            // F3 — yanıta GİRMEZ...
            Assert.Empty(kapali!.OptionalValues);

            // ...ama F1 — veri DURUYOR.
            await using VehicleCatalogDbContext okuma = database.CreateContext();
            Vehicle saklanan = await okuma.Vehicles
                .AsNoTracking()
                .Include(item => item.OptionalFieldValues)
                .SingleAsync(item => item.Id == arac.Id);

            Assert.Equal(9_900m, saklanan.OptionalFieldValues.Single().Value.Amount!.Value.Amount);
        }
        finally
        {
            await AyarDegistirAsync(FieldFormState.Optional);
        }

        VehicleAdminDetailDto? acik = await Sorgu().FindAsync(arac.Id.Value, CancellationToken.None);

        Assert.Equal("9900.00", acik!.OptionalValues.Single().Value);
    }

    [Fact]
    public async Task Doner_BulunamayanAracIcinNull()
    {
        Assert.Null(await Sorgu().FindAsync(Guid.CreateVersion7(), CancellationToken.None));
    }

    // --- yardımcılar ---

    private VehicleAdminQueries Sorgu() => new(
        database.CreateContext(),
        new SahteModelSorgusu(),
        new ShowcaseLocaleOptions(Tr, [Tr, En, Ar]),
        new CatalogAdminPagingOptions(25, 100));

    private static VehicleAdminListFilter Filtre(
        int page = 1,
        int? pageSize = null,
        VehicleAdminListSort sort = VehicleAdminListSort.LastUpdated) =>
        new("tr", page, pageSize, sort);

    private async Task<Guid[]> SiraAsync(VehicleAdminListSort sort)
    {
        PagedResult<VehicleAdminListItemDto> sayfa =
            await Sorgu().ListAsync(Filtre(pageSize: 100, sort: sort), CancellationToken.None);

        return [.. sayfa.Items.Select(item => item.VehicleId)];
    }

    private async Task<IReadOnlyList<VehicleAdminListItemDto>> TumSayfalarAsync() =>
        (await Sorgu().ListAsync(Filtre(pageSize: 100), CancellationToken.None)).Items;

    private async Task<Vehicle> KaydetAsync(
        string prefix,
        bool yayinla = false,
        DateTimeOffset? guncelleme = null,
        (string Locale, bool Aciklama)[]? metin = null)
    {
        Vehicle arac = new VehicleBuilder().WithVin($"{prefix}{Guid.CreateVersion7():N}"[..24]).Build();
        arac.Touch(guncelleme ?? new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));

        Listing ilan = Listing.CreateDraft(arac.Id, showcaseSortWeight: 0);

        if (metin is not null)
        {
            ilan.SetText(
                LocalizedText.From(metin.Select(item =>
                    new KeyValuePair<Locale, string>(Locale.Create(item.Locale).Value, "Başlık"))),
                LocalizedText.From(metin
                    .Where(item => item.Aciklama)
                    .Select(item => new KeyValuePair<Locale, string>(Locale.Create(item.Locale).Value, "Açıklama"))));
        }

        if (yayinla)
        {
            ilan.Publish(new FixedClock(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(ilan);
        await context.SaveChangesAsync();

        return arac;
    }

    /// <summary>
    /// Sayfalama ve sıralama testleri TOPLAM sayıya bakıyor; koleksiyon veritabanı
    /// paylaşıldığı için önceki testlerin araçları sayıyı bozardı.
    /// </summary>
    private async Task TemizleAsync()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        await context.Listings.ExecuteDeleteAsync();
        await context.Vehicles.ExecuteDeleteAsync();
    }

    /// <summary>
    /// Satırı günceller ama `updated_at`'e DOKUNMAZ: amaç sıralama anahtarını
    /// değiştirmeden satırın fiziksel yerini oynatmak.
    /// </summary>
    private async Task KilometreDegistirAsync(Guid vehicleId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        await context.Vehicles
            .Where(vehicle => vehicle.Id == new VehicleId(vehicleId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                vehicle => vehicle.MileageKilometers,
                vehicle => vehicle.MileageKilometers + 1));
    }

    private async Task AyarDegistirAsync(FieldFormState durum)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        VehicleFieldSetting ayar = await context.VehicleFieldSettings
            .SingleAsync(item => item.FieldKey == VehicleFieldKeys.DamageRecordAmount);

        ayar.FormState = durum;
        await context.SaveChangesAsync();
    }
}
