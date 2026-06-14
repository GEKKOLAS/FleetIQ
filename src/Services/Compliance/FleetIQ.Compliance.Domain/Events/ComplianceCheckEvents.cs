using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Compliance.Domain.Events;

public sealed record ComplianceCheckCreatedDomainEvent(Guid ComplianceCheckId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record ComplianceCheckStatusChangedDomainEvent(Guid ComplianceCheckId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
