using FleetIQ.Billing.Domain;
using FleetIQ.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure;

/// <summary>Seeds a draft and an issued invoice on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(BillingDbContext db)
    {
        if (await db.Invoices.AnyAsync()) return;

        var draft = Invoice.Create(Guid.NewGuid(), "EUR",
            new[] { ("Freight Lisbon -> Porto", 1, 850.00m), ("Fuel surcharge", 1, 120.50m) });

        var issued = Invoice.Create(Guid.NewGuid(), "USD",
            new[] { ("Long-haul freight", 2, 1500.00m) });
        if (issued.IsSuccess) issued.Value.Issue(DateTime.UtcNow.AddDays(30));

        if (draft.IsSuccess) db.Invoices.Add(draft.Value);
        if (issued.IsSuccess) db.Invoices.Add(issued.Value);

        await db.SaveChangesAsync();
    }
}
