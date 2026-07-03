using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.AddVehicle;

public sealed record AddVehicleCommand(Guid CarrierId, string Plate, string Type, decimal CapacityKg) : ICommand<Result>;

public sealed class AddVehicleCommandValidator : AbstractValidator<AddVehicleCommand>
{
    public AddVehicleCommandValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.Plate).NotEmpty();
        RuleFor(x => x.CapacityKg).GreaterThan(0);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<VehicleType>(t, true, out _))
            .WithMessage("Type must be Van, Truck, Trailer or Refrigerated.");
    }
}

public sealed class AddVehicleCommandHandler(ICarrierRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddVehicleCommand, Result>
{
    public async Task<Result> Handle(AddVehicleCommand command, CancellationToken ct)
    {
        var carrier = await repository.GetByIdAsync(command.CarrierId, ct);
        if (carrier is null) return Result.Failure(CarrierErrors.NotFound);

        var type = Enum.Parse<VehicleType>(command.Type, true);
        var result = carrier.AddVehicle(command.Plate, type, command.CapacityKg);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
