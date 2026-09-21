using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;

namespace CarGallery.Modules.Dealership.Tests.Integration;

internal sealed class TestSaati(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    internal void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
}

internal sealed class SayanAlici : IIntegrationEventHandler<BranchOpened>, IIntegrationEventDispatchTarget
{
    internal int CagrilmaSayisi { get; private set; }

    public string HandlerName => nameof(SayanAlici);

    public Task HandleAsync(BranchOpened integrationEvent, CancellationToken cancellationToken)
    {
        CagrilmaSayisi++;
        return Task.CompletedTask;
    }

    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((BranchOpened)integrationEvent, cancellationToken);
}

/// <summary>Sıra bağımsızlığını sınar: son yazılan ad kazanır, gelme sırası değil.</summary>
internal sealed class SonAdiTutanAlici
    : IIntegrationEventHandler<BranchOpened>,
      IIntegrationEventHandler<BranchDetailsChanged>,
      IIntegrationEventDispatchTarget
{
    private readonly SortedDictionary<DateTimeOffset, string> adlar = [];

    internal string? SonAd => adlar.Count == 0 ? null : adlar[adlar.Keys.Last()];

    public string HandlerName => nameof(SonAdiTutanAlici);

    public Task HandleAsync(BranchOpened integrationEvent, CancellationToken cancellationToken)
    {
        Kaydet(integrationEvent.OccurredAt, integrationEvent.Names);
        return Task.CompletedTask;
    }

    public Task HandleAsync(BranchDetailsChanged integrationEvent, CancellationToken cancellationToken)
    {
        Kaydet(integrationEvent.OccurredAt, integrationEvent.Names);
        return Task.CompletedTask;
    }

    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        integrationEvent switch
        {
            BranchOpened opened => HandleAsync(opened, cancellationToken),
            BranchDetailsChanged changed => HandleAsync(changed, cancellationToken),
            _ => Task.CompletedTask,
        };

    private void Kaydet(DateTimeOffset occurredAt, IReadOnlyList<BranchNameDto> names)
    {
        string? tr = names.FirstOrDefault(name => name.Locale == "tr")?.Value;

        if (tr is not null)
        {
            adlar[occurredAt] = tr;
        }
    }
}

internal sealed class HepHataVerenAlici : IIntegrationEventHandler<BranchOpened>, IIntegrationEventDispatchTarget
{
    public string HandlerName => nameof(HepHataVerenAlici);

    public Task HandleAsync(BranchOpened integrationEvent, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("alıcı bilinçli olarak hata veriyor");

    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((BranchOpened)integrationEvent, cancellationToken);
}
