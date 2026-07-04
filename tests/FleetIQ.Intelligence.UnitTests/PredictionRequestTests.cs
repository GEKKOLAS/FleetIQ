using FleetIQ.Intelligence.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Intelligence.UnitTests;

public class PredictionRequestTests
{
    private static PredictionRequest NewAggregate() => PredictionRequest.Create("eta-v2", "Eta").Value;

    [Fact]
    public void Create_SetsInitialStatus_AndNormalizesType()
    {
        var entity = PredictionRequest.Create("eta-v2", "eta").Value;
        entity.Status.ShouldBe(PredictionRequestStatus.Requested);
        entity.PredictionType.ShouldBe("Eta");
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_WithUnknownType_Fails()
    {
        PredictionRequest.Create("m", "Weather").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Complete_RequiresRunning()
    {
        var entity = NewAggregate();
        entity.Complete("{}", 0.9m).IsFailure.ShouldBeTrue();

        entity.Start().IsSuccess.ShouldBeTrue();
        entity.Complete("""{"eta":"16:30"}""", 0.9m).IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(PredictionRequestStatus.Completed);
        entity.Confidence.ShouldBe(0.9m);
        entity.FinishedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Complete_WithConfidenceOutOfRange_Fails()
    {
        var entity = NewAggregate();
        entity.Start();
        entity.Complete("{}", 1.2m).IsFailure.ShouldBeTrue();
        entity.Complete("{}", -0.1m).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Fail_FromRequestedOrRunning_KeepsError()
    {
        var requested = NewAggregate();
        requested.Fail("model unavailable").IsSuccess.ShouldBeTrue();
        requested.Status.ShouldBe(PredictionRequestStatus.Failed);
        requested.Error.ShouldBe("model unavailable");

        var running = NewAggregate();
        running.Start();
        running.Fail("timeout").IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Fail_AfterCompleted_Fails()
    {
        var entity = NewAggregate();
        entity.Start();
        entity.Complete("{}", 0.5m);
        entity.Fail("late error").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Start_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Start();
        entity.Start().IsFailure.ShouldBeTrue();
    }
}
