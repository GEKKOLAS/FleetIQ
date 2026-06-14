using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain;

// NOTE: Core of the hybrid model: add RateAgreement, Settlement, Dispute aggregates and a Money value object; integrate Stripe.net for payments.
public enum InvoiceStatus { Draft = 0, Issued = 1, Paid = 2 }

public static class InvoiceErrors
{
    public static readonly Error InvalidTransition =
        new("Invoice.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Invoice aggregate root.</summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; } = 0m;
    public string Currency { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Invoice() { } // EF

    private Invoice(Guid id, Guid customerId, decimal amount, string currency) : base(id)
    {
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Status = InvoiceStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Invoice Create(Guid customerId, decimal amount, string currency)
    {
        var entity = new Invoice(Guid.NewGuid(), customerId, amount, currency);
        entity.Raise(new Events.InvoiceCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Issue()
    {
        if (Status != InvoiceStatus.Draft) return Result.Failure(InvoiceErrors.InvalidTransition);
        Status = InvoiceStatus.Issued;
        Raise(new Events.InvoiceStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
