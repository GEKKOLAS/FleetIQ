using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Carriers.Domain.Events;

public sealed record CarrierCreatedDomainEvent(Guid CarrierId, string LegalName) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record CarrierVerifiedDomainEvent(Guid CarrierId, string LegalName) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record CarrierSuspendedDomainEvent(Guid CarrierId, string Reason) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record VehicleAddedDomainEvent(Guid CarrierId, string Plate) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
public sealed record DriverAddedDomainEvent(Guid CarrierId, string FullName) : IDomainEvent { public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
