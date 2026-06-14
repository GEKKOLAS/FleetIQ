using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Compliance.Domain;

// NOTE: Add a rules engine (NRules), HOS/ELD logs and immutable audit records.
public enum ComplianceCheckStatus { Pending = 0, Passed = 1, Failed = 2 }

public static class ComplianceCheckErrors
{
    public static readonly Error InvalidTransition =
        new("ComplianceCheck.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>ComplianceCheck aggregate root.</summary>
public sealed class ComplianceCheck : AggregateRoot<Guid>
{
    public Guid SubjectId { get; private set; }
    public string RuleCode { get; private set; } = string.Empty;
    public string Jurisdiction { get; private set; } = string.Empty;
    public ComplianceCheckStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private ComplianceCheck() { } // EF

    private ComplianceCheck(Guid id, Guid subjectId, string ruleCode, string jurisdiction) : base(id)
    {
        SubjectId = subjectId;
        RuleCode = ruleCode;
        Jurisdiction = jurisdiction;
        Status = ComplianceCheckStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static ComplianceCheck Create(Guid subjectId, string ruleCode, string jurisdiction)
    {
        var entity = new ComplianceCheck(Guid.NewGuid(), subjectId, ruleCode, jurisdiction);
        entity.Raise(new Events.ComplianceCheckCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Pass()
    {
        if (Status != ComplianceCheckStatus.Pending) return Result.Failure(ComplianceCheckErrors.InvalidTransition);
        Status = ComplianceCheckStatus.Passed;
        Raise(new Events.ComplianceCheckStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
