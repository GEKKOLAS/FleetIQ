using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Application.CreateNotification;
using FleetIQ.Notifications.Application.GetNotification;
using FleetIQ.Notifications.Application.ListNotifications;
using FluentValidation;

namespace FleetIQ.Notifications.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notification").RequireAuthorization();

        group.MapPost("/", async (
            CreateNotificationCommand command,
            IValidator<CreateNotificationCommand> validator,
            ICommandHandler<CreateNotificationCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/notifications/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        // LIST recent - lets you observe notifications created by the InvoiceIssued consumer.
        group.MapGet("/", async (
            int? take,
            IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationResponse>> handler,
            CancellationToken ct) =>
        {
            return Results.Ok(await handler.Handle(new ListNotificationsQuery(take ?? 20), ct));
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetNotificationQuery, NotificationResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetNotificationQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPost("/{id:guid}/marksent", async (
            Guid id,
            INotificationRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();
            var result = entity.MarkSent();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });
            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
