using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.CancelLoad;

public sealed record CancelLoadCommand(Guid LoadId) : ICommand<Result>;

public sealed class CancelLoadCommandHandler(ILoadRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelLoadCommand, Result>
{
    public async Task<Result> Handle(CancelLoadCommand command, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        var result = load.Cancel();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
