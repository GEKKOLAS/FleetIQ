using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Analytics.Application.Abstractions;
using FleetIQ.Analytics.Domain;

namespace FleetIQ.Analytics.Application.CreateReportDefinition;

public sealed record CreateReportDefinitionCommand(string Name, string Query, string Schedule = "OnDemand") : ICommand<Result<Guid>>;

public sealed class CreateReportDefinitionCommandValidator : AbstractValidator<CreateReportDefinitionCommand>
{
    public CreateReportDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Query).NotEmpty();
    }
}

public sealed class CreateReportDefinitionCommandHandler(IReportDefinitionRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateReportDefinitionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateReportDefinitionCommand command, CancellationToken ct)
    {
        var created = ReportDefinition.Create(command.Name, command.Query, command.Schedule);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error);

        await repository.AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(created.Value.Id);
    }
}
