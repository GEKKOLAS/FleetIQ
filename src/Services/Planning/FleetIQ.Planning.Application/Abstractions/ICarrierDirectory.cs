namespace FleetIQ.Planning.Application.Abstractions;

/// <summary>Read-side lookup over the local carrier read-model (fed by Carrier integration events).</summary>
public interface ICarrierDirectory
{
    Task<bool> IsVerifiedAsync(Guid carrierId, CancellationToken ct);
}
