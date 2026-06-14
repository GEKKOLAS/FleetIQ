using FleetIQ.Tracking.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Tracking.UnitTests;

public class ShipmentTests
{
    private static Shipment NewAggregate() => Shipment.Create(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(ShipmentStatus.Dispatched);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Deliver_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Deliver();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ShipmentStatus.Delivered);
    }

    [Fact]
    public void Deliver_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Deliver();
        var second = entity.Deliver();
        second.IsFailure.ShouldBeTrue();
    }
}
