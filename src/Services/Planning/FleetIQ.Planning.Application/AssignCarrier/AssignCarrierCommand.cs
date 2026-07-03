using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.AssignCarrier;

public sealed record AssignCarrierCommand(Guid LoadId, Guid CarrierId) : ICommand<Result>;

public sealed class AssignCarrierCommandValidator : AbstractValidator<AssignCarrierCommand>
{
    public AssignCarrierCommandValidator()
    {
        RuleFor(x => x.LoadId).NotEmpty();
        RuleFor(x => x.CarrierId).NotEmpty();
    }
}

public sealed class AssignCarrierCommandHandler(
    ILoadRepository repository, IUnitOfWork unitOfWork, ICarrierDirectory carriers)
    : ICommandHandler<AssignCarrierCommand, Result>
{
    public async Task<Result> Handle(AssignCarrierCommand command, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        // Validate against the local read-model fed by Carrier integration events.
        if (!await carriers.IsVerifiedAsync(command.CarrierId, ct))
            return Result.Failure(LoadErrors.CarrierNotAssignable);

        var result = load.AssignCarrier(command.CarrierId);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
