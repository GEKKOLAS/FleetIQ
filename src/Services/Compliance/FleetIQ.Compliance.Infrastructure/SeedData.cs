using FleetIQ.Compliance.Domain;
using FleetIQ.Compliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Compliance.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(ComplianceDbContext db)
    {
        if (await db.ComplianceChecks.AnyAsync()) return;

        var hos = ComplianceCheck.Create(Guid.NewGuid(), "Driver", "HOS-11H", "EU").Value;
        hos.AddFinding("HOS-11H", "High", "Driver exceeded 11h daily driving limit twice this week.");
        hos.Fail();

        var insurance = ComplianceCheck.Create(Guid.NewGuid(), "Carrier", "INS-VALID", "ES").Value;
        insurance.Pass();

        var customs = ComplianceCheck.Create(Guid.NewGuid(), "Shipment", "CUSTOMS-T1", "EU").Value;

        db.ComplianceChecks.AddRange(hos, insurance, customs);
        await db.SaveChangesAsync();
    }
}
