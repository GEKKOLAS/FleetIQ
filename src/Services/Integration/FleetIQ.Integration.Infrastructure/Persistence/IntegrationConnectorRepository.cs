using FleetIQ.Integration.Application.Abstractions;
using FleetIQ.Integration.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Integration.Infrastructure.Persistence;

public sealed class IntegrationConnectorRepository(IntegrationDbContext db) : IIntegrationConnectorRepository
{
    public async Task AddAsync(IntegrationConnector entity, CancellationToken ct) => await db.IntegrationConnectors.AddAsync(entity, ct);

    public Task<IntegrationConnector?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.IntegrationConnectors.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<IntegrationConnector>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.IntegrationConnectors.AsNoTracking()
            .OrderByDescending(c => c.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
