using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Compliance.Application.Abstractions;

namespace FleetIQ.Compliance.Application.ListComplianceChecks;

public sealed record ListComplianceChecksQuery(int Take) : IQuery<IReadOnlyList<ComplianceCheckListItem>>;

public sealed record ComplianceCheckListItem(
    Guid Id, Guid SubjectId, string SubjectType, string RuleCode, string Jurisdiction, string Status,
    DateTime CreatedOnUtc, DateTime? CheckedOnUtc, int FindingCount, bool HasBlockingFindings);

public sealed class ListComplianceChecksQueryHandler(IComplianceCheckRepository repository)
    : IQueryHandler<ListComplianceChecksQuery, IReadOnlyList<ComplianceCheckListItem>>
{
    public async Task<IReadOnlyList<ComplianceCheckListItem>> Handle(ListComplianceChecksQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(c => new ComplianceCheckListItem(
                c.Id, c.SubjectId, c.SubjectType, c.RuleCode, c.Jurisdiction, c.Status.ToString(),
                c.CreatedOnUtc, c.CheckedOnUtc, c.Findings.Count, c.HasBlockingFindings))
            .ToList();
    }
}
