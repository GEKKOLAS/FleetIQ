using FleetIQ.Documents.Application.Abstractions;
using FleetIQ.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure.Persistence;

public sealed class DocumentRepository(DocumentsDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document entity, CancellationToken ct) => await db.Documents.AddAsync(entity, ct);

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Documents.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Document>> ListRecentAsync(int take, CancellationToken ct)
    {
        take = take is < 1 or > 100 ? 20 : take;
        return await db.Documents.AsNoTracking()
            .OrderByDescending(d => d.CreatedOnUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
