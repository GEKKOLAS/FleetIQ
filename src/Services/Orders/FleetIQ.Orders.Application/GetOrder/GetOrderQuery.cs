using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Orders.Application.Abstractions;

namespace FleetIQ.Orders.Application.GetOrder;

public sealed record GetOrderQuery(Guid Id) : IQuery<OrderDetailResponse?>;

public sealed record OrderItemResponse(string Description, int Quantity, decimal WeightKg);

public sealed record OrderDetailResponse(
    Guid Id, string OrderNumber, Guid CustomerId, string Status,
    AddressResponse Pickup, AddressResponse Delivery,
    DateTime CreatedOnUtc, DateTime? ConfirmedOnUtc, DateTime? CancelledOnUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record AddressResponse(string Street, string City, string PostalCode, string Country, double? Lat, double? Lng);

public sealed class GetOrderQueryHandler(IOrderRepository repository)
    : IQueryHandler<GetOrderQuery, OrderDetailResponse?>
{
    public async Task<OrderDetailResponse?> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(query.Id, ct);
        if (order is null) return null;

        return new OrderDetailResponse(
            order.Id, order.OrderNumber, order.CustomerId, order.Status.ToString(),
            new AddressResponse(order.Pickup.Street, order.Pickup.City, order.Pickup.PostalCode, order.Pickup.Country, order.Pickup.Latitude, order.Pickup.Longitude),
            new AddressResponse(order.Delivery.Street, order.Delivery.City, order.Delivery.PostalCode, order.Delivery.Country, order.Delivery.Latitude, order.Delivery.Longitude),
            order.CreatedOnUtc, order.ConfirmedOnUtc, order.CancelledOnUtc,
            order.Items.Select(i => new OrderItemResponse(i.Description, i.Quantity, i.WeightKg)).ToList());
    }
}
