using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Identity.Application.Abstractions;
using FleetIQ.Identity.Application.CreateUserAccount;
using FleetIQ.Identity.Application.GetUserAccount;
using FluentValidation;

namespace FleetIQ.Identity.Api.Endpoints;

public static class UserAccountEndpoints
{
    public static void MapUserAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/useraccounts").WithTags("UserAccount").RequireAuthorization();

        group.MapPost("/", async (
            CreateUserAccountCommand command,
            IValidator<CreateUserAccountCommand> validator,
            ICommandHandler<CreateUserAccountCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/useraccounts/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetUserAccountQuery, UserAccountResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetUserAccountQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Activate -> Active
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IUserAccountRepository repository,
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
