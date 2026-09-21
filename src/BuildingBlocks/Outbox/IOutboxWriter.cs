namespace CarGallery.BuildingBlocks.Outbox;

public interface IOutboxWriter
{
    void Write(IIntegrationEvent integrationEvent);
}
