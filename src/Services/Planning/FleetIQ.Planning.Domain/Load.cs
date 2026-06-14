using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain;

// NOTE: Real implementation integrates Google OR-Tools and a routing engine (OSRM/Valhalla); Route and TransportPlan are sibling aggregates.
public enum LoadStatus { Planned = 0, Optimized = 1, Assigned = 2 }

public static class LoadErrors
{
    public static readonly Error InvalidTransition =
        new("Load.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Load aggregate root.</summary>
public sealed class Load : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public string OriginCity { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; } = 0m;
    public LoadStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Load() { } // EF

    private Load(Guid id, Guid orderId, string originCity, string destinationCity, decimal weightKg) : base(id)
    {
        OrderId = orderId;
        OriginCity = originCity;
        DestinationCity = destinationCity;
        WeightKg = weightKg;
        Status = LoadStatus.Planned;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Load Create(Guid orderId, string originCity, string destinationCity, decimal weightKg)
    {
        var entity = new Load(Guid.NewGuid(), orderId, originCity, destinationCity, weightKg);
        entity.Raise(new Events.LoadCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Optimize()
    {
        if (Status != LoadStatus.Planned) return Result.Failure(LoadErrors.InvalidTransition);
        Status = LoadStatus.Optimized;
        Raise(new Events.LoadStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
