namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Bir entegrasyon olayının alıcısı. Yayıncı abone listesini BİLMEZ (C7, Yasak Listesi 18):
/// dağıtıcı alıcıları DI'dan çözer, yayıncı hiçbirini adıyla anmaz.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Dağıtıcının tip bilgisi olmadan çağırabilmesi için ince sarmalayıcı.
/// </summary>
public interface IIntegrationEventDispatchTarget
{
    string HandlerName { get; }

    Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
