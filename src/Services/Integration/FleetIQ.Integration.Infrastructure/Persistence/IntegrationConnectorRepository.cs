using FleetIQ.Integration.Application.Abstractions;
using FleetIQ.Integration.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Integration.Infrastructure.Persistence;

public sealed class IntegrationConnectorRepository(IntegrationDbContext db) : IIntegrationConnectorRepository
{
    public async Task AddAsync(IntegrationConnector entity, CancellationToken ct) => await db.IntegrationConnectors.AddAsync(entity, ct);

    public Task<IntegrationConnector?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.IntegrationConnectors.FirstOrDefaultAsync(x => x.Id == id, ct);
}
