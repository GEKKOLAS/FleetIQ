using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Carriers.Domain.Events;

namespace FleetIQ.Carriers.Domain;

public enum CarrierStatus { Onboarding = 0, Verified = 1, Suspended = 2 }

public static class CarrierErrors
{
    public static readonly Error NotFound = new("Carrier.NotFound", "Carrier not found.");
    public static readonly Error NameRequired = new("Carrier.NameRequired", "Legal name, registration number and country are required.");
    public static readonly Error NotOnboarding = new("Carrier.NotOnboarding", "Only a carrier in Onboarding can be verified.");
    public static readonly Error NeedsFleet = new("Carrier.NeedsFleet", "A carrier needs at least one vehicle and one driver before it can be verified.");
    public static readonly Error NotVerified = new("Carrier.NotVerified", "Only a verified carrier can be suspended.");
    public static readonly Error NotSuspended = new("Carrier.NotSuspended", "Only a suspended carrier can be reactivated.");
    public static readonly Error Finished = new("Carrier.Suspended", "The carrier is suspended; modify it after reactivating.");
    public static readonly Error InvalidCapacity = new("Carrier.InvalidCapacity", "Vehicle capacity must be greater than zero.");
    public static readonly Error VehicleRequired = new("Carrier.VehicleRequired", "Plate and type are required.");
    public static readonly Error DriverRequired = new("Carrier.DriverRequired", "Driver name and licence are required.");
}

/// <summary>Carrier aggregate root: a transport company with its fleet (Vehicles) and staff (Drivers).
/// Lifecycle: Onboarding -> Verified -> Suspended (and back via Reactivate). Only a Verified carrier
/// can be assigned loads.</summary>
public sealed class Carrier : AggregateRoot<Guid>
{
    private readonly List<Vehicle> _vehicles = new();
    private readonly List<Driver> _drivers = new();

    public string LegalName { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public CarrierStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? VerifiedOnUtc { get; private set; }
    public DateTime? SuspendedOnUtc { get; private set; }

    public IReadOnlyList<Vehicle> Vehicles => _vehicles.AsReadOnly();
    public IReadOnlyList<Driver> Drivers => _drivers.AsReadOnly();

    public decimal TotalCapacityKg => _vehicles.Sum(v => v.CapacityKg);
    public int FleetSize => _vehicles.Count;
    public int DriverCount => _drivers.Count;
    public bool CanAcceptLoads => Status == CarrierStatus.Verified;

    private Carrier() { } // EF

    private Carrier(Guid id, string legalName, string registrationNumber, string country) : base(id)
    {
        LegalName = legalName;
        RegistrationNumber = registrationNumber;
        Country = country;
        Status = CarrierStatus.Onboarding;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<Carrier> Create(string legalName, string registrationNumber, string country)
    {
        if (string.IsNullOrWhiteSpace(legalName) || string.IsNullOrWhiteSpace(registrationNumber) || string.IsNullOrWhiteSpace(country))
            return Result.Failure<Carrier>(CarrierErrors.NameRequired);

        var carrier = new Carrier(Guid.NewGuid(), legalName.Trim(), registrationNumber.Trim(), country.Trim());
        carrier.Raise(new CarrierCreatedDomainEvent(carrier.Id, carrier.LegalName));
        return Result.Success(carrier);
    }

    public Result AddVehicle(string plate, VehicleType type, decimal capacityKg)
    {
        if (Status == CarrierStatus.Suspended) return Result.Failure(CarrierErrors.Finished);
        if (string.IsNullOrWhiteSpace(plate)) return Result.Failure(CarrierErrors.VehicleRequired);
        if (capacityKg <= 0) return Result.Failure(CarrierErrors.InvalidCapacity);

        _vehicles.Add(new Vehicle(Guid.NewGuid(), plate.Trim().ToUpperInvariant(), type, capacityKg));
        Raise(new VehicleAddedDomainEvent(Id, plate));
        return Result.Success();
    }

    public Result AddDriver(string fullName, string licenceNumber)
    {
        if (Status == CarrierStatus.Suspended) return Result.Failure(CarrierErrors.Finished);
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(licenceNumber)) return Result.Failure(CarrierErrors.DriverRequired);

        _drivers.Add(new Driver(Guid.NewGuid(), fullName.Trim(), licenceNumber.Trim().ToUpperInvariant()));
        Raise(new DriverAddedDomainEvent(Id, fullName));
        return Result.Success();
    }

    public Result Verify()
    {
        if (Status != CarrierStatus.Onboarding) return Result.Failure(CarrierErrors.NotOnboarding);
        if (_vehicles.Count == 0 || _drivers.Count == 0) return Result.Failure(CarrierErrors.NeedsFleet);

        Status = CarrierStatus.Verified;
        VerifiedOnUtc = DateTime.UtcNow;
        Raise(new CarrierVerifiedDomainEvent(Id, LegalName));
        return Result.Success();
    }

    public Result Suspend(string reason)
    {
        if (Status != CarrierStatus.Verified) return Result.Failure(CarrierErrors.NotVerified);
        Status = CarrierStatus.Suspended;
        SuspendedOnUtc = DateTime.UtcNow;
        Raise(new CarrierSuspendedDomainEvent(Id, reason));
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != CarrierStatus.Suspended) return Result.Failure(CarrierErrors.NotSuspended);
        Status = CarrierStatus.Verified;
        SuspendedOnUtc = null;
        Raise(new CarrierVerifiedDomainEvent(Id, LegalName)); // reactivation makes it assignable again
        return Result.Success();
    }
}
