using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Domain.Events;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// `0023` — satışı geri alma geçişi, agregatın üzerinde ve veritabanı görmeden
/// (`docs/testing.md` § 1). Kablo — kilit, şube okuması, kayıt, yansıtma — entegrasyon
/// testlerinde (`Integration/SatisGeriAlmaTests`).
/// </summary>
public sealed class SatisGeriAlmaTests
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0023/AC-1: Yayındayken satıldı işaretlenen ilanın satışı Yönetici tarafından geri alındığında ilan **Yayında** olur ve vitrinde yeniden görünür")]
    public void Doner_Yayinda_YayindanSatildiysa()
    {
        Listing listing = Satilmis(rezerveden: false);

        Result sonuc = listing.RevertSale(canReturnToShowcase: true);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    [Fact(DisplayName = "0023/AC-2: Rezervedeyken satıldı işaretlenen ilanın satışı geri alındığında ilan **Rezerve** olur ve vitrinde rezerve olarak görünür")]
    public void Doner_Rezerve_RezervedenSatildiysa()
    {
        Listing listing = Satilmis(rezerveden: true);

        Result sonuc = listing.RevertSale(canReturnToShowcase: true);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Reserved, listing.Status);
    }

    [Fact(DisplayName = "0023/AC-6: Geri alınan ilan, onay penceresinde gösterilen duruma döner")]
    public void Doner_OnizlemedekiDuruma()
    {
        Listing listing = Satilmis(rezerveden: true);
        ListingStatus onizleme = listing.SaleRevertTarget(canReturnToShowcase: true);

        listing.RevertSale(canReturnToShowcase: true);

        Assert.Equal(onizleme, listing.Status);
    }

    [Fact(DisplayName = "0023/AC-8: Geri alınan ilanın adresi değişmez; eski adres ilanı açar")]
    public void Degismez_Adres_GeriAlmada()
    {
        Listing listing = Satilmis(rezerveden: false);
        ListingSlug? once = listing.Slug;

        listing.RevertSale(canReturnToShowcase: true);

        Assert.NotNull(once);
        Assert.Equal(once, listing.Slug);
    }

    [Fact(DisplayName = "0023/AC-9: Geri alınan ilanın ilk yayın tarihi değişmez ve vitrin sırasındaki yeri satıştan öncekiyle aynıdır")]
    public void Degismez_IlkYayinTarihi_GeriAlmada()
    {
        Listing listing = Satilmis(rezerveden: false);
        DateTimeOffset? once = listing.PublishedAt;

        listing.RevertSale(canReturnToShowcase: true);

        Assert.Equal(Simdi, once);
        Assert.Equal(once, listing.PublishedAt);
    }

    [Fact(DisplayName = "0023/AC-11: Satıştan önce öne çıkarılmış ve satılmışken dokunulmamış ilan geri alındığında öne çıkarılmış olarak döner")]
    public void Doner_OneCikarilmis_SatilmiskenDokunulmadiysa()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), new FixedClock(Simdi));
        listing.Feature();
        listing.MarkSold();

        listing.RevertSale(canReturnToShowcase: true);

        Assert.True(listing.IsFeatured);
    }

    [Fact(DisplayName = "0023/AC-12: Satılmışken öne çıkarması kaldırılan ilan geri alındığında öne çıkarılmamış olarak döner (D15)")]
    public void Doner_OneCikarilmamis_SatilmiskenKaldirildiysa()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), new FixedClock(Simdi));
        listing.Feature();
        listing.MarkSold();
        listing.Unfeature();

        listing.RevertSale(canReturnToShowcase: true);

        Assert.False(listing.IsFeatured);
    }

    [Theory(DisplayName = "0023/AC-14: 10 karakterden kısa gerekçe sunucuda reddedilir ve ilan Satıldı kalır")]
    [InlineData("kısa")]
    [InlineData("123456789")]
    [InlineData("   dokuz   ")]
    public void Reddedilir_KisaGerekce(string gerekce)
    {
        Result<string> sonuc = ListingSaleReversal.NormalizeReason(gerekce);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ListingSaleReversal.ReasonTooShort, sonuc.Error.Code);
    }

    [Fact]
    public void Kabul_Edilir_TamOnKarakter()
    {
        Result<string> sonuc = ListingSaleReversal.NormalizeReason("  0123456789  ");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("0123456789", sonuc.Value);
    }

    [Fact(DisplayName = "0023/AC-15: 500 karakterden uzun gerekçe reddedilir")]
    public void Reddedilir_UzunGerekce()
    {
        Result<string> sinirda = ListingSaleReversal.NormalizeReason(new string('a', 500));
        Result<string> asan = ListingSaleReversal.NormalizeReason(new string('a', 501));

        Assert.True(sinirda.IsSuccess);
        Assert.True(asan.IsFailure);
        Assert.Equal(ListingSaleReversal.ReasonTooLong, asan.Error.Code);
    }

    [Theory(DisplayName = "0023/AC-16: Yalnız boşluktan oluşan gerekçe boş sayılır ve reddedilir")]
    [InlineData("")]
    [InlineData("            ")]
    [InlineData("\t\n  \r\n")]
    [InlineData(null)]
    public void Reddedilir_BosGerekce(string? gerekce)
    {
        Result<string> sonuc = ListingSaleReversal.NormalizeReason(gerekce);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ListingSaleReversal.ReasonRequired, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0023/AC-22: Başarılı geri alma `VehicleSaleReverted` duyurusunu araç, ilan, adres ve dönülen durumla değişiklikle aynı işlemde outbox'a yazar (D17)")]
    public void Yayar_GeriAlmaOlayini_DonulenDurumla()
    {
        Listing listing = Satilmis(rezerveden: true);
        listing.ClearDomainEvents();

        listing.RevertSale(canReturnToShowcase: true);

        ListingSaleReverted olay = Assert.IsType<ListingSaleReverted>(Assert.Single(listing.DomainEvents));
        Assert.Equal(listing.Id, olay.ListingId);
        Assert.Equal(ListingStatus.Reserved, olay.RestoredStatus);
    }

    [Fact(DisplayName = "0023/AC-23: Yayındayken satılıp geri alınan, sonra rezerve edilip yeniden satılan ilan ikinci geri almada **Rezerve**'ye döner; iki kayıt da durur (D18)")]
    public void Doner_Rezerveye_IkinciGeriAlmada()
    {
        Listing listing = Satilmis(rezerveden: false);

        listing.RevertSale(canReturnToShowcase: true);
        ListingStatus birinci = listing.Status;
        listing.Reserve();
        listing.MarkSold();
        listing.RevertSale(canReturnToShowcase: true);

        Assert.Equal(ListingStatus.Published, birinci);
        Assert.Equal(ListingStatus.Reserved, listing.Status);
        Assert.Null(listing.StatusBeforeSale);
    }

    [Fact(DisplayName = "0023/AC-24: Arşivdeki ilan için geri alma reddedilir")]
    public void Reddedilir_Arsivde()
    {
        Listing listing = Satilmis(rezerveden: false);
        listing.Archive();

        Result sonuc = listing.RevertSale(canReturnToShowcase: true);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Archived, listing.Status);
        Assert.Null(listing.StatusBeforeSale);
    }

    [Theory(DisplayName = "0023/AC-25: Yayında, Rezerve ya da Taslak bir ilan için geri alma reddedilir")]
    [InlineData(nameof(ListingStatus.Published))]
    [InlineData(nameof(ListingStatus.Reserved))]
    [InlineData(nameof(ListingStatus.Draft))]
    public void Reddedilir_SatilmamisIlanda(string durum)
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        ListingStatus hedef = Enum.Parse<ListingStatus>(durum);

        if (hedef is not ListingStatus.Draft)
        {
            listing.Publish(Adres(listing), new FixedClock(Simdi));
        }

        if (hedef is ListingStatus.Reserved)
        {
            listing.Reserve();
        }

        listing.ClearDomainEvents();

        Result sonuc = listing.RevertSale(canReturnToShowcase: true);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(hedef, listing.Status);
        Assert.Empty(listing.DomainEvents);
    }

    private static Listing Satilmis(bool rezerveden)
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), new FixedClock(Simdi));

        if (rezerveden)
        {
            listing.Reserve();
        }

        listing.MarkSold();

        return listing;
    }

    private static ListingSlug Adres(Listing listing) =>
        ListingSlug.Create("Toyota Corolla", 2020, listing.Id).Value;
}
