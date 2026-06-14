using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Notifications.Domain.Events;

public sealed record NotificationCreatedDomainEvent(Guid NotificationId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record NotificationStatusChangedDomainEvent(Guid NotificationId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
