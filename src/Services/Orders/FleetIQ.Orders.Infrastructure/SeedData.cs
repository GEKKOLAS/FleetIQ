using FleetIQ.Orders.Domain.Orders;
using FleetIQ.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Orders.Infrastructure;

public static class SeedData
{
    public static async Task SeedAsync(OrdersDbContext db)
    {
        if (await db.Orders.AnyAsync()) return;

        var pickup = new Address("1 Main St", "Lisbon", "1000-001", "PT");
        var delivery = new Address("5 Park Ave", "Porto", "4000-002", "PT");

        var draft = Order.Create(Guid.NewGuid(), pickup, delivery,
            new[] { ("Pallet", 2, 120m), ("Box", 5, 15m) });

        var confirmed = Order.Create(Guid.NewGuid(), pickup, delivery,
            new[] { ("Crate", 1, 300m) });
        if (confirmed.IsSuccess) confirmed.Value.Confirm();

        if (draft.IsSuccess) db.Orders.Add(draft.Value);
        if (confirmed.IsSuccess) db.Orders.Add(confirmed.Value);

        await db.SaveChangesAsync();
    }
}
