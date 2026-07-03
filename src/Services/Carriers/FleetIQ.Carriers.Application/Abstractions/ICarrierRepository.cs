using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.Abstractions;

public interface ICarrierRepository
{
    Task AddAsync(Carrier entity, CancellationToken ct);
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Carrier>> ListAsync(CarrierStatus? status, int page, int pageSize, CancellationToken ct);
}
