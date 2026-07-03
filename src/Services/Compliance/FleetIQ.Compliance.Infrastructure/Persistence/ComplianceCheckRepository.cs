using FleetIQ.Compliance.Application.Abstractions;
using FleetIQ.Compliance.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Compliance.Infrastructure.Persistence;

public sealed class ComplianceCheckRepository(ComplianceDbContext db) : IComplianceCheckRepository
{
    public async Task AddAsync(ComplianceCheck entity, CancellationToken ct) => await db.ComplianceChecks.AddAsync(entity, ct);

    public Task<ComplianceCheck?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ComplianceChecks.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ComplianceCheck>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.ComplianceChecks.AsNoTracking()
            .OrderByDescending(c => c.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
