using FleetIQ.Inventory.Domain;
using FleetIQ.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Inventory.Infrastructure;

/// <summary>Seeds stock across two warehouses: healthy, reserved, low-stock, adjusted and discontinued items.</summary>
public static class SeedData
{
    public static async Task SeedAsync(InventoryDbContext db)
    {
        if (await db.InventoryItems.AnyAsync()) return;

        var pallets = InventoryItem.Create("PAL-EUR-1200", "EUR pallet 1200x800", "LIS-01", "unit", 40).Value;
        pallets.Receive(320, "PO-2026-0101");
        pallets.Reserve(60, "ORD-2026-01042");

        var wrap = InventoryItem.Create("WRAP-23MU", "Stretch film 23µm roll", "LIS-01", "roll", 25).Value;
        wrap.Receive(48, "PO-2026-0102");
        wrap.Reserve(10, "ORD-2026-01044");
        wrap.Ship(10, "ORD-2026-01044");
        wrap.Adjust(30, "Cycle count: 8 rolls damaged");

        var straps = InventoryItem.Create("STRAP-50MM", "Ratchet strap 50mm 8m", "OPO-02", "unit", 30).Value;
        straps.Receive(36, "PO-2026-0107");
        straps.Reserve(12, "ORD-2026-01047");

        var coolant = InventoryItem.Create("REEF-GEL-5KG", "Cold-chain gel pack 5kg", "OPO-02", "unit", 50).Value;
        coolant.Receive(400, "PO-2026-0110");
        coolant.Reserve(120, "ORD-2026-01049");
        coolant.Ship(80, "ORD-2026-01049");

        var labels = InventoryItem.Create("LBL-A6-THERM", "Thermal shipping label A6", "LIS-01", "box", 15).Value;
        labels.Receive(12, "PO-2026-0111");

        var oldBags = InventoryItem.Create("BAG-PE-OLD", "PE bag (legacy size)", "OPO-02", "box", 0).Value;
        oldBags.Receive(5, "PO-2025-0904");
        oldBags.Adjust(0, "Written off - obsolete packaging");
        oldBags.Discontinue();

        db.InventoryItems.AddRange(pallets, wrap, straps, coolant, labels, oldBags);
        await db.SaveChangesAsync();
    }
}
