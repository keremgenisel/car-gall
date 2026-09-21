namespace CarGallery.BuildingBlocks;

/// <summary>Modülün içinde kalan, aynı transaction'da işlenen olay (docs/architecture.md § 2.B).</summary>
public interface IDomainEvent;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => domainEvents;

    public void ClearDomainEvents() => domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);
}
