using FleetIQ.Compliance.Domain;
using FleetIQ.Compliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Compliance.Infrastructure;

/// <summary>Seeds compliance checks in every state (Pending, Passed, Failed, Waived) with findings.</summary>
public static class SeedData
{
    public static async Task SeedAsync(ComplianceDbContext db)
    {
        if (await db.ComplianceChecks.AnyAsync()) return;

        var hos = ComplianceCheck.Create(Guid.NewGuid(), "Driver", "HOS-11H", "EU").Value;
        hos.AddFinding("HOS-11H", "High", "Driver exceeded 11h daily driving limit twice this week.");
        hos.Fail();

        var insurance = ComplianceCheck.Create(Guid.NewGuid(), "Carrier", "INS-VALID", "ES").Value;
        insurance.AddFinding("INS-VALID", "Low", "Certificate copy is low resolution; original verified by phone.");
        insurance.Pass();

        var customs = ComplianceCheck.Create(Guid.NewGuid(), "Shipment", "CUSTOMS-T1", "EU").Value;

        var tacho = ComplianceCheck.Create(Guid.NewGuid(), "Vehicle", "TACHO-CAL", "PT").Value;
        tacho.AddFinding("TACHO-CAL", "Critical", "Tachograph calibration certificate expired 3 months ago.");
        tacho.Fail();
        tacho.Waive("Vehicle scheduled for calibration on Friday; regulator notified.");

        var licence = ComplianceCheck.Create(Guid.NewGuid(), "Driver", "LIC-ADR", "ES").Value;
        licence.Pass();

        var weights = ComplianceCheck.Create(Guid.NewGuid(), "Load", "MAX-WEIGHT", "EU").Value;
        weights.AddFinding("MAX-WEIGHT", "Medium", "Load within 2% of the legal axle-weight limit; re-check at pickup.");

        db.ComplianceChecks.AddRange(hos, insurance, customs, tacho, licence, weights);
        await db.SaveChangesAsync();
    }
}
