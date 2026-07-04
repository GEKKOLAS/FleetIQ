using FleetIQ.Orders.Domain.Orders;
using FleetIQ.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Orders.Infrastructure;

/// <summary>Seeds orders across the whole lifecycle on first run (Development only).</summary>
public static class SeedData
{
    private static readonly Guid Acme = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Nordic = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(OrdersDbContext db)
    {
        if (await db.Orders.AnyAsync()) return;

        var samples = new (Guid Customer, string PickCity, string PickPc, string DelCity, string DelPc, (string, int, decimal)[] Items, string State)[]
        {
            (Acme, "Lisboa", "1000-001", "Porto", "4000-002", new[] { ("Pallet electronics", 2, 120m), ("Spare parts box", 5, 15m) }, "ReadyForPlanning"),
            (Acme, "Madrid", "28001", "Sevilla", "41001", new[] { ("Refrigerated produce", 6, 150m) }, "ReadyForPlanning"),
            (Acme, "Barcelona", "08001", "Valencia", "46001", new[] { ("Flat-pack furniture", 12, 40m), ("Textile rolls", 8, 25m) }, "Confirmed"),
            (Nordic, "Bilbao", "48001", "Zaragoza", "50001", new[] { ("Machined steel parts", 3, 310m) }, "Confirmed"),
            (Acme, "Porto", "4000-002", "Braga", "4700-001", new[] { ("Wine cases", 40, 18m) }, "Confirmed"),
            (Nordic, "Valencia", "46001", "Alicante", "03001", new[] { ("Citrus crates", 25, 22m) }, "Draft"),
            (Acme, "Faro", "8000-001", "Lisboa", "1000-001", new[] { ("Seafood (chilled)", 10, 30m) }, "Draft"),
            (Acme, "Coimbra", "3000-001", "Porto", "4000-002", new[] { ("Lab equipment", 1, 95m) }, "Cancelled")
        };

        foreach (var s in samples)
        {
            var pickup = new Address("1 Industrial Park", s.PickCity, s.PickPc, s.PickCity.Length > 0 && "Lisboa|Porto|Braga|Faro|Coimbra".Contains(s.PickCity) ? "PT" : "ES");
            var delivery = new Address("5 Logistics Hub", s.DelCity, s.DelPc, "Lisboa|Porto|Braga|Faro|Coimbra".Contains(s.DelCity) ? "PT" : "ES");

            var created = Order.Create(s.Customer, pickup, delivery, s.Items);
            if (created.IsFailure) continue;
            var order = created.Value;

            switch (s.State)
            {
                case "Confirmed":
                    order.Confirm();
                    break;
                case "ReadyForPlanning":
                    order.Confirm();
                    order.MarkReadyForPlanning();
                    break;
                case "Cancelled":
                    order.Cancel();
                    break;
            }

            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }
}
