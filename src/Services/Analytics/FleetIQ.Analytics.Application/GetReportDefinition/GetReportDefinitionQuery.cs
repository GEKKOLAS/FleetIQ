using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Analytics.Application.Abstractions;

namespace FleetIQ.Analytics.Application.GetReportDefinition;

public sealed record GetReportDefinitionQuery(Guid Id) : IQuery<ReportDefinitionResponse?>;

public sealed record ReportDefinitionResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetReportDefinitionQueryHandler(IReportDefinitionRepository repository)
    : IQueryHandler<GetReportDefinitionQuery, ReportDefinitionResponse?>
{
    public async Task<ReportDefinitionResponse?> Handle(GetReportDefinitionQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new ReportDefinitionResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
