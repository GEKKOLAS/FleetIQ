using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.AddOrderItem;

public sealed record AddOrderItemCommand(Guid OrderId, string Description, int Quantity, decimal WeightKg) : ICommand<Result>;

public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddOrderItemCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddOrderItemCommand, Result>
{
    public async Task<Result> Handle(AddOrderItemCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.OrderId, ct);
        if (entity is null) return Result.Failure(OrderErrors.NotFound);

        var result = entity.AddItem(command.Description, command.Quantity, command.WeightKg);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
