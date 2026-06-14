using FleetIQ.Carriers.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Carriers.UnitTests;

public class CarrierTests
{
    private static Carrier NewAggregate() => Carrier.Create("LegalName", "RegistrationNumber", "Country");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(CarrierStatus.Onboarding);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Verify_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Verify();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(CarrierStatus.Verified);
    }

    [Fact]
    public void Verify_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Verify();
        var second = entity.Verify();
        second.IsFailure.ShouldBeTrue();
    }
}
