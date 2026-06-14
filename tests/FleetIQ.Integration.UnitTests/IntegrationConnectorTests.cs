using FleetIQ.Integration.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Integration.UnitTests;

public class IntegrationConnectorTests
{
    private static IntegrationConnector NewAggregate() => IntegrationConnector.Create("SystemName", "Endpoint");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(IntegrationConnectorStatus.Registered);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Activate_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Activate();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(IntegrationConnectorStatus.Active);
    }

    [Fact]
    public void Activate_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Activate();
        var second = entity.Activate();
        second.IsFailure.ShouldBeTrue();
    }
}
