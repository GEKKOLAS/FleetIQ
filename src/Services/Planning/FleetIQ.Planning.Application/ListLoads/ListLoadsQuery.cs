using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.ListLoads;

public sealed record ListLoadsQuery(string? Status, int Page, int PageSize) : IQuery<PagedResult<LoadSummaryResponse>>;

public sealed record LoadSummaryResponse(
    Guid Id, string Reference, string Status, string OriginCity, string DestinationCity,
    decimal TotalWeightKg, int OrderCount, Guid? CarrierId, DateTime CreatedOnUtc);

public sealed class ListLoadsQueryHandler(ILoadRepository repository)
    : IQueryHandler<ListLoadsQuery, PagedResult<LoadSummaryResponse>>
{
    public async Task<PagedResult<LoadSummaryResponse>> Handle(ListLoadsQuery query, CancellationToken ct)
    {
        LoadStatus? status = Enum.TryParse<LoadStatus>(query.Status, true, out var s) ? s : null;
        var page = await repository.ListAsync(status, query.Page, query.PageSize, ct);

        var items = page.Items.Select(l => new LoadSummaryResponse(
            l.Id, l.Reference, l.Status.ToString(), l.OriginCity, l.DestinationCity,
            l.TotalWeightKg, l.Orders.Count, l.CarrierId, l.CreatedOnUtc)).ToList();

        return new PagedResult<LoadSummaryResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
