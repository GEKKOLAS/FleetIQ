using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FleetIQ.Tracking.Application.CreateShipment;
using FleetIQ.Tracking.Application.DeliverShipment;
using FleetIQ.Tracking.Application.DispatchShipment;
using FleetIQ.Tracking.Application.GetShipment;
using FleetIQ.Tracking.Application.ListShipments;
using FleetIQ.Tracking.Application.RecordLocation;
using FleetIQ.Tracking.Application.ReportException;
using FluentValidation;

namespace FleetIQ.Tracking.Api.Endpoints;

public static class ShipmentEndpoints
{
    public static void MapShipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipments").RequireAuthorization();

        group.MapPost("/", async (CreateShipmentCommand command, IValidator<CreateShipmentCommand> validator,
            ICommandHandler<CreateShipmentCommand, Result<Guid>> handler, CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.Created($"/shipments/{r.Value}", new { id = r.Value }) : ToProblem(r.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetShipmentQuery, ShipmentDetailResponse?> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new GetShipmentQuery(id), ct);
            return r is null ? Results.NotFound() : Results.Ok(r);
        });

        group.MapGet("/", async (Guid? carrierId, string? status, int? page, int? pageSize,
            IQueryHandler<ListShipmentsQuery, PagedResult<ShipmentSummaryResponse>> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new ListShipmentsQuery(carrierId, status, page ?? 1, pageSize ?? 50), ct)));

        group.MapPost("/{id:guid}/dispatch", async (Guid id, ICommandHandler<DispatchShipmentCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new DispatchShipmentCommand(id), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/locations", async (Guid id, RecordLocationBody body, IValidator<RecordLocationCommand> validator,
            ICommandHandler<RecordLocationCommand, Result> handler, CancellationToken ct) =>
        {
            var command = new RecordLocationCommand(id, body.Latitude, body.Longitude, body.SpeedKph);
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());
            var r = await handler.Handle(command, ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/exceptions", async (Guid id, NoteBody body, ICommandHandler<ReportExceptionCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new ReportExceptionCommand(id, body.Note), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });

        group.MapPost("/{id:guid}/deliver", async (Guid id, ReceivedByBody body, ICommandHandler<DeliverShipmentCommand, Result> handler, CancellationToken ct) =>
        {
            var r = await handler.Handle(new DeliverShipmentCommand(id, body.ReceivedBy), ct);
            return r.IsSuccess ? Results.NoContent() : ToProblem(r.Error);
        });
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Code.EndsWith("NotFound") ? StatusCodes.Status404NotFound
            : error.Code is "Shipment.NotCreated" or "Shipment.NotMoving" or "Shipment.NotDeliverable" or "Shipment.AlreadyFinished"
                ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        return Results.Problem(detail: error.Description, title: error.Code, statusCode: status);
    }

    public sealed record RecordLocationBody(double Latitude, double Longitude, double? SpeedKph);
    public sealed record NoteBody(string Note);
    public sealed record ReceivedByBody(string ReceivedBy);
}
