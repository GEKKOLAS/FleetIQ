using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure.Persistence;

public sealed class LoadRepository(PlanningDbContext db) : ILoadRepository
{
    public async Task AddAsync(Load entity, CancellationToken ct) => await db.Loads.AddAsync(entity, ct);

    public Task<Load?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Loads.FirstOrDefaultAsync(x => x.Id == id, ct);
}
