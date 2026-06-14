using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Analytics.Domain.Events;

public sealed record ReportDefinitionCreatedDomainEvent(Guid ReportDefinitionId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record ReportDefinitionStatusChangedDomainEvent(Guid ReportDefinitionId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
