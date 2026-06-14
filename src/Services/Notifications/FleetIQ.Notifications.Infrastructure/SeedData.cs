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
            Notification.Create("Recipient 1", "Channel 1", "TemplateId 1"),
            Notification.Create("Recipient 2", "Channel 2", "TemplateId 2"),
            Notification.Create("Recipient 3", "Channel 3", "TemplateId 3"));

        await db.SaveChangesAsync();
    }
}
