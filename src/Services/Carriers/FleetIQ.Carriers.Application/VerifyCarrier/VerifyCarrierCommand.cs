using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Contracts;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.VerifyCarrier;

public sealed record VerifyCarrierCommand(Guid CarrierId) : ICommand<Result>;

public sealed class VerifyCarrierCommandHandler(
    ICarrierRepository repository, IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : ICommandHandler<VerifyCarrierCommand, Result>
{
    public async Task<Result> Handle(VerifyCarrierCommand command, CancellationToken ct)
    {
        var carrier = await repository.GetByIdAsync(command.CarrierId, ct);
        if (carrier is null) return Result.Failure(CarrierErrors.NotFound);

        var result = carrier.Verify();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        await publisher.PublishAsync(new CarrierVerifiedIntegrationEvent(carrier.Id, carrier.LegalName), ct);
        return Result.Success();
    }
}
