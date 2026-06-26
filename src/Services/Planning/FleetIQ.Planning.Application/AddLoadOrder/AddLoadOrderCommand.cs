using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.AddLoadOrder;

public sealed record AddLoadOrderCommand(Guid LoadId, Guid OrderId, string Description, decimal WeightKg) : ICommand<Result>;

public sealed class AddLoadOrderCommandValidator : AbstractValidator<AddLoadOrderCommand>
{
    public AddLoadOrderCommandValidator()
    {
        RuleFor(x => x.LoadId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddLoadOrderCommandHandler(ILoadRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddLoadOrderCommand, Result>
{
    public async Task<Result> Handle(AddLoadOrderCommand command, CancellationToken ct)
    {
        var load = await repository.GetByIdAsync(command.LoadId, ct);
        if (load is null) return Result.Failure(LoadErrors.NotFound);

        var result = load.AddOrder(command.OrderId, command.Description, command.WeightKg);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
