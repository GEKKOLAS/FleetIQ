using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Carriers.Domain.Events;

public sealed record CarrierCreatedDomainEvent(Guid CarrierId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record CarrierStatusChangedDomainEvent(Guid CarrierId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
