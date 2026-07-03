using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Documents.Domain.Events;

public sealed record DocumentCreatedDomainEvent(Guid DocumentId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record DocumentStatusChangedDomainEvent(Guid DocumentId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record DocumentVersionAddedDomainEvent(Guid DocumentId, int Number) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
