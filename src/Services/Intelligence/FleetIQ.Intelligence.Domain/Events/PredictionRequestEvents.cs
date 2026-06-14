using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Intelligence.Domain.Events;

public sealed record PredictionRequestCreatedDomainEvent(Guid PredictionRequestId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record PredictionRequestStatusChangedDomainEvent(Guid PredictionRequestId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
