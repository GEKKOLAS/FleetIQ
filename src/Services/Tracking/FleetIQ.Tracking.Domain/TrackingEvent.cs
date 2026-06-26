using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Tracking.Domain;

public enum TrackingEventType { Created = 0, Dispatched = 1, Milestone = 2, Exception = 3, Delivered = 4 }

/// <summary>A milestone/exception in the shipment timeline. Regular child entity (own table + FK).
/// Frequent location pings update the aggregate's current position instead of adding rows here.</summary>
public sealed class TrackingEvent : Entity<Guid>
{
    public TrackingEventType Type { get; private set; }
    public string? Note { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    internal TrackingEvent(Guid id, TrackingEventType type, string? note, double? lat, double? lng) : base(id)
    {
        Type = type; Note = note; Latitude = lat; Longitude = lng; OccurredOnUtc = DateTime.UtcNow;
    }

    private TrackingEvent() { } // EF: materialized row, IsTransient = false
}
