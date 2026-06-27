using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.ListShipments;

public sealed record ListShipmentsQuery(Guid? CarrierId, string? Status, int Page, int PageSize)
    : IQuery<PagedResult<ShipmentSummaryResponse>>;

public sealed record ShipmentSummaryResponse(
    Guid Id, Guid LoadId, Guid CarrierId, string Status,
    double? CurrentLatitude, double? CurrentLongitude, DateTime? LastPositionAtUtc, DateTime CreatedOnUtc,
    string OriginCity, string DestinationCity,
    double OriginLatitude, double OriginLongitude, double DestinationLatitude, double DestinationLongitude);

public sealed class ListShipmentsQueryHandler(IShipmentRepository repository)
    : IQueryHandler<ListShipmentsQuery, PagedResult<ShipmentSummaryResponse>>
{
    public async Task<PagedResult<ShipmentSummaryResponse>> Handle(ListShipmentsQuery query, CancellationToken ct)
    {
        ShipmentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ShipmentStatus>(query.Status, true, out var parsed))
            status = parsed;

        var paged = await repository.ListAsync(query.CarrierId, status, query.Page, query.PageSize, ct);
        var items = paged.Items
            .Select(s => new ShipmentSummaryResponse(s.Id, s.LoadId, s.CarrierId, s.Status.ToString(),
                s.CurrentLatitude, s.CurrentLongitude, s.LastPositionAtUtc, s.CreatedOnUtc,
                s.OriginCity, s.DestinationCity,
                s.OriginLatitude, s.OriginLongitude, s.DestinationLatitude, s.DestinationLongitude))
            .ToList();
        return new PagedResult<ShipmentSummaryResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
