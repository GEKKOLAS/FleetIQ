using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.DeliverShipment;

public sealed record DeliverShipmentCommand(Guid ShipmentId, string ReceivedBy) : ICommand<Result>;

public sealed class DeliverShipmentCommandValidator : AbstractValidator<DeliverShipmentCommand>
{
    public DeliverShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.ReceivedBy).NotEmpty();
    }
}

public sealed class DeliverShipmentCommandHandler(IShipmentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeliverShipmentCommand, Result>
{
    public async Task<Result> Handle(DeliverShipmentCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.ShipmentId, ct);
        if (entity is null) return Result.Failure(ShipmentErrors.NotFound);
        var result = entity.Deliver(command.ReceivedBy);
        if (result.IsFailure) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
