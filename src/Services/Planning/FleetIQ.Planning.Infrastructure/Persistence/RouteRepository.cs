using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Planning.Infrastructure.Persistence;

public sealed class RouteRepository(PlanningDbContext db) : IRouteRepository
{
    public async Task AddAsync(Route entity, CancellationToken ct) => await db.Routes.AddAsync(entity, ct);

    public Task<Route?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Routes.Include(r => r.Stops).FirstOrDefaultAsync(r => r.Id == id, ct);
}
