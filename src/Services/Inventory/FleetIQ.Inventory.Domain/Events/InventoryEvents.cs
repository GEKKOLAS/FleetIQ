using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Inventory.Domain.Events;

public sealed record InventoryItemCreatedDomainEvent(Guid InventoryItemId, string Sku, string WarehouseCode) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record StockReceivedDomainEvent(Guid InventoryItemId, string Sku, decimal Quantity, decimal OnHand) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record StockReservedDomainEvent(Guid InventoryItemId, string Sku, decimal Quantity, string Reference) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record StockReservationReleasedDomainEvent(Guid InventoryItemId, string Sku, decimal Quantity, string Reference) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record StockShippedDomainEvent(Guid InventoryItemId, string Sku, decimal Quantity, string Reference, decimal OnHand) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record StockAdjustedDomainEvent(Guid InventoryItemId, string Sku, decimal Delta, string Reason, decimal OnHand) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record LowStockDetectedDomainEvent(Guid InventoryItemId, string Sku, string WarehouseCode, decimal Available, decimal ReorderPoint) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record InventoryItemDiscontinuedDomainEvent(Guid InventoryItemId, string Sku) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
