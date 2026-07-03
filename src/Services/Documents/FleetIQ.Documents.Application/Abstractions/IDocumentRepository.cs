using FleetIQ.Documents.Domain;

namespace FleetIQ.Documents.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document entity, CancellationToken ct);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Document>> ListRecentAsync(int take, CancellationToken ct);
}
