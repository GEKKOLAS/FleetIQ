using FleetIQ.Planning.Domain;
using FleetIQ.Planning.Domain.Events;
using Shouldly;
using Xunit;

namespace FleetIQ.Planning.UnitTests;

public class RouteTests
{
    private static Route New() => Route.Create(Guid.NewGuid()).Value;

    [Fact]
    public void Create_SetsDraft_AndRaisesEvent()
    {
        var result = Route.Create(Guid.NewGuid());
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(RouteStatus.Draft);
        result.Value.DomainEvents.OfType<RouteCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AddStop_IncrementsSequence()
    {
        var route = New();
        route.AddStop(StopKind.Pickup, "Lisboa", "Rua A", 38.72, -9.14);
        route.AddStop(StopKind.Delivery, "Porto", "Rua B", 41.15, -8.61);

        route.Stops.Count.ShouldBe(2);
        route.Stops[0].Sequence.ShouldBe(1);
        route.Stops[1].Sequence.ShouldBe(2);
        route.Stops[1].Kind.ShouldBe(StopKind.Delivery);
    }

    [Fact]
    public void AddStop_WithInvalidCoordinates_Fails()
    {
        var route = New();
        route.AddStop(StopKind.Pickup, "X", "Y", 200, 0).Error.ShouldBe(RouteErrors.InvalidCoordinates);
    }

    [Fact]
    public void Finalize_WithTooFewStops_Fails()
    {
        var route = New();
        route.AddStop(StopKind.Pickup, "Lisboa", "Rua A", 38.72, -9.14);
        route.Finalize().Error.ShouldBe(RouteErrors.TooFewStops);
    }

    [Fact]
    public void Finalize_WithTwoStops_Succeeds()
    {
        var route = New();
        route.AddStop(StopKind.Pickup, "Lisboa", "Rua A", 38.72, -9.14);
        route.AddStop(StopKind.Delivery, "Porto", "Rua B", 41.15, -8.61);

        route.Finalize().IsSuccess.ShouldBeTrue();
        route.Status.ShouldBe(RouteStatus.Finalized);
        route.FinalizedOnUtc.ShouldNotBeNull();
        route.DomainEvents.OfType<RouteFinalizedDomainEvent>().ShouldHaveSingleItem();
    }
}
