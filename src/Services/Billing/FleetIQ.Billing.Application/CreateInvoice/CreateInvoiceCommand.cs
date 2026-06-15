using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.CreateInvoice;

public sealed record CreateInvoiceLine(string Description, int Quantity, decimal UnitPrice);

public sealed record CreateInvoiceCommand(Guid CustomerId, string Currency, IReadOnlyList<CreateInvoiceLine> Lines)
    : ICommand<Result<Guid>>;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateInvoiceCommandHandler(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateInvoiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var lines = command.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice));
        var result = Invoice.Create(command.CustomerId, command.Currency, lines);
        if (result.IsFailure) return Result.Failure<Guid>(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(result.Value.Id);
    }
}
