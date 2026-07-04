using FleetIQ.Carriers.Domain;
using FleetIQ.Carriers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure;

/// <summary>Sample carriers across the lifecycle (Verified, Onboarding, Suspended) on first run.</summary>
public static class SeedData
{
    public static async Task SeedAsync(CarriersDbContext db)
    {
        if (await db.Carriers.AnyAsync()) return;

        var samples = new (string Name, string Reg, string Country, string State, (string Plate, VehicleType Type, decimal Cap)[] Fleet, (string Name, string Lic)[] Drivers)[]
        {
            ("Transportes Iberia SL", "ES-B12345678", "ES", "Verified",
                new[] { ("1234-ABC", VehicleType.Truck, 24000m), ("5678-DEF", VehicleType.Trailer, 30000m), ("4321-JKL", VehicleType.Truck, 24000m) },
                new[] { ("Ana Costa", "ES-D-99001"), ("Luis Pena", "ES-D-99002"), ("Carmen Vidal", "ES-D-99003") }),
            ("Lusitania Cargo Lda", "PT-509876543", "PT", "Verified",
                new[] { ("AA-01-BB", VehicleType.Refrigerated, 18000m), ("CC-02-DD", VehicleType.Truck, 22000m) },
                new[] { ("Joao Silva", "PT-D-44521"), ("Ines Martins", "PT-D-44522") }),
            ("Atlantic Freight", "ES-B87654321", "ES", "Onboarding",
                new[] { ("9012-GHI", VehicleType.Van, 3500m) },
                new[] { ("Marta Ruiz", "ES-D-77310") }),
            ("Cantabria Logistics", "ES-B11223344", "ES", "Suspended",
                new[] { ("7788-MNO", VehicleType.Trailer, 32000m) },
                new[] { ("Pablo Serrano", "ES-D-55890") }),
            ("Duero Freight Lda", "PT-501234987", "PT", "Onboarding",
                new (string, VehicleType, decimal)[] { },
                new (string, string)[] { })
        };

        foreach (var s in samples)
        {
            var r = Carrier.Create(s.Name, s.Reg, s.Country);
            if (r.IsFailure) continue;
            var c = r.Value;
            foreach (var v in s.Fleet) c.AddVehicle(v.Plate, v.Type, v.Cap);
            foreach (var d in s.Drivers) c.AddDriver(d.Name, d.Lic);

            switch (s.State)
            {
                case "Verified":
                    c.Verify();
                    break;
                case "Suspended":
                    c.Verify();
                    c.Suspend("Insurance certificate expired");
                    break;
            }

            db.Carriers.Add(c);
        }

        await db.SaveChangesAsync();
    }
}
