using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Analytics.Application.Abstractions;

namespace FleetIQ.Analytics.Application.ListReportDefinitions;

public sealed record ListReportDefinitionsQuery(int Take) : IQuery<IReadOnlyList<ReportDefinitionListItem>>;

public sealed record ReportDefinitionListItem(
    Guid Id, string Name, string Query, string Schedule, string Status, DateTime CreatedOnUtc,
    DateTime? LastRunOnUtc, int RunCount);

public sealed class ListReportDefinitionsQueryHandler(IReportDefinitionRepository repository)
    : IQueryHandler<ListReportDefinitionsQuery, IReadOnlyList<ReportDefinitionListItem>>
{
    public async Task<IReadOnlyList<ReportDefinitionListItem>> Handle(ListReportDefinitionsQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(r => new ReportDefinitionListItem(
                r.Id, r.Name, r.Query, r.Schedule, r.Status.ToString(), r.CreatedOnUtc,
                r.LastRunOnUtc, r.Runs.Count))
            .ToList();
    }
}
