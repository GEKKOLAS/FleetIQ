using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Carriers.Domain;

// NOTE: Add Vehicle and Driver as separate aggregates and KYC/insurance value objects per the conceptual map.
public enum CarrierStatus { Onboarding = 0, Verified = 1, Suspended = 2 }

public static class CarrierErrors
{
    public static readonly Error InvalidTransition =
        new("Carrier.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>Carrier aggregate root.</summary>
public sealed class Carrier : AggregateRoot<Guid>
{
    public string LegalName { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public CarrierStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private Carrier() { } // EF

    private Carrier(Guid id, string legalName, string registrationNumber, string country) : base(id)
    {
        LegalName = legalName;
        RegistrationNumber = registrationNumber;
        Country = country;
        Status = CarrierStatus.Onboarding;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Carrier Create(string legalName, string registrationNumber, string country)
    {
        var entity = new Carrier(Guid.NewGuid(), legalName, registrationNumber, country);
        entity.Raise(new Events.CarrierCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Verify()
    {
        if (Status != CarrierStatus.Onboarding) return Result.Failure(CarrierErrors.InvalidTransition);
        Status = CarrierStatus.Verified;
        Raise(new Events.CarrierStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
