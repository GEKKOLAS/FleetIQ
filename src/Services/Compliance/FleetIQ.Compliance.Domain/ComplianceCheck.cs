using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Compliance.Domain;

// NOTE: Add a rules engine (NRules), HOS/ELD logs and immutable audit records later.
public enum ComplianceCheckStatus { Pending = 0, Passed = 1, Failed = 2, Waived = 3 }

public static class ComplianceCheckErrors
{
    public static readonly Error InvalidTransition =
        new("ComplianceCheck.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error SubjectRequired =
        new("ComplianceCheck.SubjectRequired", "A subject id is required.");
    public static readonly Error SubjectTypeInvalid =
        new("ComplianceCheck.SubjectTypeInvalid", "Subject type must be one of: Carrier, Driver, Vehicle, Order, Load, Shipment.");
    public static readonly Error RuleCodeRequired =
        new("ComplianceCheck.RuleCodeRequired", "A rule code is required.");
    public static readonly Error JurisdictionRequired =
        new("ComplianceCheck.JurisdictionRequired", "A jurisdiction is required.");
    public static readonly Error SeverityInvalid =
        new("ComplianceCheck.SeverityInvalid", "Severity must be one of: Low, Medium, High, Critical.");
    public static readonly Error MessageRequired =
        new("ComplianceCheck.MessageRequired", "A finding message is required.");
    public static readonly Error BlockingFindings =
        new("ComplianceCheck.BlockingFindings", "The check has High/Critical findings and cannot pass.");
    public static readonly Error NoFindings =
        new("ComplianceCheck.NoFindings", "A check cannot fail without at least one recorded finding.");
    public static readonly Error ReasonRequired =
        new("ComplianceCheck.ReasonRequired", "A waive reason is required.");
}

/// <summary>ComplianceCheck aggregate root. Findings are recorded while Pending; the check can
/// Pass (no blocking findings), Fail (needs findings), and a Failed check can be Waived with a reason.</summary>
public sealed class ComplianceCheck : AggregateRoot<Guid>
{
    private static readonly string[] AllowedSubjectTypes = ["Carrier", "Driver", "Vehicle", "Order", "Load", "Shipment"];
    private readonly List<ComplianceFinding> _findings = new();

    public Guid SubjectId { get; private set; }
    public string SubjectType { get; private set; } = "Carrier";
    public string RuleCode { get; private set; } = string.Empty;
    public string Jurisdiction { get; private set; } = string.Empty;
    public ComplianceCheckStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? CheckedOnUtc { get; private set; }
    public string? WaivedReason { get; private set; }
    public IReadOnlyList<ComplianceFinding> Findings => _findings.AsReadOnly();

    public bool HasBlockingFindings => _findings.Any(f => f.Severity is "High" or "Critical");

    private ComplianceCheck() { } // EF

    private ComplianceCheck(Guid id, Guid subjectId, string subjectType, string ruleCode, string jurisdiction) : base(id)
    {
        SubjectId = subjectId;
        SubjectType = subjectType;
        RuleCode = ruleCode;
        Jurisdiction = jurisdiction;
        Status = ComplianceCheckStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<ComplianceCheck> Create(Guid subjectId, string subjectType, string ruleCode, string jurisdiction)
    {
        if (subjectId == Guid.Empty) return Result.Failure<ComplianceCheck>(ComplianceCheckErrors.SubjectRequired);
        var normalizedType = AllowedSubjectTypes.FirstOrDefault(t => string.Equals(t, subjectType, StringComparison.OrdinalIgnoreCase));
        if (normalizedType is null) return Result.Failure<ComplianceCheck>(ComplianceCheckErrors.SubjectTypeInvalid);
        if (string.IsNullOrWhiteSpace(ruleCode)) return Result.Failure<ComplianceCheck>(ComplianceCheckErrors.RuleCodeRequired);
        if (string.IsNullOrWhiteSpace(jurisdiction)) return Result.Failure<ComplianceCheck>(ComplianceCheckErrors.JurisdictionRequired);

        var entity = new ComplianceCheck(Guid.NewGuid(), subjectId, normalizedType, ruleCode.Trim(), jurisdiction.Trim());
        entity.Raise(new Events.ComplianceCheckCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result AddFinding(string code, string severity, string message)
    {
        if (Status != ComplianceCheckStatus.Pending) return Result.Failure(ComplianceCheckErrors.InvalidTransition);
        var normalizedSeverity = ComplianceFinding.AllowedSeverities.FirstOrDefault(s => string.Equals(s, severity, StringComparison.OrdinalIgnoreCase));
        if (normalizedSeverity is null) return Result.Failure(ComplianceCheckErrors.SeverityInvalid);
        if (string.IsNullOrWhiteSpace(message)) return Result.Failure(ComplianceCheckErrors.MessageRequired);

        _findings.Add(new ComplianceFinding(Guid.NewGuid(), string.IsNullOrWhiteSpace(code) ? RuleCode : code.Trim(), normalizedSeverity, message.Trim()));
        Raise(new Events.ComplianceFindingAddedDomainEvent(Id, normalizedSeverity));
        return Result.Success();
    }

    public Result Pass()
    {
        if (Status != ComplianceCheckStatus.Pending) return Result.Failure(ComplianceCheckErrors.InvalidTransition);
        if (HasBlockingFindings) return Result.Failure(ComplianceCheckErrors.BlockingFindings);

        Status = ComplianceCheckStatus.Passed;
        CheckedOnUtc = DateTime.UtcNow;
        Raise(new Events.ComplianceCheckStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Fail()
    {
        if (Status != ComplianceCheckStatus.Pending) return Result.Failure(ComplianceCheckErrors.InvalidTransition);
        if (_findings.Count == 0) return Result.Failure(ComplianceCheckErrors.NoFindings);

        Status = ComplianceCheckStatus.Failed;
        CheckedOnUtc = DateTime.UtcNow;
        Raise(new Events.ComplianceCheckStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Waive(string reason)
    {
        if (Status != ComplianceCheckStatus.Failed) return Result.Failure(ComplianceCheckErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(ComplianceCheckErrors.ReasonRequired);

        Status = ComplianceCheckStatus.Waived;
        WaivedReason = reason.Trim();
        Raise(new Events.ComplianceCheckStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
