using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Analytics.Application.Abstractions;

namespace FleetIQ.Analytics.Application.GetReportDefinition;

public sealed record GetReportDefinitionQuery(Guid Id) : IQuery<ReportDefinitionResponse?>;

public sealed record ReportRunLine(Guid Id, DateTime StartedOnUtc, DateTime? CompletedOnUtc, bool? Succeeded, int? RowCount, string? Error);

public sealed record ReportDefinitionResponse(
    Guid Id, string Name, string Query, string Schedule, string Status, DateTime CreatedOnUtc,
    DateTime? LastRunOnUtc, IReadOnlyList<ReportRunLine> Runs);

public sealed class GetReportDefinitionQueryHandler(IReportDefinitionRepository repository)
    : IQueryHandler<GetReportDefinitionQuery, ReportDefinitionResponse?>
{
    public async Task<ReportDefinitionResponse?> Handle(GetReportDefinitionQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null
            ? null
            : new ReportDefinitionResponse(
                entity.Id, entity.Name, entity.Query, entity.Schedule, entity.Status.ToString(), entity.CreatedOnUtc,
                entity.LastRunOnUtc,
                entity.Runs.OrderByDescending(r => r.StartedOnUtc)
                    .Select(r => new ReportRunLine(r.Id, r.StartedOnUtc, r.CompletedOnUtc, r.Succeeded, r.RowCount, r.Error))
                    .ToList());
    }
}
