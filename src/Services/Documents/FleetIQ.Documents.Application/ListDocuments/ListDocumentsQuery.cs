using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Documents.Application.Abstractions;

namespace FleetIQ.Documents.Application.ListDocuments;

public sealed record ListDocumentsQuery(int Take) : IQuery<IReadOnlyList<DocumentListItem>>;

public sealed record DocumentListItem(
    Guid Id, Guid OwnerId, string Category, string StorageRef, string Status, DateTime CreatedOnUtc,
    DateTime? ExpiresOnUtc, bool IsExpired, int VersionCount);

public sealed class ListDocumentsQueryHandler(IDocumentRepository repository)
    : IQueryHandler<ListDocumentsQuery, IReadOnlyList<DocumentListItem>>
{
    public async Task<IReadOnlyList<DocumentListItem>> Handle(ListDocumentsQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(d => new DocumentListItem(
                d.Id, d.OwnerId, d.Category, d.StorageRef, d.Status.ToString(), d.CreatedOnUtc,
                d.ExpiresOnUtc, d.IsExpired, d.Versions.Count))
            .ToList();
    }
}
