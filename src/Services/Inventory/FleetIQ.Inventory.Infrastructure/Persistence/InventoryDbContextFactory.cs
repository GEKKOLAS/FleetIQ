using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetIQ.Inventory.Infrastructure.Persistence;

/// <summary>Lets 'dotnet ef' create the context without building the web host (no app startup, no seeding).</summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=fleetiq_inventory;Username=fleetiq")
            .Options;
        return new InventoryDbContext(options);
    }
}
