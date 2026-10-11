using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain.Events;

internal sealed record ListingPublished(ListingId ListingId, DateTimeOffset PublishedAt) : IDomainEvent;

internal sealed record ListingReserved(ListingId ListingId) : IDomainEvent;

internal sealed record ListingReturnedToShowcase(ListingId ListingId) : IDomainEvent;

internal sealed record ListingSold(ListingId ListingId) : IDomainEvent;

internal sealed record ListingUnpublished(ListingId ListingId) : IDomainEvent;

internal sealed record ListingArchived(ListingId ListingId) : IDomainEvent;

/// <summary>
/// `0012b` **D11** — aracın fiyatı değişti. `Vehicle` bu spec'e kadar hiç domain olayı
/// yaymıyordu; `VehicleConfiguration` `DomainEvents`'i zaten `Ignore` ediyor (`:16`), yani
/// EF tarafında bedel yok.
/// </summary>
internal sealed record VehiclePriceWasChanged(VehicleId VehicleId) : IDomainEvent;

/// <summary>`0012b` **D11** / **A7** — araç başka bir şubeye taşındı.</summary>
internal sealed record VehicleWasRelocated(VehicleId VehicleId) : IDomainEvent;
