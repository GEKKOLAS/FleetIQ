using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders.Events;

public sealed record OrderCancelledDomainEvent(Guid OrderId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
