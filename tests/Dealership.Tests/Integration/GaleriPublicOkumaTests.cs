using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// `0014` — galerinin ziyaretçiye dönük okuması (R1, R7 · D6, D8, D13, D15).
///
/// <para>Koleksiyon migration seed'ini paylaşıyor ve başka testler o seed'e dayanıyor
/// (`SubeIletisimSorgusuTests`). Bu yüzden her test kendi şube kümesini bir İŞLEMİN içinde
/// kurar ve işlem commit edilmeden atılır: seed hiçbir koşulda kirlenmez.</para>
/// </summary>
[Collection(DealershipDatabaseCollection.Name)]
public sealed class GaleriPublicOkumaTests(DealershipDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;
    private static readonly Locale En = Locale.Create("en").Value;

    private static int sayac;

    [Fact(DisplayName = "0014/AC-12: Ana sayfa galeri profilindeki ticari unvanı gösterir: profil \"Örnek Otomotiv\" iken sayfada bu ad görünür")]
    public async Task Doner_TicariUnvan()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await UnvanYazAsync(context, "Örnek Otomotiv");

        PublicGalleryDto galeri = await OkuAsync(context, "tr");

        Assert.Equal("Örnek Otomotiv", galeri.CommercialName);
    }

    [Fact(DisplayName = "0014/AC-13: Profilin ticari unvanı \"Başka Ad\" yapıldığında ana sayfa bir sonraki yüklemede yeni adı gösterir — ad çeviri kataloğundan gelmez")]
    public async Task Doner_YeniUnvan_DegistirildiktenSonra()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await UnvanYazAsync(context, "Örnek Otomotiv");
        Assert.Equal("Örnek Otomotiv", (await OkuAsync(context, "tr")).CommercialName);

        await UnvanYazAsync(context, "Başka Ad");

        // Önbellek yok: bir sonraki okuma yeni adı görür. Ad dile göre değişmez — çeviri
        // kataloğundan gelseydi Arapça okuma başka bir metin döndürürdü.
        Assert.Equal("Başka Ad", (await OkuAsync(context, "tr")).CommercialName);
        Assert.Equal("Başka Ad", (await OkuAsync(context, "ar")).CommercialName);
    }

    [Fact(DisplayName = "0014/AC-42: İki aktif şube varken altlık her ikisinin adını, adresini ve telefonunu gösterir")]
    public async Task Doner_IkiAktifSube_AdAdresTelefon()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        await SubeEkleAsync(context, "Ataşehir Şubesi", "Ataşehir Bulvarı 5", "+902165550001");
        await SubeEkleAsync(context, "Kadıköy Şubesi", "Bağdat Caddesi 1", "+902165550002");

        PublicGalleryDto galeri = await OkuAsync(context, "tr");

        Assert.Equal(2, galeri.Branches.Count);
        Assert.Equal(new PublicBranchDto("Ataşehir Şubesi", "Ataşehir Bulvarı 5", "+902165550001"), galeri.Branches[0]);
        Assert.Equal(new PublicBranchDto("Kadıköy Şubesi", "Bağdat Caddesi 1", "+902165550002"), galeri.Branches[1]);
    }

    [Fact(DisplayName = "0014/AC-43: geçici kapalı ve kapatılmış şubeler altlıkta yer almaz")]
    public async Task Gostermez_GeciciKapaliVeKapatilmisSube()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        await SubeEkleAsync(context, "Açık Şube", "Adres 1", "+902165550001");
        Branch gecici = await SubeEkleAsync(context, "Geçici Kapalı Şube", "Adres 2", "+902165550002");
        Branch kapali = await SubeEkleAsync(context, "Kapatılmış Şube", "Adres 3", "+902165550003");

        Assert.True(gecici.CloseTemporarily().IsSuccess);
        Assert.True(kapali.Close(otherActiveBranchCount: 1).IsSuccess);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        PublicGalleryDto galeri = await OkuAsync(context, "tr");

        Assert.Equal(["Açık Şube"], galeri.Branches.Select(branch => branch.Name));
    }

    [Fact(DisplayName = "0014/AC-48: Ziyaretçiye dönük okuma yanıtı geçici kapalı ve kapatılmış şubeleri hiç taşımaz; süzme sunucuda yapılır, uçta değil")]
    public async Task Tasimaz_KapaliSubeleri_SorguSuzer()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        Branch gecici = await SubeEkleAsync(context, "Geçici Kapalı Şube", "Adres 2", "+902165550002");
        Branch kapali = await SubeEkleAsync(context, "Kapatılmış Şube", "Adres 3", "+902165550003");
        await SubeEkleAsync(context, "Açık Şube", "Adres 1", "+902165550001");
        Assert.True(gecici.CloseTemporarily().IsSuccess);
        Assert.True(kapali.Close(otherActiveBranchCount: 1).IsSuccess);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        PublicGalleryDto galeri = await OkuAsync(context, "tr");

        // Yanıt uca giden şeyin TAMAMI; kapalı şube burada yoksa uç onu hiç görmez.
        Assert.Single(galeri.Branches);
        Assert.DoesNotContain(galeri.Branches, branch => branch.Name != "Açık Şube");
    }

    [Fact(DisplayName = "0014/AC-44: Arapça adresi girilmemiş bir şube, Arapça sayfada adresini varsayılan dildeki metniyle gösterir; satır boş kalmaz")]
    public async Task Doner_VarsayilanAdres_ArapcaAdresYoksa()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        await SubeEkleAsync(
            context,
            Metin((Tr, "Kadıköy Şubesi"), (Ar, "فرع كاديكوي")),
            Metin((Tr, "Bağdat Caddesi 1")),
            "+902165550002");

        PublicBranchDto sube = Assert.Single((await OkuAsync(context, "ar")).Branches);

        Assert.Equal("فرع كاديكوي", sube.Name);
        Assert.Equal("Bağdat Caddesi 1", sube.Address);
    }

    [Fact(DisplayName = "0014/AC-68: Arapça adı girilmemiş bir şube, Arapça sayfada adını varsayılan dildeki metniyle gösterir; satır boş kalmaz")]
    public async Task Doner_VarsayilanAd_ArapcaAdYoksa()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        await SubeEkleAsync(
            context,
            Metin((Tr, "Kadıköy Şubesi"), (En, "Kadikoy Branch")),
            Metin((Tr, "Bağdat Caddesi 1"), (Ar, "شارع بغداد ١")),
            "+902165550002");

        PublicBranchDto sube = Assert.Single((await OkuAsync(context, "ar")).Branches);

        Assert.Equal("Kadıköy Şubesi", sube.Name);
        Assert.Equal("شارع بغداد ١", sube.Address);
    }

    [Fact(DisplayName = "0014/AC-66: Şubeler varsayılan dildeki adın ordinal sırasıyla dizilir: önce \"Bakırköy\", sonra \"Ataşehir\" adlı şube kaydedilmişken altlıkta \"Ataşehir\" önce gelir")]
    public async Task Siralar_VarsayilanDildekiAdinOrdinalSirasiyla()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await SubeleriTemizleAsync(context);
        await SubeEkleAsync(context, Metin((Tr, "Bakırköy"), (En, "A-Bakirkoy")), Metin((Tr, "Adres 1")), "+902125550001");
        await SubeEkleAsync(context, Metin((Tr, "Ataşehir"), (En, "Z-Atasehir")), Metin((Tr, "Adres 2")), "+902165550002");

        // İngilizce adlar ters sırada seçildi: sıra ziyaretçinin dilinden DEĞİL varsayılan
        // dildeki addan gelir, yani İngilizce sayfada da "Ataşehir" (Z-Atasehir) önce.
        Assert.Equal(["Ataşehir", "Bakırköy"], (await OkuAsync(context, "tr")).Branches.Select(branch => branch.Name));
        Assert.Equal(["Z-Atasehir", "A-Bakirkoy"], (await OkuAsync(context, "en")).Branches.Select(branch => branch.Name));
    }

    private static async Task<PublicGalleryDto> OkuAsync(DealershipDbContext context, string locale)
    {
        PublicGalleryDto? galeri = await new PublicGalleryQueries(context, Tr).GetAsync(locale, CancellationToken.None);
        Assert.NotNull(galeri);
        return galeri;
    }

    private static Task UnvanYazAsync(DealershipDbContext context, string unvan) =>
        context.Galleries.ExecuteUpdateAsync(setters => setters.SetProperty(gallery => gallery.CommercialName, unvan));

    private static Task SubeleriTemizleAsync(DealershipDbContext context) =>
        context.Database.ExecuteSqlRawAsync($"DELETE FROM {DealershipDbContext.Schema}.branches");

    private static Task<Branch> SubeEkleAsync(DealershipDbContext context, string ad, string adres, string telefon) =>
        SubeEkleAsync(context, Metin((Tr, ad)), Metin((Tr, adres)), telefon);

    private static async Task<Branch> SubeEkleAsync(
        DealershipDbContext context,
        LocalizedText adlar,
        LocalizedText adresler,
        string telefon)
    {
        Branch sube = Branch.Open(
            adlar,
            adresler,
            PhoneNumber.Create(telefon).Value,
            TimeZoneId.Create("Europe/Istanbul").Value,
            BranchSlug.Create($"galeri-okuma-{Interlocked.Increment(ref sayac)}").Value,
            Tr).Value;

        context.Branches.Add(sube);
        await context.SaveChangesAsync();
        return sube;
    }

    private static LocalizedText Metin(params (Locale Locale, string Value)[] degerler) =>
        LocalizedText.From(degerler.Select(item => new KeyValuePair<Locale, string>(item.Locale, item.Value)));
}
