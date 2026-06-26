using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Domain.Events;

namespace FleetIQ.Planning.Domain;

public enum LoadStatus { Draft = 0, Planned = 1, Confirmed = 2, Cancelled = 3 }

public static class LoadErrors
{
    public static readonly Error NotFound = new("Load.NotFound", "Load not found.");
    public static readonly Error NotDraft = new("Load.NotDraft", "The load can only be modified while in Draft.");
    public static readonly Error NoOrders = new("Load.NoOrders", "The load needs at least one order before it can be planned.");
    public static readonly Error NoCarrier = new("Load.NoCarrier", "Assign a carrier before planning the load.");
    public static readonly Error NoRoute = new("Load.NoRoute", "Attach a finalized route before planning the load.");
    public static readonly Error NotPlanned = new("Load.NotPlanned", "The load must be Planned before it can be confirmed.");
    public static readonly Error AlreadyFinished = new("Load.AlreadyFinished", "The load is already confirmed or cancelled.");
    public static readonly Error InvalidWeight = new("Load.InvalidWeight", "Weight must be zero or greater.");
    public static readonly Error CityRequired = new("Load.CityRequired", "Origin and destination cities are required.");
}

/// <summary>Load aggregate root: what is being moved. Holds the consolidated orders and the
/// lifecycle Draft -> Planned -> Confirmed (or Cancelled). The route (sequence of stops) is a
/// sibling aggregate referenced by id (RouteId); aggregates never hold object references across
/// their boundary.</summary>
public sealed class Load : AggregateRoot<Guid>
{
    private readonly List<LoadOrder> _orders = new();

    public string Reference { get; private set; } = string.Empty;
    public string OriginCity { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public decimal TotalWeightKg { get; private set; }
    public LoadStatus Status { get; private set; }
    public Guid? CarrierId { get; private set; }
    public Guid? RouteId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? PlannedOnUtc { get; private set; }
    public DateTime? ConfirmedOnUtc { get; private set; }
    public DateTime? CancelledOnUtc { get; private set; }

    public IReadOnlyList<LoadOrder> Orders => _orders.AsReadOnly();

    private Load() { } // EF

    private Load(Guid id, string reference, string originCity, string destinationCity) : base(id)
    {
        Reference = reference;
        OriginCity = originCity;
        DestinationCity = destinationCity;
        Status = LoadStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Load> Create(string originCity, string destinationCity)
    {
        if (string.IsNullOrWhiteSpace(originCity) || string.IsNullOrWhiteSpace(destinationCity))
            return Result.Failure<Load>(LoadErrors.CityRequired);

        var reference = $"LD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var load = new Load(Guid.NewGuid(), reference, originCity.Trim(), destinationCity.Trim());
        load.Raise(new LoadCreatedDomainEvent(load.Id, reference));
        return Result.Success(load);
    }

    public Result AddOrder(Guid orderId, string description, decimal weightKg)
    {
        if (Status != LoadStatus.Draft) return Result.Failure(LoadErrors.NotDraft);
        if (orderId == Guid.Empty) return Result.Failure(LoadErrors.NotFound);
        if (weightKg < 0) return Result.Failure(LoadErrors.InvalidWeight);

        _orders.Add(new LoadOrder(Guid.NewGuid(), orderId, description?.Trim() ?? string.Empty, weightKg));
        TotalWeightKg = _orders.Sum(o => o.WeightKg);
        Raise(new LoadOrderAddedDomainEvent(Id, orderId));
        return Result.Success();
    }

    public Result AssignCarrier(Guid carrierId)
    {
        if (Status is LoadStatus.Confirmed or LoadStatus.Cancelled) return Result.Failure(LoadErrors.AlreadyFinished);
        if (carrierId == Guid.Empty) return Result.Failure(LoadErrors.NoCarrier);
        CarrierId = carrierId;
        Raise(new LoadCarrierAssignedDomainEvent(Id, carrierId));
        return Result.Success();
    }

    public Result AttachRoute(Guid routeId)
    {
        if (Status is LoadStatus.Confirmed or LoadStatus.Cancelled) return Result.Failure(LoadErrors.AlreadyFinished);
        if (routeId == Guid.Empty) return Result.Failure(LoadErrors.NoRoute);
        RouteId = routeId;
        return Result.Success();
    }

    public Result Plan()
    {
        if (Status != LoadStatus.Draft) return Result.Failure(LoadErrors.NotDraft);
        if (_orders.Count == 0) return Result.Failure(LoadErrors.NoOrders);
        if (CarrierId is null) return Result.Failure(LoadErrors.NoCarrier);
        if (RouteId is null) return Result.Failure(LoadErrors.NoRoute);

        Status = LoadStatus.Planned;
        PlannedOnUtc = DateTime.UtcNow;
        Raise(new LoadPlannedDomainEvent(Id));
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != LoadStatus.Planned) return Result.Failure(LoadErrors.NotPlanned);
        Status = LoadStatus.Confirmed;
        ConfirmedOnUtc = DateTime.UtcNow;
        Raise(new LoadConfirmedDomainEvent(Id, CarrierId!.Value));
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is LoadStatus.Confirmed or LoadStatus.Cancelled) return Result.Failure(LoadErrors.AlreadyFinished);
        Status = LoadStatus.Cancelled;
        CancelledOnUtc = DateTime.UtcNow;
        Raise(new LoadCancelledDomainEvent(Id));
        return Result.Success();
    }
}
