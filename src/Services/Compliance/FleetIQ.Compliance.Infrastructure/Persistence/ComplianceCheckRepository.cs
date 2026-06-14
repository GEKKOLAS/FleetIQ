using FleetIQ.Compliance.Application.Abstractions;
using FleetIQ.Compliance.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Compliance.Infrastructure.Persistence;

public sealed class ComplianceCheckRepository(ComplianceDbContext db) : IComplianceCheckRepository
{
    public async Task AddAsync(ComplianceCheck entity, CancellationToken ct) => await db.ComplianceChecks.AddAsync(entity, ct);

    public Task<ComplianceCheck?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ComplianceChecks.FirstOrDefaultAsync(x => x.Id == id, ct);
}
