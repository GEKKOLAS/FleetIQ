using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.RecordPayment;

public sealed record RecordInvoicePaymentCommand(Guid InvoiceId) : ICommand<Result>;

public sealed class RecordInvoicePaymentCommandHandler(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordInvoicePaymentCommand, Result>
{
    public async Task<Result> Handle(RecordInvoicePaymentCommand command, CancellationToken ct)
    {
        var invoice = await repository.GetByIdAsync(command.InvoiceId, ct);
        if (invoice is null) return Result.Failure(InvoiceErrors.NotFound);

        var result = invoice.RecordPayment();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
