namespace FleetIQ.Tracking.Application.Abstractions;

/// <summary>Pushes live positions to subscribed clients (implemented with SignalR in the API layer).</summary>
public interface IShipmentLocationBroadcaster
{
    Task BroadcastAsync(Guid shipmentId, double latitude, double longitude, double? speedKph, string status, DateTime atUtc, CancellationToken ct);
}
