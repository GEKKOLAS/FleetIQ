using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain.Events;

public sealed record LoadCreatedDomainEvent(Guid LoadId, string Reference) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record LoadOrderAddedDomainEvent(Guid LoadId, Guid OrderId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record LoadCarrierAssignedDomainEvent(Guid LoadId, Guid CarrierId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record LoadPlannedDomainEvent(Guid LoadId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record LoadConfirmedDomainEvent(Guid LoadId, Guid CarrierId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record LoadCancelledDomainEvent(Guid LoadId) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
