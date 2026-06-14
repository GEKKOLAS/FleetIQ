using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Application.CreatePredictionRequest;
using FleetIQ.Intelligence.Application.GetPredictionRequest;
using FluentValidation;

namespace FleetIQ.Intelligence.Api.Endpoints;

public static class PredictionRequestEndpoints
{
    public static void MapPredictionRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/predictionrequests").WithTags("PredictionRequest").RequireAuthorization();

        group.MapPost("/", async (
            CreatePredictionRequestCommand command,
            IValidator<CreatePredictionRequestCommand> validator,
            ICommandHandler<CreatePredictionRequestCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/predictionrequests/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetPredictionRequestQuery, PredictionRequestResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetPredictionRequestQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Complete -> Completed
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            IPredictionRequestRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Complete();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
