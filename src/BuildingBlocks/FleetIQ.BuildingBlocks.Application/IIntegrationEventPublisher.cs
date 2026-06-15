namespace FleetIQ.BuildingBlocks.Application;

/// <summary>Publishes integration events to the bus. Keeps the Application layer free of MassTransit.</summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(object integrationEvent, CancellationToken ct = default);
}
