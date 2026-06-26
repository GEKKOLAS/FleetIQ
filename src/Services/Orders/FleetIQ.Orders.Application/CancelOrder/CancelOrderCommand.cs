using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : ICommand<Result>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class CancelOrderCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.OrderId, ct);
        if (entity is null) return Result.Failure(OrderErrors.NotFound);

        var result = entity.Cancel();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
