using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain.Events;

public sealed record InvoiceCreatedDomainEvent(Guid InvoiceId, Guid CustomerId) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record InvoiceLineAddedDomainEvent(Guid InvoiceId, string Description, int Quantity, decimal UnitPrice) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record InvoiceIssuedDomainEvent(Guid InvoiceId, decimal Total, string Currency) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record InvoicePaidDomainEvent(Guid InvoiceId, decimal Total, string Currency) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }

public sealed record InvoiceCancelledDomainEvent(Guid InvoiceId) : IDomainEvent
{ public DateTime OccurredOnUtc { get; } = DateTime.UtcNow; }
