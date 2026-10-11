using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// `0012c` **R5** — öne çıkarmanın agregattaki kuralları. Veritabanı yok: kural saf ve
/// `docs/testing.md` § 1'in önceliği "durum geçişleri ve invariantlar her zaman" diyor.
///
/// <para>Öne çıkarma bir durum geçişi DEĞİL ve bu testler tam olarak onu ölçüyor:
/// idempotentlik (**D48**) geçişlerin katılığından bilinçli bir ayrılık.</para>
/// </summary>
public sealed class OneCikarmaTests
{
    [Fact(DisplayName = "0012c/AC-26: Öne çıkarma **idempotenttir**: zaten öne çıkarılmış kayıt **başarılı** döner")]
    public void Doner_Basarili_ZatenOneCikarilmissa()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), Listing.NormalWeight);

        Assert.True(listing.Feature().IsSuccess);

        Result ikinci = listing.Feature();

        Assert.True(ikinci.IsSuccess);
        Assert.True(listing.IsFeatured);
    }

    [Fact(DisplayName = "0012c/AC-27: Öne çıkarmayı kaldırma da **idempotenttir**: zaten öne çıkarılmamış kayıt **başarılı** döner")]
    public void Doner_Basarili_ZatenOneCikarilmamissa()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), Listing.NormalWeight);

        Result sonuc = listing.Unfeature();

        Assert.True(sonuc.IsSuccess);
        Assert.False(listing.IsFeatured);
    }

    [Fact(DisplayName = "0012c/AC-28: Arşivlenmiş ilan öne çıkarılamaz: yanıt `listing.cannot_feature.archived` ve `409` taşır")]
    public void OneCikarilamaz_Arsivdeyse()
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), Listing.NormalWeight);

        Assert.True(listing.Archive().IsSuccess);

        Result oneCikar = listing.Feature();
        Result kaldir = listing.Unfeature();

        Assert.True(oneCikar.IsFailure);
        Assert.Equal(Listing.CannotFeatureArchived, oneCikar.Error.Code);

        // Kaldırma da reddediliyor: arşiv ön koşulu EYLEMİN kendisine ait, yönüne değil.
        Assert.True(kaldir.IsFailure);
        Assert.Equal(Listing.CannotFeatureArchived, kaldir.Error.Code);

        // Kod `listing.invalid_status_transition` DEĞİL: öne çıkarma bir geçiş değil ve
        // panel bir sıralama eylemi için "geçersiz durum geçişi" derse personel olmayan
        // bir geçişi arar (**D49**).
        Assert.NotEqual(Listing.InvalidTransition, oneCikar.Error.Code);
    }

    /// <summary>
    /// Durum adı DİZE olarak geliyor: `ListingStatus` modülün `internal`'ı ve public bir
    /// test metodunun parametresi olamaz (CS0051). Ad `Enum.Parse` ile çözülüyor —
    /// yanlış yazılmış bir ad testi yeşil bırakmaz, patlatır.
    /// </summary>
    [Theory(DisplayName = "0012c/AC-29: Öne çıkarma **arşiv dışındaki her durumda** çalışır: taslak, yayında, rezerve ve satıldı")]
    [InlineData("Draft")]
    [InlineData("Published")]
    [InlineData("Reserved")]
    [InlineData("Sold")]
    public void Calisir_ArsivDisindakiHerDurumda(string durumAdi)
    {
        ListingStatus durum = Enum.Parse<ListingStatus>(durumAdi);
        Listing listing = DurumdakiIlan(durum);

        Result sonuc = listing.Feature();

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);
        Assert.True(listing.IsFeatured);
        Assert.Equal(durum, listing.Status);
    }

    /// <summary>
    /// A3'ün yolundan geçerek istenen duruma getirir. Durumu doğrudan yazmak mümkün
    /// değil ve olmamalı: geçişler agregatın kendi kapısından geçer.
    /// </summary>
    private static Listing DurumdakiIlan(ListingStatus durum)
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), Listing.NormalWeight);

        if (durum is ListingStatus.Draft)
        {
            return listing;
        }

        Assert.True(listing.Publish(Adres(listing), new FixedClock(DateTimeOffset.UtcNow)).IsSuccess);

        if (durum is ListingStatus.Reserved)
        {
            Assert.True(listing.Reserve().IsSuccess);
        }

        if (durum is ListingStatus.Sold)
        {
            Assert.True(listing.MarkSold().IsSuccess);
        }

        return listing;
    }

    private static ListingSlug Adres(Listing listing) =>
        ListingSlug.Create("Toyota Corolla", 2021, listing.Id).Value;
}
