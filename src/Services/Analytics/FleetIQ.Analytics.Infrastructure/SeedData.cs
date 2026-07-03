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

        var onTime = ReportDefinition.Create("Weekly on-time delivery", "SELECT carrier, avg(delay_min) FROM shipments GROUP BY carrier", "Weekly").Value;
        var revenue = ReportDefinition.Create("Monthly invoiced revenue", "SELECT currency, sum(total) FROM invoices WHERE status = 'Issued' GROUP BY currency", "Monthly").Value;
        var utilization = ReportDefinition.Create("Fleet utilization", "SELECT carrier, sum(weight_kg) / sum(capacity_kg) FROM loads GROUP BY carrier", "Daily").Value;

        // Give one report a bit of history so the runs table has something to show.
        var run = onTime.StartRun();
        if (run.IsSuccess) onTime.CompleteRun(run.Value, 42);

        db.ReportDefinitions.AddRange(onTime, revenue, utilization);
        await db.SaveChangesAsync();
    }
}
