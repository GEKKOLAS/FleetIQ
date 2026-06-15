using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.AddInvoiceLine;

public sealed record AddInvoiceLineCommand(Guid InvoiceId, string Description, int Quantity, decimal UnitPrice)
    : ICommand<Result>;

public sealed class AddInvoiceLineCommandValidator : AbstractValidator<AddInvoiceLineCommand>
{
    public AddInvoiceLineCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddInvoiceLineCommandHandler(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddInvoiceLineCommand, Result>
{
    public async Task<Result> Handle(AddInvoiceLineCommand command, CancellationToken ct)
    {
        var invoice = await repository.GetByIdAsync(command.InvoiceId, ct);
        if (invoice is null) return Result.Failure(InvoiceErrors.NotFound);

        var result = invoice.AddLine(command.Description, command.Quantity, command.UnitPrice);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
