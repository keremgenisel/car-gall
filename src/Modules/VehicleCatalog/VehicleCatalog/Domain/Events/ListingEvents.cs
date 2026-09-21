using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain.Events;

internal sealed record ListingPublished(ListingId ListingId, DateTimeOffset PublishedAt) : IDomainEvent;

internal sealed record ListingReserved(ListingId ListingId) : IDomainEvent;

internal sealed record ListingReturnedToShowcase(ListingId ListingId) : IDomainEvent;

internal sealed record ListingSold(ListingId ListingId) : IDomainEvent;

internal sealed record ListingUnpublished(ListingId ListingId) : IDomainEvent;

internal sealed record ListingArchived(ListingId ListingId) : IDomainEvent;
