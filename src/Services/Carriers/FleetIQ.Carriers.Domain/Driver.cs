using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Carriers.Domain;

public enum DriverStatus { Available = 0, OnDuty = 1, Off = 2 }

/// <summary>A driver employed by a carrier. Regular child entity of the Carrier aggregate.</summary>
public sealed class Driver : Entity<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string LicenceNumber { get; private set; } = string.Empty;
    public DriverStatus Status { get; private set; }

    internal Driver(Guid id, string fullName, string licenceNumber) : base(id)
    {
        FullName = fullName;
        LicenceNumber = licenceNumber;
        Status = DriverStatus.Available;
    }

    private Driver() { } // EF: materialized row, IsTransient = false
}
