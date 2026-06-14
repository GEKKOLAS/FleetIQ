using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Integration.Domain.Events;

public sealed record IntegrationConnectorCreatedDomainEvent(Guid IntegrationConnectorId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record IntegrationConnectorStatusChangedDomainEvent(Guid IntegrationConnectorId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
