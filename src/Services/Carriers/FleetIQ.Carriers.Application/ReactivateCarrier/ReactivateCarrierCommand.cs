using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Contracts;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.ReactivateCarrier;

public sealed record ReactivateCarrierCommand(Guid CarrierId) : ICommand<Result>;

public sealed class ReactivateCarrierCommandHandler(
    ICarrierRepository repository, IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : ICommandHandler<ReactivateCarrierCommand, Result>
{
    public async Task<Result> Handle(ReactivateCarrierCommand command, CancellationToken ct)
    {
        var carrier = await repository.GetByIdAsync(command.CarrierId, ct);
        if (carrier is null) return Result.Failure(CarrierErrors.NotFound);

        var result = carrier.Reactivate();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(new CarrierVerifiedIntegrationEvent(carrier.Id, carrier.LegalName), ct);
        return Result.Success();
    }
}
