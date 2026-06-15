using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Domain;

namespace FleetIQ.Billing.Application.CancelInvoice;

public sealed record CancelInvoiceCommand(Guid InvoiceId) : ICommand<Result>;

public sealed class CancelInvoiceCommandHandler(IInvoiceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelInvoiceCommand, Result>
{
    public async Task<Result> Handle(CancelInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await repository.GetByIdAsync(command.InvoiceId, ct);
        if (invoice is null) return Result.Failure(InvoiceErrors.NotFound);

        var result = invoice.Cancel();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
