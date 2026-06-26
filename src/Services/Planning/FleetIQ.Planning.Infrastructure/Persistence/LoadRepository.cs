using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure.Persistence;

public sealed class LoadRepository(PlanningDbContext db) : ILoadRepository
{
    public async Task AddAsync(Load entity, CancellationToken ct) => await db.Loads.AddAsync(entity, ct);

    public Task<Load?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Loads.Include(l => l.Orders).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<PagedResult<Load>> ListAsync(LoadStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var q = db.Loads.AsNoTracking().Include(l => l.Orders).AsQueryable();
        if (status is not null) q = q.Where(l => l.Status == status);
        q = q.OrderByDescending(l => l.CreatedOnUtc);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Load>(items, page, pageSize, total);
    }
}
