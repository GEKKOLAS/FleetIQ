using FleetIQ.Carriers.Domain;
using FleetIQ.Carriers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(CarriersDbContext db)
    {
        if (await db.Carriers.AnyAsync()) return;

        db.Carriers.AddRange(
            Carrier.Create("LegalName 1", "RegistrationNumber 1", "Country 1"),
            Carrier.Create("LegalName 2", "RegistrationNumber 2", "Country 2"),
            Carrier.Create("LegalName 3", "RegistrationNumber 3", "Country 3"));

        await db.SaveChangesAsync();
    }
}
