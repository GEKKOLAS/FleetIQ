using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Application.CreateLoad;
using FleetIQ.Planning.Application.GetLoad;
using FluentValidation;

namespace FleetIQ.Planning.Api.Endpoints;

public static class LoadEndpoints
{
    public static void MapLoadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loads").WithTags("Load").RequireAuthorization();

        group.MapPost("/", async (
            CreateLoadCommand command,
            IValidator<CreateLoadCommand> validator,
            ICommandHandler<CreateLoadCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/loads/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetLoadQuery, LoadResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetLoadQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Optimize -> Optimized
        group.MapPost("/{id:guid}/optimize", async (
            Guid id,
            ILoadRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Optimize();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
