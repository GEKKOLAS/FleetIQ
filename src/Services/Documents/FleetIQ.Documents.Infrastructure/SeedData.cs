using FleetIQ.Documents.Domain;
using FleetIQ.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    private static readonly Guid Customer = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(DocumentsDbContext db)
    {
        if (await db.Documents.AnyAsync()) return;

        db.Documents.AddRange(
            Document.Create(Customer, "CMR", "s3://fleetiq-docs/cmr-0001.pdf").Value,
            Document.Create(Customer, "Insurance", "s3://fleetiq-docs/insurance-2026.pdf", DateTime.UtcNow.AddMonths(6)).Value,
            Document.Create(Guid.NewGuid(), "Proof of delivery", "s3://fleetiq-docs/pod-0007.pdf").Value);

        await db.SaveChangesAsync();
    }
}
