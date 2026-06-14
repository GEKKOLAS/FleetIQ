using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.Abstractions;

public interface ICarrierRepository
{
    Task AddAsync(Carrier entity, CancellationToken ct);
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct);
}
