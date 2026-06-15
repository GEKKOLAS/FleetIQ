using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Billing.Application.AddInvoiceLine;
using FleetIQ.Billing.Application.CancelInvoice;
using FleetIQ.Billing.Application.CreateInvoice;
using FleetIQ.Billing.Application.GetInvoice;
using FleetIQ.Billing.Application.IssueInvoice;
using FleetIQ.Billing.Application.ListInvoices;
using FleetIQ.Billing.Application.RecordPayment;
using FluentValidation;

namespace FleetIQ.Billing.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoices").RequireAuthorization();

        // CREATE
        group.MapPost("/", async (
            CreateInvoiceCommand command, IValidator<CreateInvoiceCommand> validator,
            ICommandHandler<CreateInvoiceCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess
                ? Results.Created($"/invoices/{result.Value}", new { id = result.Value })
                : ToProblem(result.Error);
        });

        // GET BY ID
        group.MapGet("/{id:guid}", async (
            Guid id, IQueryHandler<GetInvoiceQuery, InvoiceDetailResponse?> handler, CancellationToken ct) =>
        {
            var response = await handler.Handle(new GetInvoiceQuery(id), ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        // LIST (paged + filtered + sorted)
        group.MapGet("/", async (
            Guid? customerId, string? status, DateTime? from, DateTime? to, int? page, int? pageSize, string? sort,
            IQueryHandler<ListInvoicesQuery, PagedResult<InvoiceSummaryResponse>> handler, CancellationToken ct) =>
        {
            var query = new ListInvoicesQuery(customerId, status, from, to, page ?? 1, pageSize ?? 20, sort);
            return Results.Ok(await handler.Handle(query, ct));
        });

        // ADD LINE
        group.MapPost("/{id:guid}/lines", async (
            Guid id, AddLineRequest body, IValidator<AddInvoiceLineCommand> validator,
            ICommandHandler<AddInvoiceLineCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new AddInvoiceLineCommand(id, body.Description, body.Quantity, body.UnitPrice);
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        // ISSUE
        group.MapPost("/{id:guid}/issue", async (
            Guid id, IssueRequest body, IValidator<IssueInvoiceCommand> validator,
            ICommandHandler<IssueInvoiceCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new IssueInvoiceCommand(id, body.DueDateUtc);
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var result = await handler.Handle(command, ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        // RECORD PAYMENT
        group.MapPost("/{id:guid}/payment", async (
            Guid id, ICommandHandler<RecordInvoicePaymentCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new RecordInvoicePaymentCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });

        // CANCEL
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, ICommandHandler<CancelInvoiceCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CancelInvoiceCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound")
            ? StatusCodes.Status404NotFound
            : error.Code is "Invoice.NotDraft" or "Invoice.NotIssued" or "Invoice.CannotCancelPaid"
                or "Invoice.AlreadyCancelled" or "Invoice.CannotIssueEmpty"
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record AddLineRequest(string Description, int Quantity, decimal UnitPrice);
    public sealed record IssueRequest(DateTime DueDateUtc);
}
