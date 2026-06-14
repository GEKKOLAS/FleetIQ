using FleetIQ.Analytics.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Analytics.UnitTests;

public class ReportDefinitionTests
{
    private static ReportDefinition NewAggregate() => ReportDefinition.Create("Name", "Query");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(ReportDefinitionStatus.Active);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Archive_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Archive();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ReportDefinitionStatus.Archived);
    }

    [Fact]
    public void Archive_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Archive();
        var second = entity.Archive();
        second.IsFailure.ShouldBeTrue();
    }
}
