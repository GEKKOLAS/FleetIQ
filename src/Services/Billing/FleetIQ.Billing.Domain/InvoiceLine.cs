using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain;

/// <summary>A line inside an Invoice. Part of the Invoice aggregate - created only via the aggregate root.</summary>
public sealed class InvoiceLine : Entity<Guid>
{
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal InvoiceLine(Guid id, string description, int quantity, decimal unitPrice) : base(id)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    private InvoiceLine() { Description = string.Empty; } // EF: materialized row, IsTransient = false
}
