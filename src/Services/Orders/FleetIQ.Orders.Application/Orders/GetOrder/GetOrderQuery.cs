using FleetIQ.BuildingBlocks.Application;

namespace FleetIQ.Orders.Application.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse?>;

public sealed record OrderResponse(
    Guid Id, string OrderNumber, Guid CustomerId, string Status,
    int ItemCount, DateTime CreatedOnUtc);
