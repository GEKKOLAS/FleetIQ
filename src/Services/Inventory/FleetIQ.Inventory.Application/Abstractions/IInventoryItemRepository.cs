using FleetIQ.Inventory.Application.ListInventoryItems;
using FleetIQ.Inventory.Domain;

namespace FleetIQ.Inventory.Application.Abstractions;

public interface IInventoryItemRepository
{
    Task AddAsync(InventoryItem entity, CancellationToken ct);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(string sku, string warehouseCode, CancellationToken ct);
    /// <summary>Projects list rows in the database, so the movement ledger is never loaded for the list.</summary>
    Task<IReadOnlyList<InventoryItemListItem>> ListAsync(string? warehouseCode, bool lowStockOnly, int take, CancellationToken ct);
}
