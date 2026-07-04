using FleetIQ.Tracking.Application.Routing;
using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Tracking.Infrastructure;

/// <summary>Seeds shipments in every stage (Created, InTransit, Delivered) on well-known corridors.</summary>
public static class SeedData
{
    public static async Task SeedAsync(TrackingDbContext db)
    {
        if (await db.Shipments.AnyAsync()) return;

        // A few well-known corridors so the demo map shows distinct real road routes immediately.
        var inTransit = new (string O, string D)[] { ("Lisboa", "Porto"), ("Madrid", "Sevilla"), ("Barcelona", "Valencia"), ("Bilbao", "Zaragoza") };
        var created = new (string O, string D)[] { ("Valencia", "Alicante"), ("Porto", "Braga") };
        var delivered = new (string O, string D)[] { ("Faro", "Lisboa") };

        foreach (var (o, d) in inTransit)
        {
            var s = NewShipment(o, d);
            if (s is null) continue;
            s.Dispatch();
            CityGeocoder.TryGet(o, out var oc);
            s.RecordLocation(oc.Lat, oc.Lng, 0); // start at origin; the simulator moves it
            db.Shipments.Add(s);
        }

        foreach (var (o, d) in created)
        {
            var s = NewShipment(o, d);
            if (s is not null) db.Shipments.Add(s);
        }

        foreach (var (o, d) in delivered)
        {
            var s = NewShipment(o, d);
            if (s is null) continue;
            s.Dispatch();
            CityGeocoder.TryGet(d, out var dc);
            s.RecordLocation(dc.Lat, dc.Lng, 0);
            s.Deliver("Warehouse reception");
            db.Shipments.Add(s);
        }

        await db.SaveChangesAsync();
    }

    private static Shipment? NewShipment(string origin, string destination)
    {
        CityGeocoder.TryGet(origin, out var oc);
        CityGeocoder.TryGet(destination, out var dc);
        var s = Shipment.Create(Guid.NewGuid(), Guid.NewGuid(), origin, destination, oc.Lat, oc.Lng, dc.Lat, dc.Lng);
        return s.IsSuccess ? s.Value : null;
    }
}
