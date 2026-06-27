using FleetIQ.Tracking.Application.Routing;
using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure;

public static class SeedData
{
    public static async Task SeedAsync(TrackingDbContext db)
    {
        if (await db.Shipments.AnyAsync()) return;

        // A few well-known corridors so the demo map shows distinct real road routes immediately.
        var trips = new (string O, string D)[] { ("Lisboa", "Porto"), ("Madrid", "Sevilla"), ("Barcelona", "Valencia") };

        foreach (var (o, d) in trips)
        {
            CityGeocoder.TryGet(o, out var oc);
            CityGeocoder.TryGet(d, out var dc);
            var s = Shipment.Create(Guid.NewGuid(), Guid.NewGuid(), o, d, oc.Lat, oc.Lng, dc.Lat, dc.Lng);
            if (s.IsSuccess)
            {
                s.Value.Dispatch();
                s.Value.RecordLocation(oc.Lat, oc.Lng, 0); // start at origin
                db.Shipments.Add(s.Value);
            }
        }
        await db.SaveChangesAsync();
    }
}
