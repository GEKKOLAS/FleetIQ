using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Domain;

namespace FleetIQ.Tracking.Application.ReportException;

public sealed record ReportExceptionCommand(Guid ShipmentId, string Note) : ICommand<Result>;

public sealed class ReportExceptionCommandValidator : AbstractValidator<ReportExceptionCommand>
{
    public ReportExceptionCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty();
    }
}

public sealed class ReportExceptionCommandHandler(IShipmentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<ReportExceptionCommand, Result>
{
    public async Task<Result> Handle(ReportExceptionCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.ShipmentId, ct);
        if (entity is null) return Result.Failure(ShipmentErrors.NotFound);
        var result = entity.ReportException(command.Note);
        if (result.IsFailure) return result;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
