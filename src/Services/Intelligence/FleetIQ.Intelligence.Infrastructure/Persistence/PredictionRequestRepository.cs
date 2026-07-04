using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Intelligence.Infrastructure.Persistence;

public sealed class PredictionRequestRepository(IntelligenceDbContext db) : IPredictionRequestRepository
{
    public async Task AddAsync(PredictionRequest entity, CancellationToken ct) => await db.PredictionRequests.AddAsync(entity, ct);

    public Task<PredictionRequest?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.PredictionRequests.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<PredictionRequest>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.PredictionRequests.AsNoTracking()
            .OrderByDescending(p => p.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
