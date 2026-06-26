using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.AttachRoute;

public sealed record AttachRouteCommand(Guid LoadId, Guid RouteId) : ICommand<Result>;

public sealed class AttachRouteCommandValidator : AbstractValidator<AttachRouteCommand>
{
    public AttachRouteCommandValidator()
    {
        RuleFor(x => x.LoadId).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
    }
}

public sealed class AttachRouteCommandHandler(
    ILoadRepository loads, IRouteRepository routes, IUnitOfWork unitOfWork)
    : ICommandHandler<AttachRouteCommand, Result>
{
    public async Task<Result> Handle(AttachRouteCommand command, CancellationToken ct)
    {
        var load = await loads.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        var route = await routes.GetByIdAsync(command.RouteId, ct);
        if (route is null) return Result.Failure(RouteErrors.NotFound);
        if (route.LoadId != load.Id) return Result.Failure(RouteErrors.WrongLoad);
        if (route.Status != RouteStatus.Finalized) return Result.Failure(RouteErrors.NotFinalized);

        var result = load.AttachRoute(route.Id);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
