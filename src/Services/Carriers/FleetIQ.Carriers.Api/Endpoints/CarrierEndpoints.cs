using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Carriers.Application.Abstractions;
using FleetIQ.Carriers.Application.CreateCarrier;
using FleetIQ.Carriers.Application.GetCarrier;
using FluentValidation;

namespace FleetIQ.Carriers.Api.Endpoints;

public static class CarrierEndpoints
{
    public static void MapCarrierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/carriers").WithTags("Carrier").RequireAuthorization();

        group.MapPost("/", async (
            CreateCarrierCommand command,
            IValidator<CreateCarrierCommand> validator,
            ICommandHandler<CreateCarrierCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/carriers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetCarrierQuery, CarrierResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetCarrierQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Verify -> Verified
        group.MapPost("/{id:guid}/verify", async (
            Guid id,
            ICarrierRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Verify();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
