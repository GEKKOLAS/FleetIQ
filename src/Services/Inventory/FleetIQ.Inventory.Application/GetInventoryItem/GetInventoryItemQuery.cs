using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Inventory.Application.Abstractions;

namespace FleetIQ.Inventory.Application.GetInventoryItem;

public sealed record GetInventoryItemQuery(Guid Id) : IQuery<InventoryItemResponse?>;

public sealed record StockMovementLine(
    string Type, decimal Quantity, string Reference, decimal OnHandAfter, decimal ReservedAfter, DateTime OccurredOnUtc);

public sealed record InventoryItemResponse(
    Guid Id,
    string Sku,
    string Name,
    string WarehouseCode,
    string UnitOfMeasure,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    decimal ReorderPoint,
    bool IsBelowReorderPoint,
    string Status,
    DateTime CreatedOnUtc,
    DateTime? LastMovementOnUtc,
    IReadOnlyList<StockMovementLine> Movements);

public sealed class GetInventoryItemQueryHandler(IInventoryItemRepository repository)
    : IQueryHandler<GetInventoryItemQuery, InventoryItemResponse?>
{
    public async Task<InventoryItemResponse?> Handle(GetInventoryItemQuery query, CancellationToken ct)
    {
        var i = await repository.GetByIdAsync(query.Id, ct);
        return i is null
            ? null
            : new InventoryItemResponse(
                i.Id, i.Sku, i.Name, i.WarehouseCode, i.UnitOfMeasure, i.QuantityOnHand, i.QuantityReserved,
                i.QuantityAvailable, i.ReorderPoint, i.IsBelowReorderPoint, i.Status.ToString(), i.CreatedOnUtc, i.LastMovementOnUtc,
                i.Movements.OrderByDescending(m => m.OccurredOnUtc)
                    .Select(m => new StockMovementLine(m.Type.ToString(), m.Quantity, m.Reference, m.OnHandAfter, m.ReservedAfter, m.OccurredOnUtc))
                    .ToList());
    }
}
