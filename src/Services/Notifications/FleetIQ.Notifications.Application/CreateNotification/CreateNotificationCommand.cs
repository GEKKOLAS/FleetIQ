using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Domain;

namespace FleetIQ.Notifications.Application.CreateNotification;

public sealed record CreateNotificationCommand(string Recipient, string Channel, string TemplateId) : ICommand<Result<Guid>>;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty();
        RuleFor(x => x.Channel).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}

public sealed class CreateNotificationCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateNotificationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateNotificationCommand command, CancellationToken ct)
    {
        var created = Notification.Create(command.Recipient, command.Channel, command.TemplateId);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error);

        await repository.AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(created.Value.Id);
    }
}
