using FleetIQ.Billing.Domain;
using FleetIQ.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(BillingDbContext db)
    {
        if (await db.Invoices.AnyAsync()) return;

        db.Invoices.AddRange(
            Invoice.Create(Guid.NewGuid(), 100m, "Currency 1"),
            Invoice.Create(Guid.NewGuid(), 250m, "Currency 2"),
            Invoice.Create(Guid.NewGuid(), 75m, "Currency 3"));

        await db.SaveChangesAsync();
    }
}
