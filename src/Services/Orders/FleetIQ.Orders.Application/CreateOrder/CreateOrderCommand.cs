using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.CreateOrder;

public sealed record CreateOrderItem(string Description, int Quantity, decimal WeightKg);
public sealed record AddressDto(string Street, string City, string PostalCode, string Country, double? Lat = null, double? Lng = null);
public sealed record CreateOrderCommand(Guid CustomerId, AddressDto Pickup, AddressDto Delivery, IReadOnlyList<CreateOrderItem> Items)
    : ICommand<Result<Guid>>;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Pickup).NotNull();
        RuleFor(x => x.Delivery).NotNull();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Description).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0);
            i.RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var pickup = new Address(command.Pickup.Street, command.Pickup.City, command.Pickup.PostalCode, command.Pickup.Country, command.Pickup.Lat, command.Pickup.Lng);
        var delivery = new Address(command.Delivery.Street, command.Delivery.City, command.Delivery.PostalCode, command.Delivery.Country, command.Delivery.Lat, command.Delivery.Lng);
        var items = command.Items.Select(i => (i.Description, i.Quantity, i.WeightKg));

        var result = Order.Create(command.CustomerId, pickup, delivery, items);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
