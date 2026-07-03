using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Domain;

namespace FleetIQ.Carriers.Application.CreateCarrier;

public sealed record CreateCarrierCommand(string LegalName, string RegistrationNumber, string Country) : ICommand<Result<Guid>>;

public sealed class CreateCarrierCommandValidator : AbstractValidator<CreateCarrierCommand>
{
    public CreateCarrierCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(400);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(400);
    }
}

public sealed class CreateCarrierCommandHandler(ICarrierRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCarrierCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCarrierCommand command, CancellationToken ct)
    {
        var result = Carrier.Create(command.LegalName, command.RegistrationNumber, command.Country);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
