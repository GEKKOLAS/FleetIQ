using FleetIQ.Carriers.Domain;
using FleetIQ.Carriers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure;

/// <summary>Sample carriers (Onboarding, with a small fleet) on first run. Verify one via the API/UI
/// to publish CarrierVerified and make it assignable in Planning.</summary>
public static class SeedData
{
    public static async Task SeedAsync(CarriersDbContext db)
    {
        if (await db.Carriers.AnyAsync()) return;

        var samples = new (string Name, string Reg, string Country, (string Plate, VehicleType Type, decimal Cap)[] Fleet, (string Name, string Lic)[] Drivers)[]
        {
            ("Transportes Iberia SL", "ES-B12345678", "ES",
                new[] { ("1234-ABC", VehicleType.Truck, 24000m), ("5678-DEF", VehicleType.Trailer, 30000m) },
                new[] { ("Ana Costa", "ES-D-99001"), ("Luis Pena", "ES-D-99002") }),
            ("Lusitania Cargo Lda", "PT-509876543", "PT",
                new[] { ("AA-01-BB", VehicleType.Refrigerated, 18000m) },
                new[] { ("Joao Silva", "PT-D-44521") }),
            ("Atlantic Freight", "ES-B87654321", "ES",
                new[] { ("9012-GHI", VehicleType.Van, 3500m) },
                new[] { ("Marta Ruiz", "ES-D-77310") }),
        };

        foreach (var s in samples)
        {
            var r = Carrier.Create(s.Name, s.Reg, s.Country);
            if (r.IsFailure) continue;
            var c = r.Value;
            foreach (var v in s.Fleet) c.AddVehicle(v.Plate, v.Type, v.Cap);
            foreach (var d in s.Drivers) c.AddDriver(d.Name, d.Lic);
            db.Carriers.Add(c);
        }
        await db.SaveChangesAsync();
    }
}
