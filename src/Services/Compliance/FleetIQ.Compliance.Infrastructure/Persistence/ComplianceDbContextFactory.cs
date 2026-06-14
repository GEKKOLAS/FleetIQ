using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Compliance.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class ComplianceDbContextFactory : IDesignTimeDbContextFactory<ComplianceDbContext>
{
    public ComplianceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_compliance;Username=fleetiq;Password=REDACTED")
            .Options;
        return new ComplianceDbContext(options);
    }
}
