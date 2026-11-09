using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Inventory.Application.Abstractions;

namespace FleetIQ.Inventory.Application.ListInventoryItems;

public sealed record ListInventoryItemsQuery(string? WarehouseCode, bool LowStockOnly, int Take) : IQuery<IReadOnlyList<InventoryItemListItem>>;

public sealed record InventoryItemListItem(
    Guid Id, string Sku, string Name, string WarehouseCode, string UnitOfMeasure,
    decimal QuantityOnHand, decimal QuantityReserved, decimal QuantityAvailable, decimal ReorderPoint,
    bool IsBelowReorderPoint, string Status, DateTime CreatedOnUtc, DateTime? LastMovementOnUtc);

public sealed class ListInventoryItemsQueryHandler(IInventoryItemRepository repository)
    : IQueryHandler<ListInventoryItemsQuery, IReadOnlyList<InventoryItemListItem>>
{
    public Task<IReadOnlyList<InventoryItemListItem>> Handle(ListInventoryItemsQuery query, CancellationToken ct) =>
        repository.ListAsync(query.WarehouseCode, query.LowStockOnly, query.Take, ct);
}
