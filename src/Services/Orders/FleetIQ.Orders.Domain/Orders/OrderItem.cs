using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders;

/// <summary>Child of the Order aggregate. Modeled as a REGULAR entity (own table + FK), so EF
/// inserts/removes it cleanly when the order is updated (unlike an owned collection).</summary>
public sealed class OrderItem : Entity<Guid>
{
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal WeightKg { get; private set; }

    internal OrderItem(Guid id, string description, int quantity, decimal weightKg) : base(id)
    {
        Description = description; Quantity = quantity; WeightKg = weightKg;
    }

    private OrderItem() { Description = string.Empty; } // EF: materialized row, IsTransient = false
}
