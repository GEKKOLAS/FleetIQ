using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        Amount = amount;
        Currency = currency;
    }

    private Money() { Currency = "USD"; } // EF

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount; yield return Currency;
    }
}
