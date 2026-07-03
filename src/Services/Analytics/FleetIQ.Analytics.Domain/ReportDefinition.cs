using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Analytics.Domain;

// NOTE: In reality a CQRS read-side service: it consumes events to build read models/KPIs; runs are recorded here.
public enum ReportDefinitionStatus { Active = 0, Archived = 1 }

public static class ReportDefinitionErrors
{
    public static readonly Error InvalidTransition =
        new("ReportDefinition.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error NameRequired =
        new("ReportDefinition.NameRequired", "A name is required.");
    public static readonly Error QueryRequired =
        new("ReportDefinition.QueryRequired", "A query is required.");
    public static readonly Error ScheduleInvalid =
        new("ReportDefinition.ScheduleInvalid", "Schedule must be one of: OnDemand, Daily, Weekly, Monthly.");
    public static readonly Error NotActive =
        new("ReportDefinition.NotActive", "The report definition is archived.");
    public static readonly Error RunNotFound =
        new("ReportDefinition.RunNotFound", "No run with that id exists on this report.");
    public static readonly Error RunAlreadyFinished =
        new("ReportDefinition.RunAlreadyFinished", "The run already finished.");
    public static readonly Error RunInProgress =
        new("ReportDefinition.RunInProgress", "A run is already in progress for this report.");
}

/// <summary>ReportDefinition aggregate root: a saved query plus its execution history.</summary>
public sealed class ReportDefinition : AggregateRoot<Guid>
{
    private static readonly string[] AllowedSchedules = ["OnDemand", "Daily", "Weekly", "Monthly"];
    private readonly List<ReportRun> _runs = new();

    public string Name { get; private set; } = string.Empty;
    public string Query { get; private set; } = string.Empty;
    public string Schedule { get; private set; } = "OnDemand";
    public ReportDefinitionStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastRunOnUtc { get; private set; }
    public IReadOnlyList<ReportRun> Runs => _runs.AsReadOnly();

    private ReportDefinition() { } // EF

    private ReportDefinition(Guid id, string name, string query, string schedule) : base(id)
    {
        Name = name;
        Query = query;
        Schedule = schedule;
        Status = ReportDefinitionStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<ReportDefinition> Create(string name, string query, string schedule = "OnDemand")
    {
        if (string.IsNullOrWhiteSpace(name)) return Result.Failure<ReportDefinition>(ReportDefinitionErrors.NameRequired);
        if (string.IsNullOrWhiteSpace(query)) return Result.Failure<ReportDefinition>(ReportDefinitionErrors.QueryRequired);
        var normalized = AllowedSchedules.FirstOrDefault(s => string.Equals(s, schedule, StringComparison.OrdinalIgnoreCase));
        if (normalized is null) return Result.Failure<ReportDefinition>(ReportDefinitionErrors.ScheduleInvalid);

        var entity = new ReportDefinition(Guid.NewGuid(), name.Trim(), query.Trim(), normalized);
        entity.Raise(new Events.ReportDefinitionCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result UpdateQuery(string query)
    {
        if (Status != ReportDefinitionStatus.Active) return Result.Failure(ReportDefinitionErrors.NotActive);
        if (string.IsNullOrWhiteSpace(query)) return Result.Failure(ReportDefinitionErrors.QueryRequired);

        Query = query.Trim();
        return Result.Success();
    }

    public Result<Guid> StartRun()
    {
        if (Status != ReportDefinitionStatus.Active) return Result.Failure<Guid>(ReportDefinitionErrors.NotActive);
        if (_runs.Any(r => r.CompletedOnUtc is null)) return Result.Failure<Guid>(ReportDefinitionErrors.RunInProgress);

        var run = new ReportRun(Guid.NewGuid());
        _runs.Add(run);
        Raise(new Events.ReportRunStartedDomainEvent(Id, run.Id));
        return Result.Success(run.Id);
    }

    public Result CompleteRun(Guid runId, int rowCount)
    {
        var run = _runs.FirstOrDefault(r => r.Id == runId);
        if (run is null) return Result.Failure(ReportDefinitionErrors.RunNotFound);
        if (run.CompletedOnUtc is not null) return Result.Failure(ReportDefinitionErrors.RunAlreadyFinished);

        run.Complete(rowCount);
        LastRunOnUtc = run.CompletedOnUtc;
        Raise(new Events.ReportRunCompletedDomainEvent(Id, runId, true));
        return Result.Success();
    }

    public Result FailRun(Guid runId, string error)
    {
        var run = _runs.FirstOrDefault(r => r.Id == runId);
        if (run is null) return Result.Failure(ReportDefinitionErrors.RunNotFound);
        if (run.CompletedOnUtc is not null) return Result.Failure(ReportDefinitionErrors.RunAlreadyFinished);

        run.Fail(string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim());
        LastRunOnUtc = run.CompletedOnUtc;
        Raise(new Events.ReportRunCompletedDomainEvent(Id, runId, false));
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status != ReportDefinitionStatus.Active) return Result.Failure(ReportDefinitionErrors.InvalidTransition);
        Status = ReportDefinitionStatus.Archived;
        Raise(new Events.ReportDefinitionStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Restore()
    {
        if (Status != ReportDefinitionStatus.Archived) return Result.Failure(ReportDefinitionErrors.InvalidTransition);
        Status = ReportDefinitionStatus.Active;
        Raise(new Events.ReportDefinitionStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
