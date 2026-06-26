using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.RecordLocation;

public sealed record RecordLocationCommand(Guid ShipmentId, double Latitude, double Longitude, double? SpeedKph) : ICommand<Result>;

public sealed class RecordLocationCommandValidator : AbstractValidator<RecordLocationCommand>
{
    public RecordLocationCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public sealed class RecordLocationCommandHandler(
    IShipmentRepository repository,
    IUnitOfWork unitOfWork,
    IShipmentLocationBroadcaster broadcaster) : ICommandHandler<RecordLocationCommand, Result>
{
    public async Task<Result> Handle(RecordLocationCommand command, CancellationToken ct)
    {
        var shipment = await repository.GetByIdAsync(command.ShipmentId, ct);
        if (shipment is null) return Result.Failure(ShipmentErrors.NotFound);

        var result = shipment.RecordLocation(command.Latitude, command.Longitude, command.SpeedKph);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);

        // Live push to subscribed map clients.
        await broadcaster.BroadcastAsync(shipment.Id, command.Latitude, command.Longitude, command.SpeedKph,
            shipment.Status.ToString(), DateTime.UtcNow, ct);

        return Result.Success();
    }
}
