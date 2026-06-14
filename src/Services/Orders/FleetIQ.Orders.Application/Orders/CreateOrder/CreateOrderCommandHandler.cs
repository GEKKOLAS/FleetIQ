using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var pickup = new Address(command.Pickup.Street, command.Pickup.City, command.Pickup.PostalCode, command.Pickup.Country, command.Pickup.Lat, command.Pickup.Lng);
        var delivery = new Address(command.Delivery.Street, command.Delivery.City, command.Delivery.PostalCode, command.Delivery.Country, command.Delivery.Lat, command.Delivery.Lng);

        var lines = command.Items.Select(i => new OrderLine(i.Description, i.Quantity, i.WeightKg));
        var result = Order.Create(command.CustomerId, pickup, delivery, lines);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        var order = result.Value;
        await repository.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(order.Id);
    }
}
