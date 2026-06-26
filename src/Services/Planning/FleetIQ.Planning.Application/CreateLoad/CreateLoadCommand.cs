using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.CreateLoad;

public sealed record CreateLoadCommand(string OriginCity, string DestinationCity) : ICommand<Result<Guid>>;

public sealed class CreateLoadCommandValidator : AbstractValidator<CreateLoadCommand>
{
    public CreateLoadCommandValidator()
    {
        RuleFor(x => x.OriginCity).NotEmpty().MaximumLength(400);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(400);
    }
}

public sealed class CreateLoadCommandHandler(ILoadRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateLoadCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLoadCommand command, CancellationToken ct)
    {
        var result = Load.Create(command.OriginCity, command.DestinationCity);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
