using FleetIQ.Integration.Domain;
using FleetIQ.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Integration.Infrastructure;

/// <summary>Seeds connectors in every state, including one auto-disabled by consecutive failures.</summary>
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
        gps.RecordSyncFailure("TLS handshake timeout");
        gps.RecordSyncFailure("TLS handshake timeout"); // 2 in a row: one more auto-disables it

        var wms = IntegrationConnector.Create("Warehouse WMS", "https://wms.example.com/hooks").Value;

        var marketplace = IntegrationConnector.Create("Freight marketplace", "https://loads.example.com/api/v1").Value;
        marketplace.Activate();
        marketplace.RecordSyncFailure("HTTP 500 from partner");
        marketplace.RecordSyncFailure("HTTP 500 from partner");
        marketplace.RecordSyncFailure("HTTP 500 from partner"); // auto-disabled

        var legacyTms = IntegrationConnector.Create("Legacy TMS", "https://tms-old.example.com/soap").Value;
        legacyTms.Activate();
        legacyTms.RecordSyncSuccess();
        legacyTms.Disable("Deprecated endpoint; migration to REST planned");

        db.IntegrationConnectors.AddRange(erp, gps, wms, marketplace, legacyTms);
        await db.SaveChangesAsync();
    }
}
