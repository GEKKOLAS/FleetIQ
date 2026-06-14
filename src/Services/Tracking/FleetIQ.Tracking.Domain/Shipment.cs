using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Tracking.Domain;

// NOTE: High event volume: add SignalR push, GeoPosition value objects, milestones, exceptions and proof-of-delivery.
public enum ShipmentStatus { Dispatched = 0, InTransit = 1, Delivered = 2 }

public static class ShipmentErrors
{
    public static readonly Error InvalidTransition =
        new("Shipment.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Shipment aggregate root.</summary>
public sealed class Shipment : AggregateRoot<Guid>
{
    public Guid LoadId { get; private set; }
    public Guid CarrierId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Shipment() { } // EF

    private Shipment(Guid id, Guid loadId, Guid carrierId) : base(id)
    {
        LoadId = loadId;
        CarrierId = carrierId;
        Status = ShipmentStatus.Dispatched;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Shipment Create(Guid loadId, Guid carrierId)
    {
        var entity = new Shipment(Guid.NewGuid(), loadId, carrierId);
        entity.Raise(new Events.ShipmentCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Deliver()
    {
        if (Status != ShipmentStatus.Dispatched) return Result.Failure(ShipmentErrors.InvalidTransition);
        Status = ShipmentStatus.Delivered;
        Raise(new Events.ShipmentStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
