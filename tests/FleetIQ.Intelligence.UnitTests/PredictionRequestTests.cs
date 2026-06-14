using FleetIQ.Intelligence.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Intelligence.UnitTests;

public class PredictionRequestTests
{
    private static PredictionRequest NewAggregate() => PredictionRequest.Create("ModelId", "PredictionType");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(PredictionRequestStatus.Requested);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Complete_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Complete();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(PredictionRequestStatus.Completed);
    }

    [Fact]
    public void Complete_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Complete();
        var second = entity.Complete();
        second.IsFailure.ShouldBeTrue();
    }
}
