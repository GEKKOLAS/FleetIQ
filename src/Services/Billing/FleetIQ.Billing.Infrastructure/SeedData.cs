using FleetIQ.Billing.Domain;
using FleetIQ.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Billing.Infrastructure;

/// <summary>Seeds invoices across the lifecycle (Draft, Issued, Paid, Cancelled) on first run.</summary>
public static class SeedData
{
    private static readonly Guid Acme = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Nordic = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(BillingDbContext db)
    {
        if (await db.Invoices.AnyAsync()) return;

        var samples = new (Guid Customer, string Currency, string State, (string Desc, int Qty, decimal Price)[] Lines)[]
        {
            (Acme, "EUR", "Paid", new[] { ("Freight Lisboa -> Porto", 1, 850.00m), ("Fuel surcharge", 1, 120.50m) }),
            (Acme, "EUR", "Issued", new[] { ("Freight Madrid -> Sevilla (reefer)", 1, 1240.00m), ("Toll recovery", 1, 86.40m) }),
            (Nordic, "EUR", "Issued", new[] { ("Freight Barcelona -> Valencia", 2, 640.00m) }),
            (Acme, "USD", "Issued", new[] { ("Long-haul freight", 2, 1500.00m), ("Customs handling", 1, 210.00m) }),
            (Acme, "EUR", "Draft", new[] { ("Freight Bilbao -> Zaragoza", 1, 720.00m) }),
            (Nordic, "EUR", "Draft", new[] { ("Freight Porto -> Braga", 1, 310.00m), ("Waiting time (2h)", 2, 45.00m) }),
            (Acme, "EUR", "Cancelled", new[] { ("Freight Valencia -> Alicante", 1, 480.00m) })
        };

        foreach (var s in samples)
        {
            var created = Invoice.Create(s.Customer, s.Currency, s.Lines);
            if (created.IsFailure) continue;
            var invoice = created.Value;

            switch (s.State)
            {
                case "Issued":
                    invoice.Issue(DateTime.UtcNow.AddDays(30));
                    break;
                case "Paid":
                    invoice.Issue(DateTime.UtcNow.AddDays(30));
                    invoice.RecordPayment();
                    break;
                case "Cancelled":
                    invoice.Cancel();
                    break;
            }

            db.Invoices.Add(invoice);
        }

        await db.SaveChangesAsync();
    }
}
