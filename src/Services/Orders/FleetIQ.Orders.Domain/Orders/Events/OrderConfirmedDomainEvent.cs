using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders.Events;

public sealed record OrderConfirmedDomainEvent(Guid OrderId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
