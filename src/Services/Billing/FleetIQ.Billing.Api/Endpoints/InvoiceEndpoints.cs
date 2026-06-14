using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Billing.Application.Abstractions;
using FleetIQ.Billing.Application.CreateInvoice;
using FleetIQ.Billing.Application.GetInvoice;
using FluentValidation;

namespace FleetIQ.Billing.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoice").RequireAuthorization();

        group.MapPost("/", async (
            CreateInvoiceCommand command,
            IValidator<CreateInvoiceCommand> validator,
            ICommandHandler<CreateInvoiceCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/invoices/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { result.Error.Code, result.Error.Description });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetInvoiceQuery, InvoiceResponse?> handler,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetInvoiceQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // State transition: Issue -> Issued
        group.MapPost("/{id:guid}/issue", async (
            Guid id,
            IInvoiceRepository repository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity is null) return Results.NotFound();

            var result = entity.Issue();
            if (result.IsFailure) return Results.BadRequest(new { result.Error.Code, result.Error.Description });

            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
