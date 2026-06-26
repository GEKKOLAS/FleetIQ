using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.PlanLoad;

public sealed record PlanLoadCommand(Guid LoadId) : ICommand<Result>;

public sealed class PlanLoadCommandHandler(ILoadRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<PlanLoadCommand, Result>
{
    public async Task<Result> Handle(PlanLoadCommand command, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        var result = load.Plan();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
