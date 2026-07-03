using FleetIQ.Integration.Domain;

namespace FleetIQ.Integration.Application.Abstractions;

public interface IIntegrationConnectorRepository
{
    Task AddAsync(IntegrationConnector entity, CancellationToken ct);
    Task<IntegrationConnector?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<IntegrationConnector>> ListRecentAsync(int take, CancellationToken ct);
}
