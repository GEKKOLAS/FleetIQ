using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Identity.Application.Abstractions;
using FleetIQ.Identity.Domain;

namespace FleetIQ.Identity.Application.CreateUserAccount;

public sealed record CreateUserAccountCommand(string Email, string FullName, Guid TenantId) : ICommand<Result<Guid>>;

public sealed class CreateUserAccountCommandValidator : AbstractValidator<CreateUserAccountCommand>
{
    public CreateUserAccountCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public sealed class CreateUserAccountCommandHandler(IUserAccountRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateUserAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserAccountCommand command, CancellationToken ct)
    {
        var entity = UserAccount.Create(command.Email, command.FullName, command.TenantId);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
