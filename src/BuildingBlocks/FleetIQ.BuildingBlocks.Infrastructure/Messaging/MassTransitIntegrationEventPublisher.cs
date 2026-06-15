using FleetIQ.BuildingBlocks.Application;
using MassTransit;

namespace FleetIQ.BuildingBlocks.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync(object integrationEvent, CancellationToken ct = default)
        => publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), ct);
}
