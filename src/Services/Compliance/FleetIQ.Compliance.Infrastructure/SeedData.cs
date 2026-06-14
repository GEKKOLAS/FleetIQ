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

        db.ComplianceChecks.AddRange(
            ComplianceCheck.Create(Guid.NewGuid(), "RuleCode 1", "Jurisdiction 1"),
            ComplianceCheck.Create(Guid.NewGuid(), "RuleCode 2", "Jurisdiction 2"),
            ComplianceCheck.Create(Guid.NewGuid(), "RuleCode 3", "Jurisdiction 3"));

        await db.SaveChangesAsync();
    }
}
