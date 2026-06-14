using FleetIQ.Integration.Domain;
using FleetIQ.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Integration.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(IntegrationDbContext db)
    {
        if (await db.IntegrationConnectors.AnyAsync()) return;

        db.IntegrationConnectors.AddRange(
            IntegrationConnector.Create("SystemName 1", "Endpoint 1"),
            IntegrationConnector.Create("SystemName 2", "Endpoint 2"),
            IntegrationConnector.Create("SystemName 3", "Endpoint 3"));

        await db.SaveChangesAsync();
    }
}
