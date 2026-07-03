using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Compliance.Application.Abstractions;

namespace FleetIQ.Compliance.Application.GetComplianceCheck;

public sealed record GetComplianceCheckQuery(Guid Id) : IQuery<ComplianceCheckResponse?>;

public sealed record ComplianceFindingLine(string Code, string Severity, string Message, DateTime RecordedOnUtc);

public sealed record ComplianceCheckResponse(
    Guid Id, Guid SubjectId, string SubjectType, string RuleCode, string Jurisdiction, string Status,
    DateTime CreatedOnUtc, DateTime? CheckedOnUtc, string? WaivedReason,
    IReadOnlyList<ComplianceFindingLine> Findings);

public sealed class GetComplianceCheckQueryHandler(IComplianceCheckRepository repository)
    : IQueryHandler<GetComplianceCheckQuery, ComplianceCheckResponse?>
{
    public async Task<ComplianceCheckResponse?> Handle(GetComplianceCheckQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null
            ? null
            : new ComplianceCheckResponse(
                entity.Id, entity.SubjectId, entity.SubjectType, entity.RuleCode, entity.Jurisdiction, entity.Status.ToString(),
                entity.CreatedOnUtc, entity.CheckedOnUtc, entity.WaivedReason,
                entity.Findings.OrderBy(f => f.RecordedOnUtc)
                    .Select(f => new ComplianceFindingLine(f.Code, f.Severity, f.Message, f.RecordedOnUtc))
                    .ToList());
    }
}
