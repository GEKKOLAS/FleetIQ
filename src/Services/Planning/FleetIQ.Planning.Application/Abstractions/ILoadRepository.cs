using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.Abstractions;

public interface ILoadRepository
{
    Task AddAsync(Load entity, CancellationToken ct);
    Task<Load?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Load>> ListAsync(LoadStatus? status, int page, int pageSize, CancellationToken ct);
}
