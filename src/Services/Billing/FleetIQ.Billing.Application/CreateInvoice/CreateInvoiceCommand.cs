using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.CreateInvoice;

public sealed record CreateInvoiceCommand(Guid CustomerId, decimal Amount, string Currency) : ICommand<Result<Guid>>;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty();
    }
}

public sealed class CreateInvoiceCommandHandler(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateInvoiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var entity = Invoice.Create(command.CustomerId, command.Amount, command.Currency);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}
