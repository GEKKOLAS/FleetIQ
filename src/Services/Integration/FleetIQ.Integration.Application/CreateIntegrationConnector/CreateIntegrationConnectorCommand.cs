using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Integration.Application.Abstractions;
using FleetIQ.Integration.Domain;

namespace FleetIQ.Integration.Application.CreateIntegrationConnector;

public sealed record CreateIntegrationConnectorCommand(string SystemName, string Endpoint) : ICommand<Result<Guid>>;

public sealed class CreateIntegrationConnectorCommandValidator : AbstractValidator<CreateIntegrationConnectorCommand>
{
    public CreateIntegrationConnectorCommandValidator()
    {
        RuleFor(x => x.SystemName).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}

public sealed class CreateIntegrationConnectorCommandHandler(IIntegrationConnectorRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateIntegrationConnectorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateIntegrationConnectorCommand command, CancellationToken ct)
    {
        var created = IntegrationConnector.Create(command.SystemName, command.Endpoint);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error);

        await repository.AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(created.Value.Id);
    }
}
