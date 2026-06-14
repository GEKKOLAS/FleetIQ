using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Integration.Domain;

// NOTE: Anti-corruption layer: each connector adapts an external system (ERP/WMS/GPS/marketplace) via Refit + Polly; translate external models to/from the domain.
public enum IntegrationConnectorStatus { Registered = 0, Active = 1, Disabled = 2 }

public static class IntegrationConnectorErrors
{
    public static readonly Error InvalidTransition =
        new("IntegrationConnector.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>IntegrationConnector aggregate root.</summary>
public sealed class IntegrationConnector : AggregateRoot<Guid>
{
    public string SystemName { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public IntegrationConnectorStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private IntegrationConnector() { } // EF

    private IntegrationConnector(Guid id, string systemName, string endpoint) : base(id)
    {
        SystemName = systemName;
        Endpoint = endpoint;
        Status = IntegrationConnectorStatus.Registered;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static IntegrationConnector Create(string systemName, string endpoint)
    {
        var entity = new IntegrationConnector(Guid.NewGuid(), systemName, endpoint);
        entity.Raise(new Events.IntegrationConnectorCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Activate()
    {
        if (Status != IntegrationConnectorStatus.Registered) return Result.Failure(IntegrationConnectorErrors.InvalidTransition);
        Status = IntegrationConnectorStatus.Active;
        Raise(new Events.IntegrationConnectorStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}
