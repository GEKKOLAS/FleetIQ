using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Documents.Application.Abstractions;

namespace FleetIQ.Documents.Application.GetDocument;

public sealed record GetDocumentQuery(Guid Id) : IQuery<DocumentResponse?>;

public sealed record DocumentVersionLine(int Number, string StorageRef, DateTime UploadedOnUtc);

public sealed record DocumentResponse(
    Guid Id,
    Guid OwnerId,
    string Category,
    string StorageRef,
    string Status,
    DateTime CreatedOnUtc,
    DateTime? VerifiedOnUtc,
    DateTime? ExpiresOnUtc,
    string? RejectedReason,
    bool IsExpired,
    IReadOnlyList<DocumentVersionLine> Versions);

public sealed class GetDocumentQueryHandler(IDocumentRepository repository)
    : IQueryHandler<GetDocumentQuery, DocumentResponse?>
{
    public async Task<DocumentResponse?> Handle(GetDocumentQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null
            ? null
            : new DocumentResponse(
                entity.Id, entity.OwnerId, entity.Category, entity.StorageRef, entity.Status.ToString(),
                entity.CreatedOnUtc, entity.VerifiedOnUtc, entity.ExpiresOnUtc, entity.RejectedReason, entity.IsExpired,
                entity.Versions.OrderBy(v => v.Number).Select(v => new DocumentVersionLine(v.Number, v.StorageRef, v.UploadedOnUtc)).ToList());
    }
}
