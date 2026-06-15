using FleetIQ.Contracts;
using FleetIQ.Notifications.Domain;
using FleetIQ.Notifications.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Notifications.Infrastructure.Consumers;

/// <summary>Reacts to an invoice being issued by creating a notification for the customer.</summary>
public sealed class InvoiceIssuedConsumer(NotificationsDbContext db, ILogger<InvoiceIssuedConsumer> logger)
    : IConsumer<InvoiceIssuedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InvoiceIssuedIntegrationEvent> context)
    {
        var message = context.Message;

        var notification = Notification.Create($"customer:{message.CustomerId}", "Email", "invoice-issued");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "[Notifications] Invoice {InvoiceId} issued ({Total} {Currency}) -> notification {NotificationId} created",
            message.InvoiceId, message.Total, message.Currency, notification.Id);
    }
}
