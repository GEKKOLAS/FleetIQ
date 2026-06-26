using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Tracking.Domain.Events;

public sealed record ShipmentCreatedDomainEvent(Guid ShipmentId, Guid LoadId, Guid CarrierId) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record ShipmentDispatchedDomainEvent(Guid ShipmentId) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record ShipmentLocationRecordedDomainEvent(Guid ShipmentId, double Latitude, double Longitude, double? SpeedKph) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record ShipmentExceptionRaisedDomainEvent(Guid ShipmentId, string Note) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record ShipmentDeliveredDomainEvent(Guid ShipmentId, string ReceivedBy) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
