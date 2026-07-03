using FleetIQ.Analytics.Domain;

namespace FleetIQ.Analytics.Application.Abstractions;

public interface IReportDefinitionRepository
{
    Task AddAsync(ReportDefinition entity, CancellationToken ct);
    Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ReportDefinition>> ListRecentAsync(int take, CancellationToken ct);
}
