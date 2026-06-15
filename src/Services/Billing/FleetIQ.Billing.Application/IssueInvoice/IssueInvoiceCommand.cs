using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;
using FleetIQ.Contracts;

namespace FleetIQ.Billing.Application.IssueInvoice;

public sealed record IssueInvoiceCommand(Guid InvoiceId, DateTime DueDateUtc) : ICommand<Result>;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.DueDateUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }
}

public sealed class IssueInvoiceCommandHandler(
    IInvoiceRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher) : ICommandHandler<IssueInvoiceCommand, Result>
{
    public async Task<Result> Handle(IssueInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await repository.GetByIdAsync(command.InvoiceId, ct);
        if (invoice is null) return Result.Failure(InvoiceErrors.NotFound);

        var result = invoice.Issue(command.DueDateUtc);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);

        // Tell other bounded contexts (e.g. Notifications) via a shared integration event.
        await publisher.PublishAsync(new InvoiceIssuedIntegrationEvent(
            invoice.Id, invoice.CustomerId, invoice.Total.Amount, invoice.Currency, command.DueDateUtc), ct);

        return Result.Success();
    }
}
