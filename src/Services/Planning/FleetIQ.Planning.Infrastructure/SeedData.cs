using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure;

/// <summary>Seeds loads across the lifecycle (Draft, Planned, Confirmed, Cancelled) on first run.</summary>
public static class SeedData
{
    public static async Task SeedAsync(PlanningDbContext db)
    {
        if (await db.Loads.AnyAsync()) return;

        var samples = new (string Origin, string Dest, string State, (string Desc, decimal Kg)[] Orders)[]
        {
            ("Lisboa", "Porto", "Confirmed", new[] { ("Pallet electronics", 320m), ("Spare parts", 140m) }),
            ("Madrid", "Sevilla", "Confirmed", new[] { ("Refrigerated produce", 900m) }),
            ("Barcelona", "Valencia", "Planned", new[] { ("Flat-pack furniture", 480m), ("Textile rolls", 110m) }),
            ("Bilbao", "Zaragoza", "Draft", new[] { ("Machined steel parts", 930m) }),
            ("Porto", "Braga", "Draft", new[] { ("Wine cases", 720m), ("Cork sheets", 260m) }),
            ("Valencia", "Alicante", "Cancelled", new[] { ("Citrus crates", 550m) })
        };

        foreach (var s in samples)
        {
            var created = Load.Create(s.Origin, s.Dest);
            if (created.IsFailure) continue;
            var load = created.Value;
            foreach (var (desc, kg) in s.Orders) load.AddOrder(Guid.NewGuid(), desc, kg);

            switch (s.State)
            {
                case "Planned":
                    load.AssignCarrier(Guid.NewGuid());
                    load.AttachRoute(Guid.NewGuid());
                    load.Plan();
                    break;
                case "Confirmed":
                    load.AssignCarrier(Guid.NewGuid());
                    load.AttachRoute(Guid.NewGuid());
                    load.Plan();
                    load.Confirm();
                    break;
                case "Cancelled":
                    load.Cancel();
                    break;
            }

            db.Loads.Add(load);
        }

        await db.SaveChangesAsync();
    }
}
