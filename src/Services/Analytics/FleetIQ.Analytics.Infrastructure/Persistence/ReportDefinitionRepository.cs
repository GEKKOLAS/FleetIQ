using FleetIQ.Analytics.Application.Abstractions;
using FleetIQ.Analytics.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Analytics.Infrastructure.Persistence;

public sealed class ReportDefinitionRepository(AnalyticsDbContext db) : IReportDefinitionRepository
{
    public async Task AddAsync(ReportDefinition entity, CancellationToken ct) => await db.ReportDefinitions.AddAsync(entity, ct);

    public Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ReportDefinition>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.ReportDefinitions.AsNoTracking()
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
