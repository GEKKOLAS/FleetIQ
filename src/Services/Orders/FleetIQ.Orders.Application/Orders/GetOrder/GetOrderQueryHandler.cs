using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Orders.Application.Abstractions;

namespace FleetIQ.Orders.Application.Orders.GetOrder;

public sealed class GetOrderQueryHandler(IOrderRepository repository)
    : IQueryHandler<GetOrderQuery, OrderResponse?>
{
    public async Task<OrderResponse?> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(query.OrderId, ct);
        if (order is null) return null;
        return new OrderResponse(order.Id, order.OrderNumber, order.CustomerId,
            order.Status.ToString(), order.Items.Count, order.CreatedOnUtc);
    }
}
