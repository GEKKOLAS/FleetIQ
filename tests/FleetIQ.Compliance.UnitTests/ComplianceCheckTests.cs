using FleetIQ.Compliance.Domain;
using Shouldly;
using Xunit;

namespace FleetIQ.Compliance.UnitTests;

public class ComplianceCheckTests
{
    private static ComplianceCheck NewAggregate() =>
        ComplianceCheck.Create(Guid.NewGuid(), "Driver", "HOS-11H", "EU").Value;

    [Fact]
    public void Create_SetsInitialStatus_AndNormalizesSubjectType()
    {
        var entity = ComplianceCheck.Create(Guid.NewGuid(), "driver", "HOS-11H", "EU").Value;
        entity.Status.ShouldBe(ComplianceCheckStatus.Pending);
        entity.SubjectType.ShouldBe("Driver");
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_WithUnknownSubjectType_Fails()
    {
        ComplianceCheck.Create(Guid.NewGuid(), "Spaceship", "R", "EU").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Pass_WithoutBlockingFindings_Succeeds()
    {
        var entity = NewAggregate();
        entity.AddFinding("OBS-1", "Low", "Minor observation").IsSuccess.ShouldBeTrue();
        entity.Pass().IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ComplianceCheckStatus.Passed);
        entity.CheckedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Pass_WithBlockingFinding_Fails()
    {
        var entity = NewAggregate();
        entity.AddFinding("HOS-11H", "Critical", "Severe violation");
        var result = entity.Pass();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ComplianceCheckErrors.BlockingFindings);
    }

    [Fact]
    public void Fail_RequiresAtLeastOneFinding()
    {
        var entity = NewAggregate();
        entity.Fail().IsFailure.ShouldBeTrue();
        entity.AddFinding("HOS-11H", "High", "Violation");
        entity.Fail().IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ComplianceCheckStatus.Failed);
    }

    [Fact]
    public void Waive_OnlyFromFailed_AndRequiresReason()
    {
        var entity = NewAggregate();
        entity.Waive("reason").IsFailure.ShouldBeTrue();

        entity.AddFinding("HOS-11H", "High", "Violation");
        entity.Fail();
        entity.Waive(" ").IsFailure.ShouldBeTrue();
        entity.Waive("Regulator granted exception").IsSuccess.ShouldBeTrue();
        entity.Status.ShouldBe(ComplianceCheckStatus.Waived);
        entity.WaivedReason.ShouldBe("Regulator granted exception");
    }

    [Fact]
    public void AddFinding_AfterDecision_Fails()
    {
        var entity = NewAggregate();
        entity.Pass();
        entity.AddFinding("X", "Low", "Too late").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AddFinding_WithUnknownSeverity_Fails()
    {
        NewAggregate().AddFinding("X", "Catastrophic", "msg").IsFailure.ShouldBeTrue();
    }
}
