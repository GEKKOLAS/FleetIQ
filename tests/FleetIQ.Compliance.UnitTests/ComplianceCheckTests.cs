using FleetIQ.Compliance.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Compliance.UnitTests;

public class ComplianceCheckTests
{
    private static ComplianceCheck NewAggregate() => ComplianceCheck.Create(Guid.NewGuid(), "RuleCode", "Jurisdiction");

    [Fact]
    public void Create_SetsInitialStatus_AndRaisesCreatedEvent()
    {
        var entity = NewAggregate();
        entity.Status.ShouldBe(ComplianceCheckStatus.Pending);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Pass_FromInitialState_Succeeds()
    {
        var entity = NewAggregate();
        var result = entity.Pass();
        result.IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ComplianceCheckStatus.Passed);
    }

    [Fact]
    public void Pass_Twice_Fails()
    {
        var entity = NewAggregate();
        entity.Pass();
        var second = entity.Pass();
        second.IsFailure.ShouldBeTrue();
    }
}
