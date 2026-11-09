using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Inventory.Domain;

public enum StockMovementType { Receipt = 0, Reservation = 1, ReservationRelease = 2, Shipment = 3, Adjustment = 4 }

/// <summary>
/// Immutable ledger line inside an InventoryItem. Quantity is positive for every type except
/// Adjustment, where it is the signed delta (counted - previous on hand). Balances are snapshots after the movement.
/// </summary>
public sealed class StockMovement : Entity<Guid>
{
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal OnHandAfter { get; private set; }
    public decimal ReservedAfter { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    private StockMovement() { } // EF

    internal StockMovement(Guid id, StockMovementType type, decimal quantity, string reference, decimal onHandAfter, decimal reservedAfter)
        : base(id)
    {
        Type = type;
        Quantity = quantity;
        Reference = reference;
        OnHandAfter = onHandAfter;
        ReservedAfter = reservedAfter;
        OccurredOnUtc = DateTime.UtcNow;
    }
}
