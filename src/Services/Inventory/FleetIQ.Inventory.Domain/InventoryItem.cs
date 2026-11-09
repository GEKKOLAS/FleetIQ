using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Inventory.Domain.Events;

namespace FleetIQ.Inventory.Domain;

public enum InventoryItemStatus { Active = 0, Discontinued = 1 }

public static class InventoryErrors
{
    public static readonly Error SkuRequired =
        new("Inventory.SkuRequired", "A SKU is required.");
    public static readonly Error NameRequired =
        new("Inventory.NameRequired", "A product name is required.");
    public static readonly Error WarehouseRequired =
        new("Inventory.WarehouseRequired", "A warehouse code is required.");
    public static readonly Error UnitRequired =
        new("Inventory.UnitRequired", "A unit of measure is required.");
    public static readonly Error NegativeReorderPoint =
        new("Inventory.NegativeReorderPoint", "The reorder point cannot be negative.");
    public static readonly Error QuantityMustBePositive =
        new("Inventory.QuantityMustBePositive", "The quantity must be greater than zero.");
    public static readonly Error ReferenceRequired =
        new("Inventory.ReferenceRequired", "A reference (e.g. purchase order or order number) is required.");
    public static readonly Error ReasonRequired =
        new("Inventory.ReasonRequired", "A reason is required for a stock adjustment.");
    public static readonly Error InsufficientAvailable =
        new("Inventory.InsufficientAvailable", "Not enough available (unreserved) stock for this reservation.");
    public static readonly Error InsufficientReserved =
        new("Inventory.InsufficientReserved", "The quantity exceeds the stock currently reserved.");
    public static readonly Error CountBelowReserved =
        new("Inventory.CountBelowReserved", "The counted quantity cannot be lower than the stock already reserved; release reservations first.");
    public static readonly Error CountUnchanged =
        new("Inventory.CountUnchanged", "The counted quantity equals the quantity on hand; nothing to adjust.");
    public static readonly Error NegativeCount =
        new("Inventory.NegativeCount", "The counted quantity cannot be negative.");
    public static readonly Error IsDiscontinued =
        new("Inventory.Discontinued", "The item is discontinued and cannot receive or reserve stock.");
    public static readonly Error HasReservations =
        new("Inventory.HasReservations", "The item still has reserved stock; ship or release it before discontinuing.");
    public static readonly Error DuplicateSku =
        new("Inventory.DuplicateSku", "This SKU already exists in the warehouse.");
}

/// <summary>
/// Stock of one SKU in one warehouse. Invariants: 0 &lt;= Reserved &lt;= OnHand; Available = OnHand - Reserved.
/// Flow: Receive (+OnHand) -> Reserve for an order (+Reserved) -> Ship (-Reserved, -OnHand) or Release (-Reserved).
/// Cycle counts go through Adjust (with a reason). Every change is recorded as an immutable StockMovement.
/// </summary>
public sealed class InventoryItem : AggregateRoot<Guid>
{
    private readonly List<StockMovement> _movements = new();

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string WarehouseCode { get; private set; } = string.Empty;
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public decimal ReorderPoint { get; private set; }
    public InventoryItemStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastMovementOnUtc { get; private set; }
    public IReadOnlyList<StockMovement> Movements => _movements.AsReadOnly();

    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    public bool IsBelowReorderPoint => Status == InventoryItemStatus.Active && QuantityAvailable <= ReorderPoint;

    private InventoryItem() { } // EF

    private InventoryItem(Guid id, string sku, string name, string warehouseCode, string unitOfMeasure, decimal reorderPoint)
        : base(id)
    {
        Sku = sku;
        Name = name;
        WarehouseCode = warehouseCode;
        UnitOfMeasure = unitOfMeasure;
        ReorderPoint = reorderPoint;
        Status = InventoryItemStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<InventoryItem> Create(string sku, string name, string warehouseCode, string unitOfMeasure, decimal reorderPoint)
    {
        if (string.IsNullOrWhiteSpace(sku)) return Result.Failure<InventoryItem>(InventoryErrors.SkuRequired);
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<InventoryItem>(InventoryErrors.NameRequired);
        if (string.IsNullOrWhiteSpace(warehouseCode)) return Result.Failure<InventoryItem>(InventoryErrors.WarehouseRequired);
        if (string.IsNullOrWhiteSpace(unitOfMeasure)) return Result.Failure<InventoryItem>(InventoryErrors.UnitRequired);
        if (reorderPoint < 0) return Result.Failure<InventoryItem>(InventoryErrors.NegativeReorderPoint);

        var item = new InventoryItem(Guid.NewGuid(), NormalizeCode(sku), name.Trim(), NormalizeCode(warehouseCode),
            unitOfMeasure.Trim(), reorderPoint);
        item.Raise(new InventoryItemCreatedDomainEvent(item.Id, item.Sku, item.WarehouseCode));
        return Result.Success(item);
    }

    /// <summary>SKUs and warehouse codes are case-insensitive identifiers; store them upper-cased.</summary>
    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    /// <summary>Goods arrive (e.g. against a purchase order): increases stock on hand.</summary>
    public Result Receive(decimal quantity, string reference)
    {
        if (Status == InventoryItemStatus.Discontinued) return Result.Failure(InventoryErrors.IsDiscontinued);
        var check = ValidateQuantityAndReference(quantity, reference);
        if (check.IsFailure) return check;

        QuantityOnHand += quantity;
        Record(StockMovementType.Receipt, quantity, reference);
        Raise(new StockReceivedDomainEvent(Id, Sku, quantity, QuantityOnHand));
        return Result.Success();
    }

    /// <summary>Allocates available stock to an order so it cannot be promised twice.</summary>
    public Result Reserve(decimal quantity, string reference)
    {
        if (Status == InventoryItemStatus.Discontinued) return Result.Failure(InventoryErrors.IsDiscontinued);
        var check = ValidateQuantityAndReference(quantity, reference);
        if (check.IsFailure) return check;
        if (quantity > QuantityAvailable) return Result.Failure(InventoryErrors.InsufficientAvailable);

        var wasLow = IsBelowReorderPoint;
        QuantityReserved += quantity;
        Record(StockMovementType.Reservation, quantity, reference);
        Raise(new StockReservedDomainEvent(Id, Sku, quantity, reference.Trim()));
        RaiseLowStockIfCrossed(wasLow);
        return Result.Success();
    }

    /// <summary>Gives reserved stock back to the available pool (e.g. order cancelled).</summary>
    public Result ReleaseReservation(decimal quantity, string reference)
    {
        var check = ValidateQuantityAndReference(quantity, reference);
        if (check.IsFailure) return check;
        if (quantity > QuantityReserved) return Result.Failure(InventoryErrors.InsufficientReserved);

        QuantityReserved -= quantity;
        Record(StockMovementType.ReservationRelease, quantity, reference);
        Raise(new StockReservationReleasedDomainEvent(Id, Sku, quantity, reference.Trim()));
        return Result.Success();
    }

    /// <summary>Reserved goods leave the warehouse (picked and loaded): consumes the reservation and the stock.</summary>
    public Result Ship(decimal quantity, string reference)
    {
        var check = ValidateQuantityAndReference(quantity, reference);
        if (check.IsFailure) return check;
        if (quantity > QuantityReserved) return Result.Failure(InventoryErrors.InsufficientReserved);

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
        Record(StockMovementType.Shipment, quantity, reference);
        Raise(new StockShippedDomainEvent(Id, Sku, quantity, reference.Trim(), QuantityOnHand));
        return Result.Success();
    }

    /// <summary>Cycle count: sets stock on hand to the physically counted quantity. The movement stores the signed delta.</summary>
    public Result Adjust(decimal countedQuantity, string reason)
    {
        if (countedQuantity < 0) return Result.Failure(InventoryErrors.NegativeCount);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(InventoryErrors.ReasonRequired);
        if (countedQuantity < QuantityReserved) return Result.Failure(InventoryErrors.CountBelowReserved);
        if (countedQuantity == QuantityOnHand) return Result.Failure(InventoryErrors.CountUnchanged);

        var wasLow = IsBelowReorderPoint;
        var delta = countedQuantity - QuantityOnHand;
        QuantityOnHand = countedQuantity;
        Record(StockMovementType.Adjustment, delta, reason);
        Raise(new StockAdjustedDomainEvent(Id, Sku, delta, reason.Trim(), QuantityOnHand));
        RaiseLowStockIfCrossed(wasLow);
        return Result.Success();
    }

    public Result ChangeReorderPoint(decimal reorderPoint)
    {
        if (reorderPoint < 0) return Result.Failure(InventoryErrors.NegativeReorderPoint);

        var wasLow = IsBelowReorderPoint;
        ReorderPoint = reorderPoint;
        RaiseLowStockIfCrossed(wasLow);
        return Result.Success();
    }

    /// <summary>Stops the item from being received or reserved. Remaining unreserved stock can still be adjusted out.</summary>
    public Result Discontinue()
    {
        if (Status == InventoryItemStatus.Discontinued) return Result.Failure(InventoryErrors.IsDiscontinued);
        if (QuantityReserved > 0) return Result.Failure(InventoryErrors.HasReservations);

        Status = InventoryItemStatus.Discontinued;
        Raise(new InventoryItemDiscontinuedDomainEvent(Id, Sku));
        return Result.Success();
    }

    private static Result ValidateQuantityAndReference(decimal quantity, string reference)
    {
        if (quantity <= 0) return Result.Failure(InventoryErrors.QuantityMustBePositive);
        if (string.IsNullOrWhiteSpace(reference)) return Result.Failure(InventoryErrors.ReferenceRequired);
        return Result.Success();
    }

    private void Record(StockMovementType type, decimal quantity, string reference)
    {
        var movement = new StockMovement(Guid.NewGuid(), type, quantity, reference.Trim(), QuantityOnHand, QuantityReserved);
        _movements.Add(movement);
        LastMovementOnUtc = movement.OccurredOnUtc;
    }

    /// <summary>Raises LowStock only on the transition into the low zone, not on every movement while already low.</summary>
    private void RaiseLowStockIfCrossed(bool wasLow)
    {
        if (!wasLow && IsBelowReorderPoint)
            Raise(new LowStockDetectedDomainEvent(Id, Sku, WarehouseCode, QuantityAvailable, ReorderPoint));
    }
}
