using FleetIQ.Documents.Application.Abstractions;
using FleetIQ.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetIQ.Documents.Infrastructure.Persistence;

public sealed class DocumentRepository(DocumentsDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document entity, CancellationToken ct) => await db.Documents.AddAsync(entity, ct);

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Documents.FirstOrDefaultAsync(x => x.Id == id, ct);
}
