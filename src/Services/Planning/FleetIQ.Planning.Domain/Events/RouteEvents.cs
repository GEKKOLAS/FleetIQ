using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain.Events;

public sealed record RouteCreatedDomainEvent(Guid RouteId, Guid LoadId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record RouteStopAddedDomainEvent(Guid RouteId, int Sequence) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record RouteFinalizedDomainEvent(Guid RouteId, Guid LoadId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
