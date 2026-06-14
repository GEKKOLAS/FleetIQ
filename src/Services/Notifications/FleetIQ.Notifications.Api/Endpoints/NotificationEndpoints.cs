using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Notifications.Application.Abstractions;
using FleetIQ.Notifications.Application.CreateNotification;
using FleetIQ.Notifications.Application.GetNotification;
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

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetNotificationQuery, NotificationResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetNotificationQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: MarkSent -> Sent
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
