using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Planning.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class PlanningDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    public PlanningDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_planning;Username=fleetiq;Password=REDACTED")
            .Options;
        return new PlanningDbContext(options);
    }
}
