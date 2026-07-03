using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Contracts;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.SuspendCarrier;

public sealed record SuspendCarrierCommand(Guid CarrierId, string Reason) : ICommand<Result>;

public sealed class SuspendCarrierCommandHandler(
    ICarrierRepository repository, IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : ICommandHandler<SuspendCarrierCommand, Result>
{
    public async Task<Result> Handle(SuspendCarrierCommand command, CancellationToken ct)
    {
        var carrier = await repository.GetByIdAsync(command.CarrierId, ct);
        if (carrier is null) return Result.Failure(CarrierErrors.NotFound);

        var result = carrier.Suspend(command.Reason ?? "n/a");
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(new CarrierSuspendedIntegrationEvent(carrier.Id), ct);
        return Result.Success();
    }
}
