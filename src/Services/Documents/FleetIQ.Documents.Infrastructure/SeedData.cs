using FleetIQ.Documents.Domain;
using FleetIQ.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure;

/// <summary>Seeds documents across the lifecycle (Uploaded, Verified, Rejected, re-uploaded, Archived).</summary>
public static class SeedData
{
    private static readonly Guid Acme = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Nordic = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(DocumentsDbContext db)
    {
        if (await db.Documents.AnyAsync()) return;

        var cmr = Document.Create(Acme, "CMR", "s3://fleetiq-docs/cmr-2026-0001.pdf").Value;
        cmr.Verify();

        var insurance = Document.Create(Acme, "Insurance", "s3://fleetiq-docs/insurance-2026.pdf", DateTime.UtcNow.AddMonths(6)).Value;
        insurance.Verify();

        var licence = Document.Create(Nordic, "Other", "s3://fleetiq-docs/operator-licence.pdf", DateTime.UtcNow.AddDays(20)).Value;

        var pod = Document.Create(Acme, "Proof of delivery", "s3://fleetiq-docs/pod-0007.pdf").Value;

        var customs = Document.Create(Nordic, "Customs", "s3://fleetiq-docs/customs-t1-draft.pdf").Value;
        customs.Reject("Missing HS codes on page 2");
        customs.AddVersion("s3://fleetiq-docs/customs-t1-v2.pdf");

        var oldContract = Document.Create(Acme, "Other", "s3://fleetiq-docs/contract-2024.pdf").Value;
        oldContract.Verify();
        oldContract.Archive();

        db.Documents.AddRange(cmr, insurance, licence, pod, customs, oldContract);
        await db.SaveChangesAsync();
    }
}
