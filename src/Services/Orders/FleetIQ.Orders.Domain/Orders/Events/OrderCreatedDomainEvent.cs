using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders.Events;

public sealed record OrderCreatedDomainEvent(Guid OrderId, string OrderNumber, Guid CustomerId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
