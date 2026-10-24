using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Carriers.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class CarriersDbContextFactory : IDesignTimeDbContextFactory<CarriersDbContext>
{
    public CarriersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CarriersDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_carriers;Username=fleetiq")
            .Options;
        return new CarriersDbContext(options);
    }
}
