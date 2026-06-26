using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Application.Abstractions;

namespace FleetIQ.Planning.Application.GetRoute;

public sealed record GetRouteQuery(Guid Id) : IQuery<RouteDetailResponse?>;

public sealed record RouteStopResponse(int Sequence, string Kind, string City, string Address, double Latitude, double Longitude);

public sealed record RouteDetailResponse(
    Guid Id, Guid LoadId, string Status, DateTime CreatedOnUtc, DateTime? FinalizedOnUtc,
    IReadOnlyList<RouteStopResponse> Stops);

public sealed class GetRouteQueryHandler(IRouteRepository repository)
    : IQueryHandler<GetRouteQuery, RouteDetailResponse?>
{
    public async Task<RouteDetailResponse?> Handle(GetRouteQuery query, CancellationToken ct)
    {
        var route = await repository.GetByIdAsync(query.Id, ct);
        if (route is null) return null;

        return new RouteDetailResponse(
            route.Id, route.LoadId, route.Status.ToString(), route.CreatedOnUtc, route.FinalizedOnUtc,
            route.Stops.Select(s => new RouteStopResponse(s.Sequence, s.Kind.ToString(), s.City, s.Address, s.Latitude, s.Longitude)).ToList());
    }
}
