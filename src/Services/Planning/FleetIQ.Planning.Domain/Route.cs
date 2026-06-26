using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Domain.Events;

namespace FleetIQ.Planning.Domain;

public enum RouteStatus { Draft = 0, Finalized = 1 }

public static class RouteErrors
{
    public static readonly Error NotFound = new("Route.NotFound", "Route not found.");
    public static readonly Error NotDraft = new("Route.NotDraft", "The route can only be modified while in Draft.");
    public static readonly Error TooFewStops = new("Route.TooFewStops", "A route needs at least two stops to be finalized.");
    public static readonly Error InvalidCoordinates = new("Route.InvalidCoordinates", "Latitude/longitude out of range.");
    public static readonly Error LoadRequired = new("Route.LoadRequired", "A route must belong to a load.");
    public static readonly Error NotFinalized = new("Route.NotFinalized", "The route must be finalized before it can be attached.");
    public static readonly Error WrongLoad = new("Route.WrongLoad", "The route belongs to a different load.");
}

/// <summary>Route aggregate root: the ordered sequence of stops for a Load. Sibling of Load,
/// linked by LoadId. Lifecycle Draft -> Finalized.</summary>
public sealed class Route : AggregateRoot<Guid>
{
    private readonly List<RouteStop> _stops = new();

    public Guid LoadId { get; private set; }
    public RouteStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? FinalizedOnUtc { get; private set; }

    public IReadOnlyList<RouteStop> Stops => _stops.OrderBy(s => s.Sequence).ToList().AsReadOnly();

    private Route() { } // EF

    private Route(Guid id, Guid loadId) : base(id)
    {
        LoadId = loadId;
        Status = RouteStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Route> Create(Guid loadId)
    {
        if (loadId == Guid.Empty) return Result.Failure<Route>(RouteErrors.LoadRequired);
        var route = new Route(Guid.NewGuid(), loadId);
        route.Raise(new RouteCreatedDomainEvent(route.Id, loadId));
        return Result.Success(route);
    }

    public Result AddStop(StopKind kind, string city, string address, double latitude, double longitude)
    {
        if (Status != RouteStatus.Draft) return Result.Failure(RouteErrors.NotDraft);
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) return Result.Failure(RouteErrors.InvalidCoordinates);

        var sequence = _stops.Count + 1;
        _stops.Add(new RouteStop(Guid.NewGuid(), sequence, kind, city?.Trim() ?? string.Empty, address?.Trim() ?? string.Empty, latitude, longitude));
        Raise(new RouteStopAddedDomainEvent(Id, sequence));
        return Result.Success();
    }

    public Result Finalize()
    {
        if (Status != RouteStatus.Draft) return Result.Failure(RouteErrors.NotDraft);
        if (_stops.Count < 2) return Result.Failure(RouteErrors.TooFewStops);
        Status = RouteStatus.Finalized;
        FinalizedOnUtc = DateTime.UtcNow;
        Raise(new RouteFinalizedDomainEvent(Id, LoadId));
        return Result.Success();
    }
}
