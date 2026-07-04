using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Intelligence.Application.Abstractions;

namespace FleetIQ.Intelligence.Application.ListPredictionRequests;

public sealed record ListPredictionRequestsQuery(int Take) : IQuery<IReadOnlyList<PredictionRequestListItem>>;

public sealed record PredictionRequestListItem(
    Guid Id, string ModelId, string PredictionType, string Status, DateTime CreatedOnUtc,
    DateTime? FinishedOnUtc, decimal? Confidence, string? Error);

public sealed class ListPredictionRequestsQueryHandler(IPredictionRequestRepository repository)
    : IQueryHandler<ListPredictionRequestsQuery, IReadOnlyList<PredictionRequestListItem>>
{
    public async Task<IReadOnlyList<PredictionRequestListItem>> Handle(ListPredictionRequestsQuery query, CancellationToken ct)
    {
        var items = await repository.ListRecentAsync(query.Take, ct);
        return items
            .Select(p => new PredictionRequestListItem(
                p.Id, p.ModelId, p.PredictionType, p.Status.ToString(), p.CreatedOnUtc,
                p.FinishedOnUtc, p.Confidence, p.Error))
            .ToList();
    }
}
