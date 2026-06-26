using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Tracking.Domain.Events;

namespace FleetIQ.Tracking.Domain;

/// <summary>Shipment aggregate root. Tracks the live position (primitive columns, updated frequently)
/// and a timeline of milestone TrackingEvents (regular child entities).</summary>
public sealed class Shipment : AggregateRoot<Guid>
{
    private readonly List<TrackingEvent> _events = new();

    public Guid LoadId { get; private set; }
    public Guid CarrierId { get; private set; }
    public ShipmentStatus Status { get; private set; }

    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }
    public double? CurrentSpeedKph { get; private set; }
    public DateTime? LastPositionAtUtc { get; private set; }

    public string? ReceivedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? DispatchedOnUtc { get; private set; }
    public DateTime? DeliveredOnUtc { get; private set; }

    public IReadOnlyList<TrackingEvent> Events => _events.AsReadOnly();

    private Shipment() { }

    private Shipment(Guid id, Guid loadId, Guid carrierId) : base(id)
    {
        LoadId = loadId; CarrierId = carrierId; Status = ShipmentStatus.Created; CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Shipment> Create(Guid loadId, Guid carrierId)
    {
        if (loadId == Guid.Empty || carrierId == Guid.Empty) return Result.Failure<Shipment>(ShipmentErrors.NotFound);
        var shipment = new Shipment(Guid.NewGuid(), loadId, carrierId);
        shipment._events.Add(new TrackingEvent(Guid.NewGuid(), TrackingEventType.Created, "Shipment created", null, null));
        shipment.Raise(new ShipmentCreatedDomainEvent(shipment.Id, loadId, carrierId));
        return Result.Success(shipment);
    }

    public Result Dispatch()
    {
        if (Status != ShipmentStatus.Created) return Result.Failure(ShipmentErrors.NotCreated);
        Status = ShipmentStatus.Dispatched;
        DispatchedOnUtc = DateTime.UtcNow;
        _events.Add(new TrackingEvent(Guid.NewGuid(), TrackingEventType.Dispatched, "Dispatched", null, null));
        Raise(new ShipmentDispatchedDomainEvent(Id));
        return Result.Success();
    }

    public Result RecordLocation(double latitude, double longitude, double? speedKph)
    {
        if (Status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit)) return Result.Failure(ShipmentErrors.NotMoving);
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) return Result.Failure(ShipmentErrors.InvalidCoordinates);

        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        CurrentSpeedKph = speedKph;
        LastPositionAtUtc = DateTime.UtcNow;
        if (Status == ShipmentStatus.Dispatched) Status = ShipmentStatus.InTransit;

        Raise(new ShipmentLocationRecordedDomainEvent(Id, latitude, longitude, speedKph));
        return Result.Success();
    }

    public Result ReportException(string note)
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled) return Result.Failure(ShipmentErrors.AlreadyFinished);
        _events.Add(new TrackingEvent(Guid.NewGuid(), TrackingEventType.Exception, note, CurrentLatitude, CurrentLongitude));
        Raise(new ShipmentExceptionRaisedDomainEvent(Id, note));
        return Result.Success();
    }

    public Result Deliver(string receivedBy)
    {
        if (Status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit)) return Result.Failure(ShipmentErrors.NotDeliverable);
        Status = ShipmentStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        ReceivedBy = receivedBy;
        _events.Add(new TrackingEvent(Guid.NewGuid(), TrackingEventType.Delivered, $"Delivered to {receivedBy}", CurrentLatitude, CurrentLongitude));
        Raise(new ShipmentDeliveredDomainEvent(Id, receivedBy));
        return Result.Success();
    }
}
