using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Orders.Application.Abstractions;
using FleetIQ.Orders.Domain.Orders;

namespace FleetIQ.Orders.Application.MarkReadyForPlanning;

public sealed record MarkReadyForPlanningCommand(Guid OrderId) : ICommand<Result>;

public sealed class MarkReadyForPlanningCommandValidator : AbstractValidator<MarkReadyForPlanningCommand>
{
    public MarkReadyForPlanningCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public sealed class MarkReadyForPlanningCommandHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkReadyForPlanningCommand, Result>
{
    public async Task<Result> Handle(MarkReadyForPlanningCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.OrderId, ct);
        if (entity is null) return Result.Failure(OrderErrors.NotFound);

        var result = entity.MarkReadyForPlanning();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
