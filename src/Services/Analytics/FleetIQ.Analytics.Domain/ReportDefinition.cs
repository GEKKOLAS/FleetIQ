using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Analytics.Domain;

// NOTE: In reality a CQRS read-side service: it consumes events to build read models/KPIs; this write-aggregate is just the scaffold shape.
public enum ReportDefinitionStatus { Active = 0, Archived = 1 }

public static class ReportDefinitionErrors
{
    public static readonly Error InvalidTransition =
        new("ReportDefinition.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>ReportDefinition aggregate root.</summary>
public sealed class ReportDefinition : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Query { get; private set; } = string.Empty;
    public ReportDefinitionStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private ReportDefinition() { } // EF

    private ReportDefinition(Guid id, string name, string query) : base(id)
    {
        Name = name;
        Query = query;
        Status = ReportDefinitionStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static ReportDefinition Create(string name, string query)
    {
        var entity = new ReportDefinition(Guid.NewGuid(), name, query);
        entity.Raise(new Events.ReportDefinitionCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Archive()
    {
        if (Status != ReportDefinitionStatus.Active) return Result.Failure(ReportDefinitionErrors.InvalidTransition);
        Status = ReportDefinitionStatus.Archived;
        Raise(new Events.ReportDefinitionStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
