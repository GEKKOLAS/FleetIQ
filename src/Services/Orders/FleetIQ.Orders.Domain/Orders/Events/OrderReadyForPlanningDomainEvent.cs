using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders.Events;

public sealed record OrderReadyForPlanningDomainEvent(Guid OrderId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
