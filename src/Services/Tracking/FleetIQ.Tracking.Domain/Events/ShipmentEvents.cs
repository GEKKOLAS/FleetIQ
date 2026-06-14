using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Tracking.Domain.Events;

public sealed record ShipmentCreatedDomainEvent(Guid ShipmentId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record ShipmentStatusChangedDomainEvent(Guid ShipmentId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
