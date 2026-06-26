using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.CreateShipment;

public sealed record CreateShipmentCommand(Guid LoadId, Guid CarrierId) : ICommand<Result<Guid>>;

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.LoadId).NotEmpty();
        RuleFor(x => x.CarrierId).NotEmpty();
    }
}

public sealed class CreateShipmentCommandHandler(IShipmentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateShipmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateShipmentCommand command, CancellationToken ct)
    {
        var result = Shipment.Create(command.LoadId, command.CarrierId);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);
        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
