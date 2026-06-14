using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.Abstractions;

public interface ILoadRepository
{
    Task AddAsync(Load entity, CancellationToken ct);
    Task<Load?> GetByIdAsync(Guid id, CancellationToken ct);
}
