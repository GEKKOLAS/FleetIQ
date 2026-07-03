using FleetIQ.Integration.Domain;
using FleetIQ.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Integration.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(IntegrationDbContext db)
    {
        if (await db.IntegrationConnectors.AnyAsync()) return;

        var erp = IntegrationConnector.Create("SAP ERP", "https://erp.example.com/api").Value;
        erp.Activate();
        erp.RecordSyncSuccess();

        var gps = IntegrationConnector.Create("Fleet GPS provider", "https://gps.example.com/v2").Value;
        gps.Activate();

        var wms = IntegrationConnector.Create("Warehouse WMS", "https://wms.example.com/hooks").Value;

        db.IntegrationConnectors.AddRange(erp, gps, wms);
        await db.SaveChangesAsync();
    }
}
