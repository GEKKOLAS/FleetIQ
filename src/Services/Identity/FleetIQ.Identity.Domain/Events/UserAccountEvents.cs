using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Identity.Domain.Events;

public sealed record UserAccountCreatedDomainEvent(Guid UserAccountId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record UserAccountStatusChangedDomainEvent(Guid UserAccountId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
