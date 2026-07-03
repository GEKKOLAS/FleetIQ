using FleetIQ.Analytics.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Analytics.UnitTests;

public class ReportDefinitionTests
{
    private static ReportDefinition NewAggregate() =>
        ReportDefinition.Create("On-time delivery", "SELECT 1", "Weekly").Value;

    [Fact]
    public void Create_SetsInitialStatus_AndNormalizesSchedule()
    {
        var entity = ReportDefinition.Create("R", "Q", "weekly").Value;
        entity.Status.ShouldBe(ReportDefinitionStatus.Active);
        entity.Schedule.ShouldBe("Weekly");
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_WithUnknownSchedule_Fails()
    {
        ReportDefinition.Create("R", "Q", "Hourly").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void StartRun_ThenComplete_UpdatesLastRun()
    {
        var entity = NewAggregate();
        var run = entity.StartRun();
        run.IsSuccess.ShouldBeTrue();

        entity.CompleteRun(run.Value, 42).IsSuccess.ShouldBeTrue();
        entity.Runs.Count.ShouldBe(1);
        entity.Runs[0].Succeeded.ShouldBe(true);
        entity.Runs[0].RowCount.ShouldBe(42);
        entity.LastRunOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void StartRun_WhileAnotherInProgress_Fails()
    {
        var entity = NewAggregate();
        entity.StartRun().IsSuccess.ShouldBeTrue();
        var second = entity.StartRun();
        second.IsFailure.ShouldBeTrue();
        second.Error.ShouldBe(ReportDefinitionErrors.RunInProgress);
    }

    [Fact]
    public void FailRun_KeepsError()
    {
        var entity = NewAggregate();
        var run = entity.StartRun();
        entity.FailRun(run.Value, "Timeout").IsSuccess.ShouldBeTrue();
        entity.Runs[0].Succeeded.ShouldBe(false);
        entity.Runs[0].Error.ShouldBe("Timeout");
    }

    [Fact]
    public void CompleteRun_Twice_Fails()
    {
        var entity = NewAggregate();
        var run = entity.StartRun();
        entity.CompleteRun(run.Value, 1);
        entity.CompleteRun(run.Value, 1).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Archive_BlocksRuns_AndRestore_Reactivates()
    {
        var entity = NewAggregate();
        entity.Archive().IsSuccess.ShouldBeTrue();
        entity.StartRun().IsFailure.ShouldBeTrue();
        entity.UpdateQuery("SELECT 2").IsFailure.ShouldBeTrue();

        entity.Restore().IsSuccess.ShouldBeTrue();
        entity.StartRun().IsSuccess.ShouldBeTrue();
    }
}
