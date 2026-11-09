using FleetIQ.Inventory.Application.Abstractions;
using FleetIQ.Inventory.Application.ListInventoryItems;
using FleetIQ.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Inventory.Infrastructure.Persistence;

public sealed class InventoryItemRepository(InventoryDbContext db) : IInventoryItemRepository
{
    public async Task AddAsync(InventoryItem entity, CancellationToken ct) => await db.InventoryItems.AddAsync(entity, ct);

    public Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(string sku, string warehouseCode, CancellationToken ct) =>
        db.InventoryItems.AnyAsync(x => x.Sku == sku && x.WarehouseCode == warehouseCode, ct);

    public async Task<IReadOnlyList<InventoryItemListItem>> ListAsync(string? warehouseCode, bool lowStockOnly, int take, CancellationToken ct)
    {
        take = take is < 1 or > 200 ? 50 : take;
        var query = db.InventoryItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(warehouseCode))
        {
            var code = InventoryItem.NormalizeCode(warehouseCode);
            query = query.Where(x => x.WarehouseCode == code);
        }

        // Same rule as InventoryItem.IsBelowReorderPoint, expressed so it runs in SQL.
        if (lowStockOnly)
            query = query.Where(x => x.Status == InventoryItemStatus.Active
                                     && x.QuantityOnHand - x.QuantityReserved <= x.ReorderPoint);

        return await query
            .OrderBy(x => x.WarehouseCode).ThenBy(x => x.Sku)
            .Take(take)
            .Select(i => new InventoryItemListItem(
                i.Id, i.Sku, i.Name, i.WarehouseCode, i.UnitOfMeasure, i.QuantityOnHand, i.QuantityReserved,
                i.QuantityOnHand - i.QuantityReserved, i.ReorderPoint,
                i.Status == InventoryItemStatus.Active && i.QuantityOnHand - i.QuantityReserved <= i.ReorderPoint,
                i.Status.ToString(), i.CreatedOnUtc, i.LastMovementOnUtc))
            .ToListAsync(ct);
    }
}
