using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Analytics.Domain;

/// <summary>One execution of a report definition.</summary>
public sealed class ReportRun : Entity<Guid>
{
    public DateTime StartedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }
    public bool? Succeeded { get; private set; }
    public int? RowCount { get; private set; }
    public string? Error { get; private set; }

    private ReportRun() { } // EF

    internal ReportRun(Guid id) : base(id)
    {
        StartedOnUtc = DateTime.UtcNow;
    }

    internal void Complete(int rowCount)
    {
        CompletedOnUtc = DateTime.UtcNow;
        Succeeded = true;
        RowCount = rowCount;
    }

    internal void Fail(string error)
    {
        CompletedOnUtc = DateTime.UtcNow;
        Succeeded = false;
        Error = error;
    }
}
