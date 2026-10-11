using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// **A3**'ün geçiş tablosu, agregatın üzerinde ve veritabanı görmeden (`docs/testing.md`
/// § 1 — domain birim testi).
///
/// <para>`ListingTests` yayın tarihinin kararlılığını ve birkaç reddi zaten ölçüyordu;
/// burası tablonun **tamamını** satır satır sınıyor — `0012b` A3'e iki geçiş ekledi
/// (`Taslak → Arşiv` **D10**, `Rezerve → Taslak` **D32**) ve tablonun tamamı tek yerde
/// görünmeden "başka geçiş yoktur" iddiası doğrulanamaz.</para>
/// </summary>
public sealed class GecisTablosuTests
{
    private static readonly DateTimeOffset Simdi = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0012b/AC-11: `Yayında` bir ilan rezerve edilir ve `Rezerve` durumuna geçer")]
    public void Gecer_Rezerveye_Yayindaykén()
    {
        Listing listing = Durumda(ListingStatus.Published);

        Result sonuc = listing.Reserve();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Reserved, listing.Status);
    }

    [Fact(DisplayName = "0012b/AC-12: `Rezerve` bir ilanın rezervasyonu kaldırılır ve ilan `Yayında` durumuna döner")]
    public void Doner_Yayinda_RezervasyonKaldirilinca()
    {
        Listing listing = Durumda(ListingStatus.Reserved);

        Result sonuc = listing.ReturnToShowcase();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    [Theory(DisplayName = "0012b/AC-13: `Yayında` ve `Rezerve` bir ilan satıldı olarak işaretlenir")]
    [InlineData(nameof(ListingStatus.Published))]
    [InlineData(nameof(ListingStatus.Reserved))]
    public void Isaretlenir_Satildi_YayindaVeRezerveden(string durum)
    {
        Listing listing = Durumda(Enum.Parse<ListingStatus>(durum));

        Result sonuc = listing.MarkSold();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Sold, listing.Status);
    }

    [Fact(DisplayName = "0012b/AC-14: `Satıldı` terminaldir: araç yeniden yayına alınamaz")]
    public void Alinamaz_Yayina_SatildiktanSonra()
    {
        Listing listing = Durumda(ListingStatus.Sold);

        // A4 — yanlış işaretleme YENİ KAYIT açılarak düzeltilir; arayüzde "geri al"
        // düğmesi kuralı delerdi.
        Result sonuc = listing.Publish(Adres(listing), new FixedClock(Simdi));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Sold, listing.Status);
    }

    [Fact(DisplayName = "0012b/AC-15: `Yayında` bir ilan yayından kaldırılır ve `Taslak` durumuna döner")]
    public void Doner_Taslaga_YayindanKaldirilinca()
    {
        Listing listing = Durumda(ListingStatus.Published);

        Result sonuc = listing.Unpublish();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Theory(DisplayName = "0012b/AC-16: `Yayında` ve `Satıldı` bir ilan arşivlenir")]
    [InlineData(nameof(ListingStatus.Published))]
    [InlineData(nameof(ListingStatus.Sold))]
    public void Arsivlenir_YayindaVeSatildi(string durum)
    {
        Listing listing = Durumda(Enum.Parse<ListingStatus>(durum));

        Result sonuc = listing.Archive();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Archived, listing.Status);
    }

    /// <summary>
    /// **D10** — `Taslak → Arşiv`. Geçiş 2026-10-11'de `docs/domain.md` A3'e eklendi:
    /// silme bu projede `Arşiv` durumudur (`0010b` X19) ve şase tekilliği `Arşiv`
    /// kayıtlarını muaf tutuyor (A5). İkisi birlikte, yanlış şaseyle açılmış ve hiç
    /// yayınlanmamış bir taslağı ne silinebilir ne arşivlenebilir bırakıyordu.
    /// </summary>
    [Fact(DisplayName = "0012b/AC-17: `Taslak` bir ilan arşivlenir")]
    public void Arsivlenir_Taslak()
    {
        Listing listing = Durumda(ListingStatus.Draft);

        Result sonuc = listing.Archive();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Archived, listing.Status);

        // Hiç yayınlanmamış ilanın ADRESİ de yok (AC-49): arşivlenmek onu üretmez.
        Assert.Null(listing.Slug);
    }

    /// <summary>
    /// **D32** — `Rezerve → Taslak`. Önceki hâli yalnız `Yayında`dan geçiyordu ve A8 şube
    /// kapandığında rezerve bir ilanı **çıkışı olmayan** bir hâlde bırakıyordu: vitrinden
    /// bayrakla düşüyor, durumu `Rezerve` kalıyor ve oradan tek çıkış `Satıldı` oluyordu.
    /// </summary>
    [Fact(DisplayName = "0012b/AC-18: `Rezerve` bir ilan yayından kaldırılır ve `Taslak` durumuna döner")]
    public void Doner_Taslaga_RezerveIlanYayindanKaldirilinca()
    {
        Listing listing = Durumda(ListingStatus.Reserved);

        Result sonuc = listing.Unpublish();

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    /// <summary>
    /// A3'te YAZILI OLMAYAN her geçiş. Tablo on dört hücre: beş durum × altı eylem
    /// eksi geçerli olanlar; burada hepsi tek tek reddediliyor.
    ///
    /// <para><b>Vaka listesinin boşalmaması ayrıca sınanıyor</b> (`docs/testing.md` § 3):
    /// sıfır vakayla koşan bir `Theory` yeşildir ve kapı sessizce açılmış olur.</para>
    /// </summary>
    [Theory(DisplayName = "0012b/AC-19: A3'te yazılı olmayan her geçiş `listing.invalid_status_transition` ile reddedilir")]
    [InlineData(nameof(ListingStatus.Draft), "reserve")]
    [InlineData(nameof(ListingStatus.Draft), "unreserve")]
    [InlineData(nameof(ListingStatus.Draft), "sold")]
    [InlineData(nameof(ListingStatus.Draft), "unpublish")]
    [InlineData(nameof(ListingStatus.Published), "publish")]
    [InlineData(nameof(ListingStatus.Published), "unreserve")]
    [InlineData(nameof(ListingStatus.Reserved), "publish")]
    [InlineData(nameof(ListingStatus.Reserved), "reserve")]
    [InlineData(nameof(ListingStatus.Reserved), "archive")]
    [InlineData(nameof(ListingStatus.Sold), "reserve")]
    [InlineData(nameof(ListingStatus.Sold), "unreserve")]
    [InlineData(nameof(ListingStatus.Sold), "sold")]
    [InlineData(nameof(ListingStatus.Sold), "unpublish")]
    [InlineData(nameof(ListingStatus.Archived), "publish")]
    [InlineData(nameof(ListingStatus.Archived), "reserve")]
    [InlineData(nameof(ListingStatus.Archived), "unreserve")]
    [InlineData(nameof(ListingStatus.Archived), "sold")]
    [InlineData(nameof(ListingStatus.Archived), "unpublish")]
    [InlineData(nameof(ListingStatus.Archived), "archive")]
    public void Reddedilir_A3teYazmayanGecis(string durum, string eylem)
    {
        ListingStatus baslangic = Enum.Parse<ListingStatus>(durum);
        Listing listing = Durumda(baslangic);

        Result sonuc = Uygula(listing, eylem);

        Assert.True(sonuc.IsFailure, $"{durum} → {eylem} kabul edildi.");
        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(baslangic, listing.Status);
    }

    [Fact]
    public void Bos_Degildir_GecersizGecisVakaListesi()
    {
        // `Theory` sıfır vakayla da yeşildir; vaka sayısı burada sabitleniyor.
        int vaka = typeof(GecisTablosuTests)
            .GetMethod(nameof(Reddedilir_A3teYazmayanGecis))!
            .GetCustomAttributes(typeof(InlineDataAttribute), inherit: false)
            .Length;

        Assert.Equal(19, vaka);
    }

    private static Result Uygula(Listing listing, string eylem) => eylem switch
    {
        "publish" => listing.Publish(Adres(listing), new FixedClock(Simdi)),
        "unpublish" => listing.Unpublish(),
        "reserve" => listing.Reserve(),
        "unreserve" => listing.ReturnToShowcase(),
        "sold" => listing.MarkSold(),
        _ => listing.Archive(),
    };

    private static Listing Durumda(ListingStatus status)
    {
        FixedClock clock = new(Simdi);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        if (status is ListingStatus.Draft)
        {
            return listing;
        }

        listing.Publish(Adres(listing), clock);
        listing.ClearDomainEvents();

        switch (status)
        {
            case ListingStatus.Reserved:
                listing.Reserve();
                break;
            case ListingStatus.Sold:
                listing.MarkSold();
                break;
            case ListingStatus.Archived:
                listing.Archive();
                break;
            case ListingStatus.Draft:
            case ListingStatus.Published:
            default:
                break;
        }

        listing.ClearDomainEvents();

        return listing;
    }

    private static ListingSlug Adres(Listing listing) =>
        ListingSlug.Create("Toyota Corolla", 2020, listing.Id).Value;
}
