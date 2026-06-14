using FleetIQ.Compliance.Domain;

namespace FleetIQ.Compliance.Application.Abstractions;

public interface IComplianceCheckRepository
{
    Task AddAsync(ComplianceCheck entity, CancellationToken ct);
    Task<ComplianceCheck?> GetByIdAsync(Guid id, CancellationToken ct);
}
