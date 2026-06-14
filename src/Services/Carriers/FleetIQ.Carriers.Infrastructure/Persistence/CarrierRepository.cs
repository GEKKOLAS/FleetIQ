using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure.Persistence;

public sealed class CarrierRepository(CarriersDbContext db) : ICarrierRepository
{
    public async Task AddAsync(Carrier entity, CancellationToken ct) => await db.Carriers.AddAsync(entity, ct);

    public Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Carriers.FirstOrDefaultAsync(x => x.Id == id, ct);
}
