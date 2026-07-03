using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Carriers.Domain;

public enum VehicleType { Van = 0, Truck = 1, Trailer = 2, Refrigerated = 3 }
public enum VehicleStatus { Available = 0, InUse = 1, Maintenance = 2 }

/// <summary>A vehicle in a carrier's fleet. Regular child entity of the Carrier aggregate.</summary>
public sealed class Vehicle : Entity<Guid>
{
    public string Plate { get; private set; } = string.Empty;
    public VehicleType Type { get; private set; }
    public decimal CapacityKg { get; private set; }
    public VehicleStatus Status { get; private set; }

    internal Vehicle(Guid id, string plate, VehicleType type, decimal capacityKg) : base(id)
    {
        Plate = plate;
        Type = type;
        CapacityKg = capacityKg;
        Status = VehicleStatus.Available;
    }

    private Vehicle() { } // EF: materialized row, IsTransient = false
}
