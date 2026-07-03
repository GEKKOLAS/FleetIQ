using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Integration.Domain;

// NOTE: Anti-corruption layer: each connector adapts an external system (ERP/WMS/GPS/marketplace) via Refit + Polly; translate external models to/from the domain.
public enum IntegrationConnectorStatus { Registered = 0, Active = 1, Disabled = 2 }

public static class IntegrationConnectorErrors
{
    public static readonly Error InvalidTransition =
        new("IntegrationConnector.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error SystemNameRequired =
        new("IntegrationConnector.SystemNameRequired", "A system name is required.");
    public static readonly Error EndpointInvalid =
        new("IntegrationConnector.EndpointInvalid", "The endpoint must be an absolute http(s) URL.");
    public static readonly Error NotActive =
        new("IntegrationConnector.NotActive", "Only an active connector can record syncs.");
    public static readonly Error ReasonRequired =
        new("IntegrationConnector.ReasonRequired", "A disable reason is required.");
}

/// <summary>IntegrationConnector aggregate root. Lifecycle: Registered -> Active &lt;-&gt; Disabled.
/// Sync results are tracked; 3 consecutive failures auto-disable the connector.</summary>
public sealed class IntegrationConnector : AggregateRoot<Guid>
{
    public const int MaxConsecutiveFailures = 3;

    public string SystemName { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public IntegrationConnectorStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastSyncOnUtc { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public string? DisabledReason { get; private set; }

    private IntegrationConnector() { } // EF

    private IntegrationConnector(Guid id, string systemName, string endpoint) : base(id)
    {
        SystemName = systemName;
        Endpoint = endpoint;
        Status = IntegrationConnectorStatus.Registered;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<IntegrationConnector> Create(string systemName, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(systemName)) return Result.Failure<IntegrationConnector>(IntegrationConnectorErrors.SystemNameRequired);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Result.Failure<IntegrationConnector>(IntegrationConnectorErrors.EndpointInvalid);

        var entity = new IntegrationConnector(Guid.NewGuid(), systemName.Trim(), uri.ToString());
        entity.Raise(new Events.IntegrationConnectorCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result Activate()
    {
        if (Status == IntegrationConnectorStatus.Active) return Result.Failure(IntegrationConnectorErrors.InvalidTransition);

        Status = IntegrationConnectorStatus.Active;
        ConsecutiveFailures = 0;
        DisabledReason = null;
        Raise(new Events.IntegrationConnectorStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Disable(string reason)
    {
        if (Status == IntegrationConnectorStatus.Disabled) return Result.Failure(IntegrationConnectorErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(IntegrationConnectorErrors.ReasonRequired);

        Status = IntegrationConnectorStatus.Disabled;
        DisabledReason = reason.Trim();
        Raise(new Events.IntegrationConnectorStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result RecordSyncSuccess()
    {
        if (Status != IntegrationConnectorStatus.Active) return Result.Failure(IntegrationConnectorErrors.NotActive);

        LastSyncOnUtc = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        return Result.Success();
    }

    /// <summary>Records a failed sync. After MaxConsecutiveFailures in a row the connector auto-disables.</summary>
    public Result RecordSyncFailure(string reason)
    {
        if (Status != IntegrationConnectorStatus.Active) return Result.Failure(IntegrationConnectorErrors.NotActive);

        LastSyncOnUtc = DateTime.UtcNow;
        ConsecutiveFailures++;
        if (ConsecutiveFailures >= MaxConsecutiveFailures)
        {
            Status = IntegrationConnectorStatus.Disabled;
            DisabledReason = $"Auto-disabled after {ConsecutiveFailures} consecutive sync failures. Last error: {reason}";
            Raise(new Events.IntegrationConnectorStatusChangedDomainEvent(Id, Status.ToString()));
        }

        return Result.Success();
    }
}
