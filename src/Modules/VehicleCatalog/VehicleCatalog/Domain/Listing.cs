using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain.Events;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Bir aracın vitrindeki yayın hâli. Araç ≠ ilan: araç fiziksel, ilan yayınsal gerçektir
/// (docs/domain.md § 3). Geçişler A3'te sayılanlarla sınırlıdır; başka geçiş yoktur.
/// </summary>
internal sealed class Listing : AggregateRoot<ListingId>
{
    internal const string InvalidTransition = "listing.invalid_status_transition";

    /// <summary>0010b **R11** — `jsonb` sınırsız metin kabul eder; tek kayıt veritabanını şişirebilir (C22).</summary>
    internal const int MaximumTitleLength = 120;

    internal const int MaximumDescriptionLength = 4000;

    /// <summary>
    /// Başlık ve açıklama AYRI kodlar döner. Tek koda eşlenince komut katmanı hangi
    /// alanı işaretleyeceğini bilemiyordu ve açıklamayı uzun yazan personele BAŞLIK
    /// alanı kırmızı gösteriliyordu — `0010a`'daki "boş şase → çok uzun" hatasının aynı
    /// sınıfı. Yanlış alan işaretlemesi personeli yanlış düzeltmeye yöneltir.
    /// </summary>
    internal const string TitleTooLong = "listing.title_too_long";

    internal const string DescriptionTooLong = "listing.description_too_long";

    /// <summary>YALNIZCA EF içindir (0010a D10); davranış değişmiyor.</summary>
    private Listing()
        : base(default)
    {
        Title = LocalizedText.Empty;
        Description = LocalizedText.Empty;
    }

    private Listing(ListingId id, VehicleId vehicleId, int showcaseSortWeight)
        : base(id)
    {
        VehicleId = vehicleId;
        ShowcaseSortWeight = showcaseSortWeight;
        Status = ListingStatus.Draft;
        Title = LocalizedText.Empty;
        Description = LocalizedText.Empty;
    }

    internal VehicleId VehicleId { get; }

    internal ListingStatus Status { get; private set; }

    /// <summary>R7'nin birinci sıralama ölçütü. Ziyaretçi değiştiremez.</summary>
    internal int ShowcaseSortWeight { get; private set; }

    /// <summary>
    /// İLK Taslak→Yayında geçişinde yazılır ve sonraki Rezerve↔Yayında gidiş gelişlerinde
    /// DEĞİŞMEZ. Aksi hâlde rezervasyonu düşen araç listenin başına zıplar ve "yeni gelen
    /// araç" yanlış sinyalini üretir; AC-5'in kararlılık testi de oturumdan oturuma değişir.
    /// </summary>
    internal DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>
    /// 0010b **R10** — ilan başlığı, etkin her dilde ayrı. Bir dilde yazmamak GEÇERLİ bir
    /// durumdur (AC-19); boş `LocalizedText` o yüzden hata değil, başlangıç hâlidir.
    /// </summary>
    internal LocalizedText Title { get; private set; }

    internal LocalizedText Description { get; private set; }

    internal static Listing CreateDraft(VehicleId vehicleId, int showcaseSortWeight) =>
        new(ListingId.New(), vehicleId, showcaseSortWeight);

    /// <summary>
    /// İlan metnini TAM DEĞİŞTİRME ile yazar (0010b **D23**): form etkin dillerin hepsini
    /// birlikte gönderir ve bir dili boşaltmak ancak böyle ifade edilebilir.
    ///
    /// <para>Uzunluk sınırı LOCALE BAŞINA denetlenir (R11, AC-20): toplamı ölçmek, üç
    /// dilde yazan galeriyi tek dilde yazandan daha erken keserdi.</para>
    /// </summary>
    internal Result SetText(LocalizedText title, LocalizedText description)
    {
        if (title.ToDictionary().Values.Any(value => value.Length > MaximumTitleLength))
        {
            return Result.Failure(new Error(TitleTooLong));
        }

        if (description.ToDictionary().Values.Any(value => value.Length > MaximumDescriptionLength))
        {
            return Result.Failure(new Error(DescriptionTooLong));
        }

        Title = title;
        Description = description;
        return Result.Success();
    }

    /// <summary>
    /// 0010b **R12/AC-21** — bir dil, başlık VE açıklamasının ikisi de doluysa tamdır;
    /// biri eksikse o dil eksiktir. Saf metot: veritabanı görmeden sınanır.
    ///
    /// <para>Boş dize dolu SAYILMAZ. Aksi hâlde formda boşluk bırakıp kaydeden personel
    /// o dili "tam" görürdü ve gösterge tam da gizlemesi gereken şeyi gizlerdi.</para>
    /// </summary>
    internal IReadOnlyList<Locale> MissingLocales(IReadOnlyList<Locale> enabled) =>
        MissingLocalesOf(Title, Description, enabled);

    /// <summary>
    /// Aynı kural, agregat ÖRNEĞİ olmadan. Özet sorgusu `listings`'ten yalnız iki kolon
    /// çekiyor ve elinde bir `Listing` yok; kuralı orada tekrar yazmak, aynı invariantı
    /// iki yerde iki farklı şey yapma riski demekti.
    /// </summary>
    internal static IReadOnlyList<Locale> MissingLocalesOf(
        LocalizedText title,
        LocalizedText description,
        IReadOnlyList<Locale> enabled)
    {
        Dictionary<string, string> titles = title.ToDictionary();
        Dictionary<string, string> descriptions = description.ToDictionary();

        return
        [
            .. enabled.Where(locale =>
                !HasText(titles, locale) || !HasText(descriptions, locale))
        ];
    }

    private static bool HasText(Dictionary<string, string> values, Locale locale) =>
        values.TryGetValue(locale.Code, out string? text) && !string.IsNullOrWhiteSpace(text);

    internal Result Publish(IClock clock)
    {
        if (Status is not ListingStatus.Draft)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Published;
        PublishedAt ??= clock.UtcNow;
        Raise(new ListingPublished(Id, PublishedAt.Value));
        return Result.Success();
    }

    internal Result Reserve()
    {
        if (Status is not ListingStatus.Published)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Reserved;
        Raise(new ListingReserved(Id));
        return Result.Success();
    }

    internal Result ReturnToShowcase()
    {
        if (Status is not ListingStatus.Reserved)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Published;
        Raise(new ListingReturnedToShowcase(Id));
        return Result.Success();
    }

    internal Result MarkSold()
    {
        if (Status is not (ListingStatus.Published or ListingStatus.Reserved))
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Sold;
        Raise(new ListingSold(Id));
        return Result.Success();
    }

    /// <summary>Yayından kaldırma. A8'de şube kapandığında da bu yol kullanılır.</summary>
    internal Result Unpublish()
    {
        if (Status is not ListingStatus.Published)
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Draft;
        Raise(new ListingUnpublished(Id));
        return Result.Success();
    }

    internal Result Archive()
    {
        if (Status is not (ListingStatus.Published or ListingStatus.Sold))
        {
            return Result.Failure(new Error(InvalidTransition));
        }

        Status = ListingStatus.Archived;
        Raise(new ListingArchived(Id));
        return Result.Success();
    }

    internal void ChangeShowcaseSortWeight(int weight) => ShowcaseSortWeight = weight;
}
