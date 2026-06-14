using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain.Events;

public sealed record InvoiceCreatedDomainEvent(Guid InvoiceId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record InvoiceStatusChangedDomainEvent(Guid InvoiceId, string Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
