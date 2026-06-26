using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.DispatchShipment;

public sealed record DispatchShipmentCommand(Guid ShipmentId) : ICommand<Result>;

public sealed class DispatchShipmentCommandValidator : AbstractValidator<DispatchShipmentCommand>
{
    public DispatchShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}

public sealed class DispatchShipmentCommandHandler(IShipmentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<DispatchShipmentCommand, Result>
{
    public async Task<Result> Handle(DispatchShipmentCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.ShipmentId, ct);
        if (entity is null) return Result.Failure(ShipmentErrors.NotFound);
        var result = entity.Dispatch();
        if (result.IsFailure) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
