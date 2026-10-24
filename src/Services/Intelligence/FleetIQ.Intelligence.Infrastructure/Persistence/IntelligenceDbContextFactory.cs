using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Intelligence.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class IntelligenceDbContextFactory : IDesignTimeDbContextFactory<IntelligenceDbContext>
{
    public IntelligenceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IntelligenceDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_intelligence;Username=fleetiq")
            .Options;
        return new IntelligenceDbContext(options);
    }
}
