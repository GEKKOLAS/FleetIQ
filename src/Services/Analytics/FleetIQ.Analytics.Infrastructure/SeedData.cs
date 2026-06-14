using FleetIQ.Analytics.Domain;
using FleetIQ.Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Analytics.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(AnalyticsDbContext db)
    {
        if (await db.ReportDefinitions.AnyAsync()) return;

        db.ReportDefinitions.AddRange(
            ReportDefinition.Create("Name 1", "Query 1"),
            ReportDefinition.Create("Name 2", "Query 2"),
            ReportDefinition.Create("Name 3", "Query 3"));

        await db.SaveChangesAsync();
    }
}
