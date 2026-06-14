using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Compliance.Application.Abstractions;

namespace FleetIQ.Compliance.Application.GetComplianceCheck;

public sealed record GetComplianceCheckQuery(Guid Id) : IQuery<ComplianceCheckResponse?>;

public sealed record ComplianceCheckResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetComplianceCheckQueryHandler(IComplianceCheckRepository repository)
    : IQueryHandler<GetComplianceCheckQuery, ComplianceCheckResponse?>
{
    public async Task<ComplianceCheckResponse?> Handle(GetComplianceCheckQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new ComplianceCheckResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
