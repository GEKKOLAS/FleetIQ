using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Analytics.Application.Abstractions;
using FleetIQ.Analytics.Application.CreateReportDefinition;
using FleetIQ.Analytics.Application.GetReportDefinition;
using FluentValidation;

namespace FleetIQ.Analytics.Api.Endpoints;

public static class ReportDefinitionEndpoints
{
    public static void MapReportDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reportdefinitions").WithTags("ReportDefinition").RequireAuthorization();

        group.MapPost("/", async (
            CreateReportDefinitionCommand command,
            IValidator<CreateReportDefinitionCommand> validator,
            ICommandHandler<CreateReportDefinitionCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/reportdefinitions/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetReportDefinitionQuery, ReportDefinitionResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetReportDefinitionQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Archive -> Archived
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            IReportDefinitionRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Archive();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
