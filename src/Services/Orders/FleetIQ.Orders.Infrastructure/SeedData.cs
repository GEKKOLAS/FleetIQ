using FleetIQ.Orders.Domain.Orders;
using FleetIQ.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Orders.Infrastructure;

/// <summary>Inserts sample orders on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(OrdersDbContext db)
    {
        if (await db.Orders.AnyAsync()) return;

        var pickup = new Address("1 Main St", "Lisbon", "1000-001", "PT");
        var delivery = new Address("5 Park Ave", "Porto", "4000-002", "PT");

        var first = Order.Create(Guid.NewGuid(), pickup, delivery,
            new[] { new OrderLine("Pallet", 2, 120m), new OrderLine("Box", 5, 15m) });
        var second = Order.Create(Guid.NewGuid(), pickup, delivery,
            new[] { new OrderLine("Crate", 1, 300m) });

        if (first.IsSuccess) db.Orders.Add(first.Value);
        if (second.IsSuccess) db.Orders.Add(second.Value);

        await db.SaveChangesAsync();
    }
}
