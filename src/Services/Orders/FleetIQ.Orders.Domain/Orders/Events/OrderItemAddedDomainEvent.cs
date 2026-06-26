using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders.Events;

public sealed record OrderItemAddedDomainEvent(Guid OrderId, string Description, int Quantity, decimal WeightKg) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
