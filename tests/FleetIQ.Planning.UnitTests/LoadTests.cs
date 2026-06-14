using FleetIQ.Planning.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Planning.UnitTests;

public class LoadTests
{
    private static Load NewAggregate() => Load.Create(Guid.NewGuid(), "OriginCity", "DestinationCity", 10m);

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(LoadStatus.Planned);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Optimize_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Optimize();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(LoadStatus.Optimized);
    }

    [Fact]
    public void Optimize_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Optimize();
        var second = entity.Optimize();
        second.IsFailure.ShouldBeTrue();
    }
}
