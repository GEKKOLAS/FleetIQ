using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.CreateRoute;

public sealed record CreateRouteCommand(Guid LoadId) : ICommand<Result<Guid>>;

public sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator() => RuleFor(x => x.LoadId).NotEmpty();
}

public sealed class CreateRouteCommandHandler(
    IRouteRepository routes, ILoadRepository loads, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRouteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRouteCommand command, CancellationToken ct)
    {
        var load = await loads.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure<Guid>(LoadErrors.NotFound);

        var result = Route.Create(command.LoadId);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await routes.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
