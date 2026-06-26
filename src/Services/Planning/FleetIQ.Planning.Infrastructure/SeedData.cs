using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure;

/// <summary>Inserts sample Draft loads (with orders) on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(PlanningDbContext db)
    {
        if (await db.Loads.AnyAsync()) return;

        var samples = new[]
        {
            ("Lisboa", "Porto", new[] { ("Pallet electronics", 320m), ("Spare parts", 140m) }),
            ("Madrid", "Sevilla", new[] { ("Refrigerated goods", 900m) }),
            ("Barcelona", "Valencia", new[] { ("Furniture", 480m), ("Textiles", 110m) }),
        };

        foreach (var (origin, dest, orders) in samples)
        {
            var result = Load.Create(origin, dest);
            if (result.IsFailure) continue;
            var load = result.Value;
            foreach (var (desc, weight) in orders)
                load.AddOrder(Guid.NewGuid(), desc, weight);
            db.Loads.Add(load);
        }

        await db.SaveChangesAsync();
    }
}
