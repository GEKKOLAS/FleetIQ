using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Billing.Domain;

/// <summary>Money value object: an amount in a 3-letter ISO currency. Immutable, compared by value.</summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    private Money() { Currency = "USD"; } // EF

    public static Money Zero(string currency) => new(0m, currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
