using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Domain;

namespace FleetIQ.Intelligence.Application.CreatePredictionRequest;

public sealed record CreatePredictionRequestCommand(string ModelId, string PredictionType) : ICommand<Result<Guid>>;

public sealed class CreatePredictionRequestCommandValidator : AbstractValidator<CreatePredictionRequestCommand>
{
    public CreatePredictionRequestCommandValidator()
    {
        RuleFor(x => x.ModelId).NotEmpty();
        RuleFor(x => x.PredictionType).NotEmpty();
    }
}

public sealed class CreatePredictionRequestCommandHandler(IPredictionRequestRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePredictionRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePredictionRequestCommand command, CancellationToken ct)
    {
        var entity = PredictionRequest.Create(command.ModelId, command.PredictionType);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
