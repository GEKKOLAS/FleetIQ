using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.ListOrders;

public sealed record ListOrdersQuery(Guid? CustomerId, string? Status, int Page, int PageSize, string? Sort)
    : IQuery<PagedResult<OrderSummaryResponse>>;

public sealed record OrderSummaryResponse(Guid Id, string OrderNumber, Guid CustomerId, string Status, DateTime CreatedOnUtc);

public sealed class ListOrdersQueryHandler(IOrderRepository repository)
    : IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public async Task<PagedResult<OrderSummaryResponse>> Handle(ListOrdersQuery query, CancellationToken ct)
    {
        OrderStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, true, out var parsed))
            status = parsed;

        var paged = await repository.ListAsync(query.CustomerId, status, query.Page, query.PageSize, query.Sort, ct);
        var items = paged.Items
            .Select(o => new OrderSummaryResponse(o.Id, o.OrderNumber, o.CustomerId, o.Status.ToString(), o.CreatedOnUtc))
            .ToList();
        return new PagedResult<OrderSummaryResponse>(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
