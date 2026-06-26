using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FleetIQ.Tracking.Api.Hubs;

/// <summary>Clients subscribe here to receive live "ShipmentLocation" messages.</summary>
[Authorize]
public sealed class TrackingHub : Hub
{
}
