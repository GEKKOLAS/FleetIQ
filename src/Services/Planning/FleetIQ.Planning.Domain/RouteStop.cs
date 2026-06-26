using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Planning.Domain;

public enum StopKind { Pickup = 0, Delivery = 1 }

/// <summary>A single stop in a Route. Regular child entity (own table + FK to Route).</summary>
public sealed class RouteStop : Entity<Guid>
{
    public int Sequence { get; private set; }
    public StopKind Kind { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    internal RouteStop(Guid id, int sequence, StopKind kind, string city, string address, double latitude, double longitude) : base(id)
    {
        Sequence = sequence;
        Kind = kind;
        City = city;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
    }

    private RouteStop() { } // EF: materialized row, IsTransient = false
}
