using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Carriers.Infrastructure.Persistence;

public sealed class CarrierRepository(CarriersDbContext db) : ICarrierRepository
{
    public async Task AddAsync(Carrier entity, CancellationToken ct) => await db.Carriers.AddAsync(entity, ct);

    public Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Carriers.Include(c => c.Vehicles).Include(c => c.Drivers).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Carrier>> ListAsync(CarrierStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var q = db.Carriers.AsNoTracking().Include(c => c.Vehicles).Include(c => c.Drivers).AsQueryable();
        if (status is not null) q = q.Where(c => c.Status == status);
        q = q.OrderByDescending(c => c.CreatedOnUtc);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Carrier>(items, page, pageSize, total);
    }
}
