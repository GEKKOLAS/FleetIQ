using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.FinalizeRoute;

public sealed record FinalizeRouteCommand(Guid RouteId) : ICommand<Result>;

public sealed class FinalizeRouteCommandHandler(IRouteRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<FinalizeRouteCommand, Result>
{
    public async Task<Result> Handle(FinalizeRouteCommand command, CancellationToken ct)
    {
        var route = await repository.GetByIdAsync(command.RouteId, ct);
        if (route is null) return Result.Failure(RouteErrors.NotFound);

        var result = route.Finalize();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
