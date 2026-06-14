using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Tracking.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class TrackingDbContextFactory : IDesignTimeDbContextFactory<TrackingDbContext>
{
    public TrackingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_tracking;Username=fleetiq;Password=REDACTED")
            .Options;
        return new TrackingDbContext(options);
    }
}
