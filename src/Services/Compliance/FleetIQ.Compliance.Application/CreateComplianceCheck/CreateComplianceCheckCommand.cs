using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Compliance.Application.Abstractions;
using FleetIQ.Compliance.Domain;

namespace FleetIQ.Compliance.Application.CreateComplianceCheck;

public sealed record CreateComplianceCheckCommand(Guid SubjectId, string RuleCode, string Jurisdiction) : ICommand<Result<Guid>>;

public sealed class CreateComplianceCheckCommandValidator : AbstractValidator<CreateComplianceCheckCommand>
{
    public CreateComplianceCheckCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.RuleCode).NotEmpty();
        RuleFor(x => x.Jurisdiction).NotEmpty();
    }
}

public sealed class CreateComplianceCheckCommandHandler(IComplianceCheckRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateComplianceCheckCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateComplianceCheckCommand command, CancellationToken ct)
    {
        var entity = ComplianceCheck.Create(command.SubjectId, command.RuleCode, command.Jurisdiction);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
