using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.ConfirmOrder;

public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<Result>;

public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class ConfirmOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOrderCommand, Result>
{
    public async Task<Result> Handle(ConfirmOrderCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.OrderId, ct);
        if (entity is null) return Result.Failure(OrderErrors.NotFound);

        var result = entity.Confirm();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
