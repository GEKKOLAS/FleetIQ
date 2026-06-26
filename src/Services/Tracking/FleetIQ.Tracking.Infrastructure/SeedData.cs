using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure;

public static class SeedData
{
    public static async Task SeedAsync(TrackingDbContext db)
    {
        if (await db.Shipments.AnyAsync()) return;

        for (var i = 0; i < 2; i++)
        {
            var s = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
            if (s.IsSuccess)
            {
                s.Value.Dispatch();
                s.Value.RecordLocation(38.7223, -9.1393, 0); // Lisbon
                db.Shipments.Add(s.Value);
            }
        }
        await db.SaveChangesAsync();
    }
}
