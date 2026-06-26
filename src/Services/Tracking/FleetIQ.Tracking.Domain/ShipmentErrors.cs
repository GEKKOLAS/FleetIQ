using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Tracking.Domain;

public static class ShipmentErrors
{
    public static readonly Error NotFound = new("Shipment.NotFound", "The shipment was not found.");
    public static readonly Error NotCreated = new("Shipment.NotCreated", "Only a newly created shipment can be dispatched.");
    public static readonly Error NotMoving = new("Shipment.NotMoving", "Locations can only be recorded for a dispatched or in-transit shipment.");
    public static readonly Error NotDeliverable = new("Shipment.NotDeliverable", "Only a dispatched or in-transit shipment can be delivered.");
    public static readonly Error AlreadyFinished = new("Shipment.AlreadyFinished", "The shipment is already delivered or cancelled.");
    public static readonly Error InvalidCoordinates = new("Shipment.InvalidCoordinates", "Latitude must be -90..90 and longitude -180..180.");
}
