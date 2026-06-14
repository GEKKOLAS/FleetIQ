using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(TrackingDbContext db)
    {
        if (await db.Shipments.AnyAsync()) return;

        db.Shipments.AddRange(
            Shipment.Create(Guid.NewGuid(), Guid.NewGuid()),
            Shipment.Create(Guid.NewGuid(), Guid.NewGuid()),
            Shipment.Create(Guid.NewGuid(), Guid.NewGuid()));

        await db.SaveChangesAsync();
    }
}
