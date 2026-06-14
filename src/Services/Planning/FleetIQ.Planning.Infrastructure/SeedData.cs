using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(PlanningDbContext db)
    {
        if (await db.Loads.AnyAsync()) return;

        db.Loads.AddRange(
            Load.Create(Guid.NewGuid(), "OriginCity 1", "DestinationCity 1", 100m),
            Load.Create(Guid.NewGuid(), "OriginCity 2", "DestinationCity 2", 250m),
            Load.Create(Guid.NewGuid(), "OriginCity 3", "DestinationCity 3", 75m));

        await db.SaveChangesAsync();
    }
}
