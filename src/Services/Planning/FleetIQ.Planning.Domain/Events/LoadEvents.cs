using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain.Events;

public sealed record LoadCreatedDomainEvent(Guid LoadId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record LoadStatusChangedDomainEvent(Guid LoadId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
