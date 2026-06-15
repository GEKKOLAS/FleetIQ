using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Billing.Domain.Events;

namespace FleetIQ.Billing.Domain;

/// <summary>Invoice aggregate root. All state changes go through intent-based methods that
/// enforce the invoice lifecycle (Draft -> Issued -> Paid, with Cancel).</summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<InvoiceLine> _lines = new();

    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public InvoiceStatus Status { get; private set; }
    public Money Total { get; private set; } = Money.Zero("USD");
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? IssuedOnUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public DateTime? PaidOnUtc { get; private set; }
    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    private Invoice() { } // EF

    private Invoice(Guid id, Guid customerId, string currency) : base(id)
    {
        CustomerId = customerId;
        Currency = currency.ToUpperInvariant();
        Status = InvoiceStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
        Total = Money.Zero(Currency);
    }

    public static Result<Invoice> Create(Guid customerId, string currency,
        IEnumerable<(string Description, int Quantity, decimal UnitPrice)> lines)
    {
        if (customerId == Guid.Empty) return Result.Failure<Invoice>(InvoiceErrors.CustomerRequired);
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Invoice>(InvoiceErrors.InvalidCurrency);

        var invoice = new Invoice(Guid.NewGuid(), customerId, currency);
        foreach (var line in lines)
        {
            var added = invoice.AddLine(line.Description, line.Quantity, line.UnitPrice);
            if (added.IsFailure) return Result.Failure<Invoice>(added.Error);
        }

        invoice.Raise(new InvoiceCreatedDomainEvent(invoice.Id, customerId));
        return Result.Success(invoice);
    }

    public Result AddLine(string description, int quantity, decimal unitPrice)
    {
        if (Status != InvoiceStatus.Draft) return Result.Failure(InvoiceErrors.NotDraft);
        if (string.IsNullOrWhiteSpace(description)) return Result.Failure(InvoiceErrors.LineDescriptionRequired);
        if (quantity <= 0) return Result.Failure(InvoiceErrors.InvalidQuantity);
        if (unitPrice < 0) return Result.Failure(InvoiceErrors.InvalidUnitPrice);

        _lines.Add(new InvoiceLine(Guid.NewGuid(), description, quantity, unitPrice));
        Recalculate();
        Raise(new InvoiceLineAddedDomainEvent(Id, description, quantity, unitPrice));
        return Result.Success();
    }

    public Result Issue(DateTime dueDateUtc)
    {
        if (Status != InvoiceStatus.Draft) return Result.Failure(InvoiceErrors.NotDraft);
        if (_lines.Count == 0 || Total.Amount <= 0) return Result.Failure(InvoiceErrors.CannotIssueEmpty);

        Status = InvoiceStatus.Issued;
        IssuedOnUtc = DateTime.UtcNow;
        DueDateUtc = dueDateUtc;
        Raise(new InvoiceIssuedDomainEvent(Id, Total.Amount, Currency));
        return Result.Success();
    }

    public Result RecordPayment()
    {
        if (Status != InvoiceStatus.Issued) return Result.Failure(InvoiceErrors.NotIssued);

        Status = InvoiceStatus.Paid;
        PaidOnUtc = DateTime.UtcNow;
        Raise(new InvoicePaidDomainEvent(Id, Total.Amount, Currency));
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == InvoiceStatus.Paid) return Result.Failure(InvoiceErrors.CannotCancelPaid);
        if (Status == InvoiceStatus.Cancelled) return Result.Failure(InvoiceErrors.AlreadyCancelled);

        Status = InvoiceStatus.Cancelled;
        Raise(new InvoiceCancelledDomainEvent(Id));
        return Result.Success();
    }

    private void Recalculate()
    {
        var sum = _lines.Sum(l => l.UnitPrice * l.Quantity);
        Total = new Money(sum, Currency);
    }
}
