using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Intelligence.Infrastructure.Persistence;

public sealed class PredictionRequestRepository(IntelligenceDbContext db) : IPredictionRequestRepository
{
    public async Task AddAsync(PredictionRequest entity, CancellationToken ct) => await db.PredictionRequests.AddAsync(entity, ct);

    public Task<PredictionRequest?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.PredictionRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
}
