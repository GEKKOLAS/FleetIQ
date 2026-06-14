using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Integration.Application.Abstractions;
using FleetIQ.Integration.Application.CreateIntegrationConnector;
using FleetIQ.Integration.Application.GetIntegrationConnector;
using FluentValidation;

namespace FleetIQ.Integration.Api.Endpoints;

public static class IntegrationConnectorEndpoints
{
    public static void MapIntegrationConnectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/integrationconnectors").WithTags("IntegrationConnector").RequireAuthorization();

        group.MapPost("/", async (
            CreateIntegrationConnectorCommand command,
            IValidator<CreateIntegrationConnectorCommand> validator,
            ICommandHandler<CreateIntegrationConnectorCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/integrationconnectors/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetIntegrationConnectorQuery, IntegrationConnectorResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetIntegrationConnectorQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Activate -> Active
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IIntegrationConnectorRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Activate();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
