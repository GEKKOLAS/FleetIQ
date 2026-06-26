using FleetIQ.Tracking.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace FleetIQ.Tracking.Api.Hubs;

public sealed class ShipmentLocationBroadcaster(IHubContext<TrackingHub> hub) : IShipmentLocationBroadcaster
{
    public Task BroadcastAsync(Guid shipmentId, double latitude, double longitude, double? speedKph, string status, DateTime atUtc, CancellationToken ct) =>
        hub.Clients.All.SendAsync("ShipmentLocation",
            new { shipmentId, latitude, longitude, speedKph, status, atUtc }, ct);
}
