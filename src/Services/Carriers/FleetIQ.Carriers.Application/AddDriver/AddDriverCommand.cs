using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.AddDriver;

public sealed record AddDriverCommand(Guid CarrierId, string FullName, string LicenceNumber) : ICommand<Result>;

public sealed class AddDriverCommandValidator : AbstractValidator<AddDriverCommand>
{
    public AddDriverCommandValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.LicenceNumber).NotEmpty();
    }
}

public sealed class AddDriverCommandHandler(ICarrierRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddDriverCommand, Result>
{
    public async Task<Result> Handle(AddDriverCommand command, CancellationToken ct)
    {
        var carrier = await repository.GetByIdAsync(command.CarrierId, ct);
        if (carrier is null) return Result.Failure(CarrierErrors.NotFound);

        var result = carrier.AddDriver(command.FullName, command.LicenceNumber);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
