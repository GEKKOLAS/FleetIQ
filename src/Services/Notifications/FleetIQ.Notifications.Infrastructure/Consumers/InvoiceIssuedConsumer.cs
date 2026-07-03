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

        var created = Notification.Create($"customer:{message.CustomerId}", "Email", "invoice-issued");
        if (created.IsFailure)
        {
            logger.LogWarning("[Notifications] Invoice {InvoiceId} issued but notification rejected: {Error}",
                message.InvoiceId, created.Error.Description);
            return;
        }

        db.Notifications.Add(created.Value);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "[Notifications] Invoice {InvoiceId} issued ({Total} {Currency}) -> notification {NotificationId} created",
            message.InvoiceId, message.Total, message.Currency, created.Value.Id);
    }
}
