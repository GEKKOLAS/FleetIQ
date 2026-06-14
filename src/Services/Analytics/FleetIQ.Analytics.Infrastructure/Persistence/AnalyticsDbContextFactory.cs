using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Analytics.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class AnalyticsDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_analytics;Username=fleetiq;Password=REDACTED")
            .Options;
        return new AnalyticsDbContext(options);
    }
}
