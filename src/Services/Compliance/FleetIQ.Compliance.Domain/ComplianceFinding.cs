using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Compliance.Domain;

/// <summary>A single rule violation or observation recorded during a compliance check.</summary>
public sealed class ComplianceFinding : Entity<Guid>
{
    public static readonly string[] AllowedSeverities = ["Low", "Medium", "High", "Critical"];

    public string Code { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "Low";
    public string Message { get; private set; } = string.Empty;
    public DateTime RecordedOnUtc { get; private set; }

    private ComplianceFinding() { } // EF

    internal ComplianceFinding(Guid id, string code, string severity, string message) : base(id)
    {
        Code = code;
        Severity = severity;
        Message = message;
        RecordedOnUtc = DateTime.UtcNow;
    }
}
