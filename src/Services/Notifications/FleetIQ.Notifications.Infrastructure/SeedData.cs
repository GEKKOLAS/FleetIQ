using FleetIQ.Notifications.Domain;
using FleetIQ.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Notifications.Infrastructure;

/// <summary>Inserts sample rows on first run (Development only).</summary>
public static class SeedData
{
    public static async Task SeedAsync(NotificationsDbContext db)
    {
        if (await db.Notifications.AnyAsync()) return;

        db.Notifications.AddRange(
            Notification.Create("ops@acme-logistics.io", "Email", "shipment-delayed").Value,
            Notification.Create("+34600111222", "Sms", "delivery-confirmed").Value,
            Notification.Create("driver:7f3a", "Push", "new-load-assigned").Value);

        await db.SaveChangesAsync();
    }
}
