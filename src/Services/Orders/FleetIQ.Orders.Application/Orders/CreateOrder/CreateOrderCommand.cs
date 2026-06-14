using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Application.Orders.CreateOrder;

public sealed record CreateOrderItem(string Description, int Quantity, decimal WeightKg);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    AddressDto Pickup,
    AddressDto Delivery,
    IReadOnlyList<CreateOrderItem> Items) : ICommand<Result<Guid>>;

public sealed record AddressDto(string Street, string City, string PostalCode, string Country, double? Lat = null, double? Lng = null);
