namespace FleetIQ.BuildingBlocks.Domain;

/// <summary>A fact that happened inside the domain, published after persistence.</summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
