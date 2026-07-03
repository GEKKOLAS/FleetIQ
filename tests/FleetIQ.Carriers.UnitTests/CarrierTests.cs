using FleetIQ.Carriers.Domain;
using FleetIQ.Carriers.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Carriers.UnitTests;

public class CarrierTests
{
    private static Carrier NewOnboarding() => Carrier.Create("Demo Freight", "ES-B1", "ES").Value;

    private static Carrier NewReadyToVerify()
    {
        var c = NewOnboarding();
        c.AddVehicle("1234-ABC", VehicleType.Truck, 24000m);
        c.AddDriver("Ana Costa", "ES-D-1");
        return c;
    }

    [Fact]
    public void Create_SetsOnboarding_AndRaisesEvent()
    {
        var result = Carrier.Create("Demo Freight", "ES-B1", "ES");
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CarrierStatus.Onboarding);
        result.Value.DomainEvents.OfType<CarrierCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_WithEmptyName_Fails()
    {
        Carrier.Create("", "ES-B1", "ES").Error.ShouldBe(CarrierErrors.NameRequired);
    }

    [Fact]
    public void Verify_WithoutFleet_Fails()
    {
        NewOnboarding().Verify().Error.ShouldBe(CarrierErrors.NeedsFleet);
    }

    [Fact]
    public void Verify_WithFleet_Succeeds_AndComputesCapacity()
    {
        var c = NewReadyToVerify();
        c.Verify().IsSuccess.ShouldBeTrue();
        c.Status.ShouldBe(CarrierStatus.Verified);
        c.CanAcceptLoads.ShouldBeTrue();
        c.TotalCapacityKg.ShouldBe(24000m);
        c.FleetSize.ShouldBe(1);
        c.DriverCount.ShouldBe(1);
        c.DomainEvents.OfType<CarrierVerifiedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Verify_Twice_Fails()
    {
        var c = NewReadyToVerify();
        c.Verify();
        c.Verify().Error.ShouldBe(CarrierErrors.NotOnboarding);
    }

    [Fact]
    public void Suspend_ThenReactivate_TogglesAssignable()
    {
        var c = NewReadyToVerify();
        c.Verify();

        c.Suspend("docs expired").IsSuccess.ShouldBeTrue();
        c.Status.ShouldBe(CarrierStatus.Suspended);
        c.CanAcceptLoads.ShouldBeFalse();

        c.Reactivate().IsSuccess.ShouldBeTrue();
        c.Status.ShouldBe(CarrierStatus.Verified);
        c.CanAcceptLoads.ShouldBeTrue();
    }
}
