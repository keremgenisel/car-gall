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

    /// <summary>YALNIZCA EF içindir (0010a D10); davranış değişmiyor.</summary>
    private Listing()
        : base(default)
    {
    }

    private Listing(ListingId id, VehicleId vehicleId, int showcaseSortWeight)
        : base(id)
    {
        VehicleId = vehicleId;
        ShowcaseSortWeight = showcaseSortWeight;
        Status = ListingStatus.Draft;
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

    internal static Listing CreateDraft(VehicleId vehicleId, int showcaseSortWeight) =>
        new(ListingId.New(), vehicleId, showcaseSortWeight);

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
