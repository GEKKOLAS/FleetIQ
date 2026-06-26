using FleetIQ.Contracts;
using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Tracking.Infrastructure.Consumers;

/// <summary>Planning -> Tracking handoff: when a load is confirmed, create the Shipment that
/// will execute it. Idempotent: skips if a shipment already exists for the load.</summary>
public sealed class LoadConfirmedConsumer(TrackingDbContext db, ILogger<LoadConfirmedConsumer> logger)
    : IConsumer<LoadConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<LoadConfirmedIntegrationEvent> context)
    {
        var message = context.Message;

        var exists = await db.Shipments.AnyAsync(s => s.LoadId == message.LoadId, context.CancellationToken);
        if (exists)
        {
            logger.LogInformation("[Tracking] Shipment for load {LoadId} already exists - skipping.", message.LoadId);
            return;
        }

        var result = Shipment.Create(message.LoadId, message.CarrierId);
        if (result.IsFailure)
        {
            logger.LogWarning("[Tracking] Could not create shipment for load {LoadId}: {Error}", message.LoadId, result.Error.Code);
            return;
        }

        db.Shipments.Add(result.Value);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "[Tracking] Load {LoadId} confirmed ({Origin} -> {Destination}) -> shipment {ShipmentId} created",
            message.LoadId, message.OriginCity, message.DestinationCity, result.Value.Id);
    }
}
