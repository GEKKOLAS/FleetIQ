using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Application.CreateShipment;
using FleetIQ.Tracking.Application.GetShipment;
using FluentValidation;

namespace FleetIQ.Tracking.Api.Endpoints;

public static class ShipmentEndpoints
{
    public static void MapShipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipment").RequireAuthorization();

        group.MapPost("/", async (
            CreateShipmentCommand command,
            IValidator<CreateShipmentCommand> validator,
            ICommandHandler<CreateShipmentCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/shipments/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetShipmentQuery, ShipmentResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetShipmentQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Deliver -> Delivered
        group.MapPost("/{id:guid}/deliver", async (
            Guid id,
            IShipmentRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Deliver();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
