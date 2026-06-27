using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Application.Routing;
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
        // Manually-created shipments get a random real city-to-city trip so they plot a real road route.
        var t = CityGeocoder.RandomTrip();
        var result = Shipment.Create(command.LoadId, command.CarrierId,
            t.OriginCity, t.DestCity, t.OLat, t.OLng, t.DLat, t.DLng);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
