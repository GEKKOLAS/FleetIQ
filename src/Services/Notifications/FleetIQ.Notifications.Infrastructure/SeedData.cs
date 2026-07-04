using FleetIQ.Notifications.Domain;
using FleetIQ.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Notifications.Infrastructure;

/// <summary>Seeds notifications in every state (Pending, Sent, Failed) across all channels.</summary>
public static class SeedData
{
    public static async Task SeedAsync(NotificationsDbContext db)
    {
        if (await db.Notifications.AnyAsync()) return;

        var invoiceMail = Notification.Create("billing@acme-logistics.io", "Email", "invoice-issued").Value;
        invoiceMail.MarkSent();

        var delaySms = Notification.Create("+34600111222", "Sms", "shipment-delayed").Value;
        delaySms.MarkFailed("Carrier SMS gateway timeout");

        var podPush = Notification.Create("driver:ana.costa", "Push", "pod-required").Value;
        podPush.MarkSent();

        var welcome = Notification.Create("ops@nordic-forwarding.se", "Email", "welcome-onboarding").Value;

        var deliveredSms = Notification.Create("+351910333444", "Sms", "delivery-confirmed").Value;
        deliveredSms.MarkSent();

        var loadPush = Notification.Create("driver:joao.silva", "Push", "new-load-assigned").Value;

        db.Notifications.AddRange(invoiceMail, delaySms, podPush, welcome, deliveredSms, loadPush);
        await db.SaveChangesAsync();
    }
}
