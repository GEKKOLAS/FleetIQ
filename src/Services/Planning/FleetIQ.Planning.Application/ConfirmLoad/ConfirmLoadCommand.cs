using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Contracts;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.ConfirmLoad;

public sealed record ConfirmLoadCommand(Guid LoadId) : ICommand<Result>;

public sealed class ConfirmLoadCommandHandler(
    ILoadRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher) : ICommandHandler<ConfirmLoadCommand, Result>
{
    public async Task<Result> Handle(ConfirmLoadCommand command, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        var result = load.Confirm();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);

        // Tell Tracking to start executing this load (it will create a Shipment).
        await publisher.PublishAsync(new LoadConfirmedIntegrationEvent(
            load.Id, load.CarrierId!.Value, load.OriginCity, load.DestinationCity,
            load.TotalWeightKg, load.ConfirmedOnUtc!.Value), ct);

        return Result.Success();
    }
}
