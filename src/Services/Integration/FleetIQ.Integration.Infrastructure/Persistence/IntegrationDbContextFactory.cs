using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Integration.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_integration;Username=fleetiq;Password=REDACTED")
            .Options;
        return new IntegrationDbContext(options);
    }
}
