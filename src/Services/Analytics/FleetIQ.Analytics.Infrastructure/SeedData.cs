using FleetIQ.Analytics.Domain;
using FleetIQ.Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Analytics.Infrastructure;

/// <summary>Seeds report definitions with run history (successful, failed and in-progress runs).</summary>
public static class SeedData
{
    public static async Task SeedAsync(AnalyticsDbContext db)
    {
        if (await db.ReportDefinitions.AnyAsync()) return;

        var onTime = ReportDefinition.Create("Weekly on-time delivery", "SELECT carrier, avg(delay_min) FROM shipments GROUP BY carrier", "Weekly").Value;
        var run1 = onTime.StartRun();
        if (run1.IsSuccess) onTime.CompleteRun(run1.Value, 42);
        var run2 = onTime.StartRun();
        if (run2.IsSuccess) onTime.CompleteRun(run2.Value, 47);

        var revenue = ReportDefinition.Create("Monthly invoiced revenue", "SELECT currency, sum(total) FROM invoices WHERE status = 'Issued' GROUP BY currency", "Monthly").Value;
        var run3 = revenue.StartRun();
        if (run3.IsSuccess) revenue.FailRun(run3.Value, "Query timeout after 30s");
        var run4 = revenue.StartRun();
        if (run4.IsSuccess) revenue.CompleteRun(run4.Value, 12);

        var utilization = ReportDefinition.Create("Fleet utilization", "SELECT carrier, sum(weight_kg) / sum(capacity_kg) FROM loads GROUP BY carrier", "Daily").Value;
        utilization.StartRun(); // in progress

        var adhoc = ReportDefinition.Create("Ad-hoc: Q2 customs delays", "SELECT * FROM shipments WHERE exception LIKE '%customs%'", "OnDemand").Value;
        adhoc.Archive();

        db.ReportDefinitions.AddRange(onTime, revenue, utilization, adhoc);
        await db.SaveChangesAsync();
    }
}
