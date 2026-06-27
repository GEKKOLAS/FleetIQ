using FleetIQ.Contracts;
using FleetIQ.Tracking.Application.Routing;
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

        // Resolve the load's cities to coordinates so the shipment plots a real road route.
        var hasO = CityGeocoder.TryGet(message.OriginCity, out var oc);
        var hasD = CityGeocoder.TryGet(message.DestinationCity, out var dc);
        if (!hasO || !hasD)
        {
            var t = CityGeocoder.RandomTrip();   // unknown city names -> fall back to a random real trip
            if (!hasO) { oc = (t.OLat, t.OLng); }
            if (!hasD) { dc = (t.DLat, t.DLng); }
        }

        var result = Shipment.Create(message.LoadId, message.CarrierId,
            message.OriginCity, message.DestinationCity, oc.Lat, oc.Lng, dc.Lat, dc.Lng);
        if (result.IsFailure)
        {
            logger.LogWarning("[Tracking] Could not create shipment for load {LoadId}: {Error}", message.LoadId, result.Error.Code);
            return;
        }

        var shipment = result.Value;
        // A confirmed load already has a carrier and a finalized route, so dispatch it right away:
        // the shipment goes active and the simulator starts moving it on the live map automatically.
        shipment.Dispatch();

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "[Tracking] Load {LoadId} confirmed ({Origin} -> {Destination}) -> shipment {ShipmentId} created and dispatched",
            message.LoadId, message.OriginCity, message.DestinationCity, shipment.Id);
    }
}
