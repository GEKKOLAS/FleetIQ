using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Application.Abstractions;

namespace FleetIQ.Tracking.Application.GetShipment;

public sealed record GetShipmentQuery(Guid Id) : IQuery<ShipmentDetailResponse?>;

public sealed record TrackingEventResponse(string Type, string? Note, double? Latitude, double? Longitude, DateTime OccurredOnUtc);

public sealed record ShipmentDetailResponse(
    Guid Id, Guid LoadId, Guid CarrierId, string Status,
    double? CurrentLatitude, double? CurrentLongitude, double? CurrentSpeedKph, DateTime? LastPositionAtUtc,
    string? ReceivedBy, DateTime CreatedOnUtc, DateTime? DispatchedOnUtc, DateTime? DeliveredOnUtc,
    IReadOnlyList<TrackingEventResponse> Events);

public sealed class GetShipmentQueryHandler(IShipmentRepository repository)
    : IQueryHandler<GetShipmentQuery, ShipmentDetailResponse?>
{
    public async Task<ShipmentDetailResponse?> Handle(GetShipmentQuery query, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(query.Id, ct);
        if (s is null) return null;
        return new ShipmentDetailResponse(
            s.Id, s.LoadId, s.CarrierId, s.Status.ToString(),
            s.CurrentLatitude, s.CurrentLongitude, s.CurrentSpeedKph, s.LastPositionAtUtc,
            s.ReceivedBy, s.CreatedOnUtc, s.DispatchedOnUtc, s.DeliveredOnUtc,
            s.Events.OrderBy(e => e.OccurredOnUtc).Select(e => new TrackingEventResponse(e.Type.ToString(), e.Note, e.Latitude, e.Longitude, e.OccurredOnUtc)).ToList());
    }
}
