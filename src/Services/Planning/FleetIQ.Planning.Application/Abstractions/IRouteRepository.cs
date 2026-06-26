using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.Abstractions;

public interface IRouteRepository
{
    Task AddAsync(Route entity, CancellationToken ct);
    Task<Route?> GetByIdAsync(Guid id, CancellationToken ct);
}
