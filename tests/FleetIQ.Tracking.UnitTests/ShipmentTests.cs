using FleetIQ.Tracking.Domain;
using FleetIQ.Tracking.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Tracking.UnitTests;

public class ShipmentTests
{
    private static Shipment New() => Shipment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;

    [Fact]
    public void Create_RaisesCreatedEvent()
    {
        var result = Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ShipmentStatus.Created);
        result.Value.DomainEvents.OfType<ShipmentCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Dispatch_ThenRecordLocation_GoesInTransit()
    {
        var s = New();
        s.Dispatch().IsSuccess.ShouldBeTrue();
        s.Status.ShouldBe(ShipmentStatus.Dispatched);

        s.RecordLocation(38.72, -9.14, 50).IsSuccess.ShouldBeTrue();
        s.Status.ShouldBe(ShipmentStatus.InTransit);
        s.CurrentLatitude.ShouldBe(38.72);
    }

    [Fact]
    public void RecordLocation_BeforeDispatch_Fails()
    {
        var s = New();
        var r = s.RecordLocation(38.72, -9.14, 50);
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(ShipmentErrors.NotMoving);
    }

    [Fact]
    public void Deliver_AfterDispatch_Succeeds()
    {
        var s = New();
        s.Dispatch();
        s.Deliver("Front desk").IsSuccess.ShouldBeTrue();
        s.Status.ShouldBe(ShipmentStatus.Delivered);
        s.ReceivedBy.ShouldBe("Front desk");
        s.DomainEvents.OfType<ShipmentDeliveredDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Deliver_BeforeDispatch_Fails()
    {
        var s = New();
        var r = s.Deliver("x");
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe(ShipmentErrors.NotDeliverable);
    }
}
