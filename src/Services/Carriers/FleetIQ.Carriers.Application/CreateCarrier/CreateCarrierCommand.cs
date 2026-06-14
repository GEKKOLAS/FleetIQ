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
        RuleFor(x => x.LegalName).NotEmpty();
        RuleFor(x => x.RegistrationNumber).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}

public sealed class CreateCarrierCommandHandler(ICarrierRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCarrierCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCarrierCommand command, CancellationToken ct)
    {
        var entity = Carrier.Create(command.LegalName, command.RegistrationNumber, command.Country);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
