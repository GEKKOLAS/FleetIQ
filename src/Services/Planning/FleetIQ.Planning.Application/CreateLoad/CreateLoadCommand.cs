using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.CreateLoad;

public sealed record CreateLoadCommand(Guid OrderId, string OriginCity, string DestinationCity, decimal WeightKg) : ICommand<Result<Guid>>;

public sealed class CreateLoadCommandValidator : AbstractValidator<CreateLoadCommand>
{
    public CreateLoadCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OriginCity).NotEmpty();
        RuleFor(x => x.DestinationCity).NotEmpty();
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLoadCommandHandler(ILoadRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateLoadCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLoadCommand command, CancellationToken ct)
    {
        var entity = Load.Create(command.OrderId, command.OriginCity, command.DestinationCity, command.WeightKg);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
