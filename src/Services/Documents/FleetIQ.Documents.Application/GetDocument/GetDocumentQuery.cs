using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Documents.Application.Abstractions;

namespace FleetIQ.Documents.Application.GetDocument;

public sealed record GetDocumentQuery(Guid Id) : IQuery<DocumentResponse?>;

public sealed record DocumentResponse(Guid Id, string Status, DateTime CreatedOnUtc);

public sealed class GetDocumentQueryHandler(IDocumentRepository repository)
    : IQueryHandler<GetDocumentQuery, DocumentResponse?>
{
    public async Task<DocumentResponse?> Handle(GetDocumentQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : new DocumentResponse(entity.Id, entity.Status.ToString(), entity.CreatedOnUtc);
    }
}
