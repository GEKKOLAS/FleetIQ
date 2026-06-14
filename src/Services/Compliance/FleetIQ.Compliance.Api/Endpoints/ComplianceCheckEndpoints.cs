using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Compliance.Application.Abstractions;
using FleetIQ.Compliance.Application.CreateComplianceCheck;
using FleetIQ.Compliance.Application.GetComplianceCheck;
using FluentValidation;

namespace FleetIQ.Compliance.Api.Endpoints;

public static class ComplianceCheckEndpoints
{
    public static void MapComplianceCheckEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/compliancechecks").WithTags("ComplianceCheck").RequireAuthorization();

        group.MapPost("/", async (
            CreateComplianceCheckCommand command,
            IValidator<CreateComplianceCheckCommand> validator,
            ICommandHandler<CreateComplianceCheckCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/compliancechecks/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetComplianceCheckQuery, ComplianceCheckResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetComplianceCheckQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Pass -> Passed
        group.MapPost("/{id:guid}/pass", async (
            Guid id,
            IComplianceCheckRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Pass();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
