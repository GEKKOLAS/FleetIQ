using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Intelligence.Application.Abstractions;
using FleetIQ.Intelligence.Application.CreatePredictionRequest;
using FleetIQ.Intelligence.Application.GetPredictionRequest;
using FleetIQ.Intelligence.Application.ListPredictionRequests;
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

        // LIST recent - powers the future Intelligence page in the SPA.
        group.MapGet("/", async (
            int? take,
            IQueryHandler<ListPredictionRequestsQuery, IReadOnlyList<PredictionRequestListItem>> handler,
            CancellationToken ct) =>
        {
            return Results.Ok(await handler.Handle(new ListPredictionRequestsQuery(take ?? 20), ct));
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetPredictionRequestQuery, PredictionRequestResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetPredictionRequestQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Start -> Running
        group.MapPost("/{id:guid}/start", async (
            Guid id,
            IPredictionRequestRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Start();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // State transition: Complete -> Completed (with result payload + confidence)
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompletePredictionRequest request,
            IPredictionRequestRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Complete(request.ResultJson, request.Confidence);
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // State transition: Fail -> Failed (with an error message)
        group.MapPost("/{id:guid}/fail", async (
            Guid id,
            FailPredictionRequest request,
            IPredictionRequestRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Fail(request.Error);
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}

public sealed record CompletePredictionRequest(string ResultJson, decimal Confidence);
public sealed record FailPredictionRequest(string Error);
