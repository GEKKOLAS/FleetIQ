using FleetIQ.Analytics.Application.Abstractions;
using FleetIQ.Analytics.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Analytics.Infrastructure.Persistence;

public sealed class ReportDefinitionRepository(AnalyticsDbContext db) : IReportDefinitionRepository
{
    public async Task AddAsync(ReportDefinition entity, CancellationToken ct) => await db.ReportDefinitions.AddAsync(entity, ct);

    public Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
}
